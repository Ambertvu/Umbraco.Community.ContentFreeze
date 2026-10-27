import type { UmbEntryPointOnInit } from "@umbraco-cms/backoffice/extension-api";
import { css, customElement, html, LitElement, nothing, property } from "@umbraco-cms/backoffice/external/lit";

/**
 * Runs on the login screen (and, being public, in the backoffice too, where the header app already covers it).
 * Kept apart from the main bundle so the login screen only loads this file.
 */
export const onInit: UmbEntryPointOnInit = async (host) => {
  if ((host as HTMLElement).tagName !== "UMB-AUTH") return;

  const status = await fetchStatus();
  if (!status?.state) return;

  const notice = document.createElement("content-freeze-login-notice") as ContentFreezeLoginNoticeElement;
  notice.status = status;
  notice.clockOffset = Date.parse(status.serverTime) - Date.now();
  // The login form is slotted next to it, so it sits right above the username field.
  (host as HTMLElement).insertAdjacentElement("afterbegin", notice);
};

interface LoginStatus {
  serverTime: string;
  state: "Scheduled" | "Active" | null;
  startsAt: string | null;
  endsAt: string | null;
}

async function fetchStatus(): Promise<LoginStatus | undefined> {
  try {
    const response = await fetch("/umbraco/contentfreeze/api/v1/freeze/login-status");
    return response.ok ? ((await response.json()) as LoginStatus) : undefined;
  } catch {
    return undefined;
  }
}

function formatTime(value: Date): string {
  const sameDay = value.toDateString() === new Date().toDateString();
  return sameDay
    ? value.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })
    : value.toLocaleString(undefined, { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });
}

@customElement("content-freeze-login-notice")
export class ContentFreezeLoginNoticeElement extends LitElement {
  @property({ attribute: false }) status?: LoginStatus;

  /** Milliseconds the server's clock runs ahead of this browser's, measured when the status was fetched. */
  @property({ attribute: false }) clockOffset = 0;

  #timer?: number;
  #refreshTimer?: number;

  override connectedCallback() {
    super.connectedCallback();
    this.#scheduleNextChange();
    // Picks up a freeze that is extended, or ended early, while the login screen is open.
    this.#refreshTimer = window.setInterval(() => this.#refresh(), 60_000);
  }

  override disconnectedCallback() {
    super.disconnectedCallback();
    window.clearTimeout(this.#timer);
    window.clearInterval(this.#refreshTimer);
  }

  async #refresh() {
    const status = await fetchStatus();
    if (!status) return;

    this.clockOffset = Date.parse(status.serverTime) - Date.now();
    this.status = status;
    window.clearTimeout(this.#timer);
    this.#scheduleNextChange();
  }

  get #now() {
    return Date.now() + this.clockOffset;
  }

  get #isActive() {
    return this.status?.startsAt != null && this.#now >= Date.parse(this.status.startsAt);
  }

  get #hasEnded() {
    return this.status?.endsAt != null && this.#now >= Date.parse(this.status.endsAt);
  }

  // Switch from "soon" to "frozen" at the start, and disappear at the end, without reloading.
  #scheduleNextChange() {
    if (!this.status?.startsAt || !this.status.endsAt) return;

    const next = this.#isActive ? Date.parse(this.status.endsAt) : Date.parse(this.status.startsAt);
    this.#timer = window.setTimeout(
      () => {
        this.requestUpdate();
        if (!this.#hasEnded) this.#scheduleNextChange();
      },
      Math.max(0, next - this.#now) + 500,
    );
  }

  #local(value: string) {
    return formatTime(new Date(Date.parse(value) - this.clockOffset));
  }

  override render() {
    if (!this.status?.startsAt || !this.status.endsAt || this.#hasEnded) return nothing;

    const endsAt = this.#local(this.status.endsAt);
    return this.#isActive
      ? html`<div class="notice active" role="status">
          <strong>Editing is frozen</strong>
          <span>A deployment is running until about ${endsAt}. You can log in, but changes cannot be saved until then.</span>
        </div>`
      : html`<div class="notice" role="status">
          <strong>Editing will be frozen soon</strong>
          <span>From ${this.#local(this.status.startsAt)} until about ${endsAt}, for a deployment. Save your work before it starts.</span>
        </div>`;
  }

  static override styles = css`
    :host {
      display: block;
      margin-bottom: var(--uui-size-space-5);
    }

    .notice {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-1);
      padding: var(--uui-size-space-4) var(--uui-size-space-5);
      border-radius: var(--uui-border-radius);
      border-left: 4px solid var(--uui-color-warning-standalone);
      background: var(--uui-color-warning);
      color: var(--uui-color-warning-contrast);
      line-height: 1.4;
    }

    .notice.active {
      border-left-color: var(--uui-color-danger-standalone);
      background: var(--uui-color-danger);
      color: var(--uui-color-danger-contrast);
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "content-freeze-login-notice": ContentFreezeLoginNoticeElement;
  }
}
