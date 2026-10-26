import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UmbBooleanState, UmbNumberState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { FreezeService } from "../api/index.js";
import type {
  FreezeResponseModel,
  FreezeStatusResponseModel,
  ScheduleFreezeRequestModel,
} from "../api/index.js";

const POLL_INTERVAL_MS = 30_000;
const WARNING_THRESHOLDS_MINUTES = [5, 1] as const;
const ANNOUNCED_STORAGE_KEY = "ContentFreeze.Announced";

/** Where the current freeze is, as seen from the browser's (server-corrected) clock. */
export type FreezePhase = "none" | "scheduled" | "warning" | "active";

/**
 * Keeps track of the current freeze for the whole backoffice: polls the status endpoint, counts down locally
 * (so the switch at the start time needs no request) and warns editors with toasts.
 */
export class ContentFreezeContext extends UmbContextBase {
  #status = new UmbObjectState<FreezeStatusResponseModel | undefined>(undefined);
  readonly status = this.#status.asObservable();
  readonly freeze = this.#status.asObservablePart((status) => status?.freeze ?? undefined);
  readonly canManage = this.#status.asObservablePart((status) => status?.canManage ?? false);

  #phase = new UmbObjectState<FreezePhase>("none");
  readonly phase = this.#phase.asObservable();

  /** True when the current user cannot edit right now: an active freeze, no bypass, enforcement on. */
  #locked = new UmbBooleanState(false);
  readonly isLocked = this.#locked.asObservable();

  /** Seconds until the next transition: the start while scheduled, the end while active. */
  #secondsRemaining = new UmbNumberState(0);
  readonly secondsRemaining = this.#secondsRemaining.asObservable();

  #clockOffsetMs = 0;
  #pollTimer?: number;
  #tickTimer?: number;
  #announced = this.#loadAnnounced();
  #lastFreezeKey?: string;
  #lastPhase: FreezePhase = "none";

  constructor(host: UmbControllerHost) {
    super(host, CONTENT_FREEZE_CONTEXT);

    this.refresh();
    this.#pollTimer = window.setInterval(() => this.refresh(), POLL_INTERVAL_MS);
    this.#tickTimer = window.setInterval(() => this.#tick(), 1000);
    document.addEventListener("visibilitychange", this.#onVisibilityChange);
  }

  /** The current time on the server's clock. */
  now(): number {
    return Date.now() + this.#clockOffsetMs;
  }

  async refresh() {
    const { data } = await FreezeService.status();
    if (!data) {
      return;
    }

    this.#clockOffsetMs = Date.parse(data.serverTime) - Date.now();
    this.#status.setValue(data);
    this.#tick();
  }

  async schedule(body: ScheduleFreezeRequestModel) {
    const result = await tryExecute(this, FreezeService.schedule({ body }));
    await this.refresh();
    return result;
  }

  async extend(id: number, minutes: number, reason?: string) {
    const result = await tryExecute(this, FreezeService.extend({ path: { id: String(id) }, body: { minutes, reason } }));
    await this.refresh();
    return result;
  }

  async end(id: number, reason?: string) {
    const result = await tryExecute(this, FreezeService.end({ path: { id: String(id) }, body: { reason } }));
    await this.refresh();
    return result;
  }

  async cancel(id: number, reason?: string) {
    const result = await tryExecute(this, FreezeService.cancel({ path: { id: String(id) }, query: { reason } }));
    await this.refresh();
    return result;
  }

  getPhase(freeze: FreezeResponseModel | undefined | null, now = this.now()): FreezePhase {
    if (!freeze) {
      return "none";
    }

    const startsAt = Date.parse(freeze.startsAt);
    const endsAt = Date.parse(freeze.endsAt);
    if (now >= endsAt) {
      return "none";
    }

    if (now >= startsAt) {
      return "active";
    }

    return startsAt - now <= WARNING_THRESHOLDS_MINUTES[0] * 60_000 ? "warning" : "scheduled";
  }

  #tick() {
    const status = this.#status.getValue();
    const freeze = status?.freeze ?? undefined;
    const now = this.now();
    const phase = this.getPhase(freeze, now);

    this.#phase.setValue(phase);
    this.#locked.setValue(phase === "active" && !status?.canBypass && !status?.enforcementDisabled);

    let remaining = 0;
    if (freeze && phase !== "none") {
      const target = phase === "active" ? Date.parse(freeze.endsAt) : Date.parse(freeze.startsAt);
      remaining = Math.max(0, Math.ceil((target - now) / 1000));
    }
    this.#secondsRemaining.setValue(remaining);

    this.#announce(freeze, phase, now);

    // The server records the start and end itself; fetch its view once the local clock passes either.
    if (this.#lastFreezeKey === freeze?.key && this.#lastPhase !== phase && (phase === "active" || phase === "none")) {
      window.setTimeout(() => this.refresh(), 2000);
    }

    this.#lastFreezeKey = freeze?.key;
    this.#lastPhase = phase;
  }

  async #announce(freeze: FreezeResponseModel | undefined, phase: FreezePhase, now: number) {
    const status = this.#status.getValue();
    const bypass = status?.canBypass ? " You can keep editing: your user group may bypass the freeze." : "";
    const reason = freeze?.reason ? ` (${freeze.reason})` : "";

    if (!freeze) {
      if (this.#lastPhase === "active") {
        await this.#toast("positive", "Editing is available again", "The content freeze has ended.");
      }
      return;
    }

    if (phase === "none" && this.#lastPhase === "active") {
      await this.#toast("positive", "Editing is available again", "The content freeze has ended.");
      return;
    }

    if (phase === "active") {
      if (this.#once(freeze, "active")) {
        await this.#toast(
          "danger",
          "Editing is temporarily blocked",
          `Content and media cannot be changed${reason} until about ${formatTime(freeze.endsAt)}.${bypass}`,
        );
      }
      return;
    }

    // Scheduled: announce once when first seen, then at each warning threshold.
    const minutesLeft = (Date.parse(freeze.startsAt) - now) / 60_000;
    const passed = WARNING_THRESHOLDS_MINUTES.filter((minutes) => minutesLeft <= minutes);
    const threshold = passed.length > 0 ? passed[passed.length - 1] : undefined;
    const step = threshold === undefined ? "scheduled" : `warn-${threshold}`;

    // On first sight only the most urgent step is shown, not every step already passed.
    if (this.#once(freeze, step)) {
      for (const minutes of WARNING_THRESHOLDS_MINUTES) {
        if (minutesLeft <= minutes) {
          this.#announced.add(`${freeze.key}:warn-${minutes}`);
        }
      }
      this.#announced.add(`${freeze.key}:scheduled`);

      const when = threshold === undefined ? `at ${formatTime(freeze.startsAt)}` : `in ${formatDuration(Math.ceil(minutesLeft * 60))}`;
      await this.#toast(
        "warning",
        "Editing will be blocked soon",
        `Editing will be blocked ${when}${reason}, until about ${formatTime(freeze.endsAt)}. Save your work.${bypass}`,
      );
    }
  }

  #once(freeze: FreezeResponseModel, step: string) {
    const key = `${freeze.key}:${step}`;
    if (this.#announced.has(key)) {
      return false;
    }

    this.#announced.add(key);
    this.#saveAnnounced();
    return true;
  }

  // Remembered per browser tab, so a page reload does not repeat toasts the editor has already seen.
  #loadAnnounced() {
    try {
      const stored = sessionStorage.getItem(ANNOUNCED_STORAGE_KEY);
      return new Set<string>(stored ? JSON.parse(stored) : []);
    } catch {
      return new Set<string>();
    }
  }

  #saveAnnounced() {
    try {
      sessionStorage.setItem(ANNOUNCED_STORAGE_KEY, JSON.stringify([...this.#announced].slice(-50)));
    } catch {
      // Storage unavailable: toasts may repeat after a reload, nothing worse.
    }
  }

  async #toast(color: "positive" | "warning" | "danger", headline: string, message: string) {
    const notifications = await this.getContext(UMB_NOTIFICATION_CONTEXT);
    notifications?.peek(color, { data: { headline, message } });
  }

  #onVisibilityChange = () => {
    if (document.visibilityState === "visible") {
      this.refresh();
    }
  };

  override destroy() {
    window.clearInterval(this.#pollTimer);
    window.clearInterval(this.#tickTimer);
    document.removeEventListener("visibilitychange", this.#onVisibilityChange);
    super.destroy();
  }
}

export const CONTENT_FREEZE_CONTEXT = new UmbContextToken<ContentFreezeContext>("ContentFreezeContext");

export function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

export function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString([], { dateStyle: "medium", timeStyle: "short" });
}

export function formatDuration(totalSeconds: number) {
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  if (hours > 0) {
    return `${hours}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
  }

  return `${minutes}:${String(seconds).padStart(2, "0")}`;
}

export { ContentFreezeContext as api };
