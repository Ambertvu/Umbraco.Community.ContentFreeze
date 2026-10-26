import { css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { FreezeService } from "../api/index.js";
import type {
  FreezeAuditEntryResponseModel,
  FreezeResponseModel,
  FreezeState,
  FreezeStatusResponseModel,
} from "../api/index.js";
import {
  CONTENT_FREEZE_CONTEXT,
  formatDateTime,
  formatDuration,
  type ContentFreezeContext,
  type FreezePhase,
} from "../context/freeze.context.js";

const STATE_COLORS: Record<FreezeState, "default" | "positive" | "warning" | "danger"> = {
  Scheduled: "warning",
  Active: "danger",
  Ended: "default",
  Expired: "default",
  Cancelled: "default",
};

/**
 * Settings dashboard for release managers: see who froze editing and why, end or cancel it, start a manual freeze
 * and look back at earlier freezes.
 */
@customElement("content-freeze-dashboard")
export class ContentFreezeDashboardElement extends UmbLitElement {
  #context?: ContentFreezeContext;

  @state() private _status?: FreezeStatusResponseModel;
  @state() private _phase: FreezePhase = "none";
  @state() private _seconds = 0;
  @state() private _history: Array<FreezeResponseModel> = [];
  @state() private _selectedId?: number;
  @state() private _audit: Array<FreezeAuditEntryResponseModel> = [];
  @state() private _busy = false;

  // New freeze form
  @state() private _reason = "";
  @state() private _startsInMinutes = 0;
  @state() private _durationMinutes = 60;

  // Actions on the current freeze
  @state() private _actionReason = "";

  constructor() {
    super();
    this.consumeContext(CONTENT_FREEZE_CONTEXT, (context) => {
      this.#context = context;
      this.observe(context?.status, (status) => {
        const changed = status?.freeze?.state !== this._status?.freeze?.state || status?.freeze?.key !== this._status?.freeze?.key;
        this._status = status;
        if (changed) {
          this.#loadHistory();
        }
      }, "_status");
      this.observe(context?.phase, (phase) => (this._phase = phase ?? "none"), "_phase");
      this.observe(context?.secondsRemaining, (seconds) => (this._seconds = seconds ?? 0), "_seconds");
    });
  }

  override connectedCallback() {
    super.connectedCallback();
    this.#context?.refresh();
    this.#loadHistory();
  }

  async #loadHistory() {
    const { data } = await tryExecute(this, FreezeService.history({ query: { count: 20 } }));
    this._history = data ?? [];
    if (this._selectedId) {
      await this.#loadAudit(this._selectedId);
    }
  }

  async #loadAudit(id: number) {
    const { data } = await tryExecute(this, FreezeService.details({ path: { id: String(id) } }));
    this._audit = data?.audit ?? [];
  }

  async #toggleDetails(id: number) {
    if (this._selectedId === id) {
      this._selectedId = undefined;
      this._audit = [];
      return;
    }

    this._selectedId = id;
    await this.#loadAudit(id);
  }

  async #run(action: () => Promise<unknown>) {
    this._busy = true;
    try {
      await action();
      this._actionReason = "";
      await this.#loadHistory();
    } finally {
      this._busy = false;
    }
  }

  async #onSchedule(event: Event) {
    event.preventDefault();
    const startsNow = this._startsInMinutes <= 0;
    const when = startsNow ? "right away" : `in ${this._startsInMinutes} minutes`;

    try {
      await umbConfirmModal(this, {
        headline: startsNow ? "Freeze editing now?" : "Schedule a freeze?",
        content: `Editors will not be able to change content or media ${when}, for ${this._durationMinutes} minutes. Editors who are working now may lose unsaved changes.`,
        color: startsNow ? "danger" : "warning",
        confirmLabel: startsNow ? "Freeze now" : "Schedule freeze",
      });
    } catch {
      return;
    }

    await this.#run(async () => {
      const { data } = await this.#context!.schedule({
        startsInMinutes: this._startsInMinutes,
        durationMinutes: this._durationMinutes,
        reason: this._reason || null,
        force: false,
      });
      if (data) {
        this._reason = "";
      }
    });
  }

  async #onEnd(freeze: FreezeResponseModel) {
    const pipelineWarning = freeze.createdBy.isApiUser
      ? ` This freeze was started by ${freeze.createdBy.name}${freeze.correlationId ? ` (${freeze.correlationId})` : ""}. Are you sure the deployment has finished?`
      : "";

    try {
      await umbConfirmModal(this, {
        headline: "End the freeze now?",
        content: `Editors can make changes again straight away.${pipelineWarning}`,
        color: "danger",
        confirmLabel: "End freeze",
      });
    } catch {
      return;
    }

    await this.#run(() => this.#context!.end(freeze.id, this._actionReason || undefined));
  }

  async #onCancel(freeze: FreezeResponseModel) {
    try {
      await umbConfirmModal(this, {
        headline: "Cancel the scheduled freeze?",
        content: freeze.createdBy.isApiUser
          ? `This freeze was scheduled by ${freeze.createdBy.name}. A pipeline that relies on it will deploy while editors keep editing.`
          : "The freeze will not start.",
        color: "warning",
        confirmLabel: "Cancel freeze",
      });
    } catch {
      return;
    }

    await this.#run(() => this.#context!.cancel(freeze.id, this._actionReason || undefined));
  }

  async #onExtend(freeze: FreezeResponseModel, minutes: number) {
    await this.#run(() => this.#context!.extend(freeze.id, minutes, this._actionReason || undefined));
  }

  override render() {
    return html`
      <div class="layout">
        ${this.#renderCurrent()}
        ${this._status?.freeze ? nothing : this.#renderForm()}
        ${this.#renderHistory()}
      </div>
    `;
  }

  #renderCurrent() {
    const freeze = this._status?.freeze;
    if (!freeze || this._phase === "none") {
      return html`
        <uui-box headline="Current status">
          <p class="status-line"><uui-icon name="icon-unlocked"></uui-icon> Editing is open. No freeze is scheduled.</p>
          ${this.#renderDisabledNote()}
        </uui-box>
      `;
    }

    const active = this._phase === "active";
    return html`
      <uui-box headline="Current status">
        <div slot="header-actions">
          <uui-tag color=${active ? "danger" : "warning"} look="primary">${active ? "Active" : "Scheduled"}</uui-tag>
        </div>
        <p class="status-line countdown">
          <uui-icon name=${active ? "icon-lock" : "icon-alarm-clock"}></uui-icon>
          <span>${active ? html`Editing is frozen. Ends in <strong>${formatDuration(this._seconds)}</strong>.` : html`Starts in <strong>${formatDuration(this._seconds)}</strong>.`}</span>
        </p>
        ${this.#renderDisabledNote()}
        <dl>
          <dt>Started by</dt>
          <dd>${freeze.createdBy.name}${freeze.createdBy.isApiUser ? html` <uui-tag look="secondary">API user</uui-tag>` : nothing}</dd>
          ${freeze.correlationId ? html`<dt>Correlation id</dt><dd><code>${freeze.correlationId}</code></dd>` : nothing}
          ${freeze.reason ? html`<dt>Reason</dt><dd>${freeze.reason}</dd>` : nothing}
          <dt>From</dt>
          <dd>${formatDateTime(freeze.startsAt)}</dd>
          <dt>Until</dt>
          <dd>${formatDateTime(freeze.endsAt)}</dd>
          <dt>Scheduled at</dt>
          <dd>${formatDateTime(freeze.createdAt)}</dd>
        </dl>
        <div class="actions">
          <uui-input
            label="Reason (optional)"
            placeholder="Reason (optional)"
            .value=${this._actionReason}
            @input=${(e: InputEvent) => (this._actionReason = (e.target as HTMLInputElement).value)}></uui-input>
          <uui-button look="secondary" label="Extend 15 minutes" ?disabled=${this._busy} @click=${() => this.#onExtend(freeze, 15)}></uui-button>
          <uui-button look="secondary" label="Extend 30 minutes" ?disabled=${this._busy} @click=${() => this.#onExtend(freeze, 30)}></uui-button>
          ${active
            ? html`<uui-button look="primary" color="danger" label="End now" ?disabled=${this._busy} @click=${() => this.#onEnd(freeze)}></uui-button>`
            : html`<uui-button look="primary" color="warning" label="Cancel freeze" ?disabled=${this._busy} @click=${() => this.#onCancel(freeze)}></uui-button>`}
        </div>
      </uui-box>
    `;
  }

  #renderDisabledNote() {
    return this._status?.enforcementDisabled
      ? html`<uui-tag color="warning">Enforcement is disabled in appsettings (ContentFreeze:Disabled): freezes are recorded but nothing is blocked.</uui-tag>`
      : nothing;
  }

  #renderForm() {
    return html`
      <uui-box headline="Freeze editing">
        <form class="form" @submit=${this.#onSchedule}>
          <uui-form-layout-item>
            <uui-label slot="label" for="reason">Reason</uui-label>
            <span slot="description">Shown to editors, for example "Release 4.2".</span>
            <uui-input
              id="reason"
              label="Reason"
              .value=${this._reason}
              @input=${(e: InputEvent) => (this._reason = (e.target as HTMLInputElement).value)}></uui-input>
          </uui-form-layout-item>
          <uui-form-layout-item>
            <uui-label slot="label" for="starts">Starts in (minutes)</uui-label>
            <span slot="description">0 starts the freeze right away. A few minutes gives editors time to save their work.</span>
            <uui-input
              id="starts"
              type="number"
              min="0"
              label="Starts in (minutes)"
              .value=${String(this._startsInMinutes)}
              @input=${(e: InputEvent) => (this._startsInMinutes = Number((e.target as HTMLInputElement).value) || 0)}></uui-input>
          </uui-form-layout-item>
          <uui-form-layout-item>
            <uui-label slot="label" for="duration">Duration (minutes)</uui-label>
            <span slot="description">The freeze ends by itself after this, even if nobody ends it.</span>
            <uui-input
              id="duration"
              type="number"
              min="1"
              label="Duration (minutes)"
              .value=${String(this._durationMinutes)}
              @input=${(e: InputEvent) => (this._durationMinutes = Number((e.target as HTMLInputElement).value) || 0)}></uui-input>
          </uui-form-layout-item>
          <uui-button
            type="submit"
            look="primary"
            color=${this._startsInMinutes > 0 ? "warning" : "danger"}
            label=${this._startsInMinutes > 0 ? "Schedule freeze" : "Freeze now"}
            ?disabled=${this._busy || this._durationMinutes < 1}></uui-button>
        </form>
      </uui-box>
    `;
  }

  #renderHistory() {
    return html`
      <uui-box headline="History">
        ${this._history.length === 0
          ? html`<p>No freezes yet.</p>`
          : html`
              <uui-table>
                <uui-table-head>
                  <uui-table-head-cell>State</uui-table-head-cell>
                  <uui-table-head-cell>From</uui-table-head-cell>
                  <uui-table-head-cell>Until</uui-table-head-cell>
                  <uui-table-head-cell>Reason</uui-table-head-cell>
                  <uui-table-head-cell>Started by</uui-table-head-cell>
                  <uui-table-head-cell>Ended by</uui-table-head-cell>
                  <uui-table-head-cell>Blocked</uui-table-head-cell>
                  <uui-table-head-cell></uui-table-head-cell>
                </uui-table-head>
                ${this._history.map((freeze) => this.#renderHistoryRow(freeze))}
              </uui-table>
            `}
      </uui-box>
    `;
  }

  #renderHistoryRow(freeze: FreezeResponseModel) {
    const selected = this._selectedId === freeze.id;
    return html`
      <uui-table-row>
        <uui-table-cell><uui-tag color=${STATE_COLORS[freeze.state]}>${freeze.state}</uui-tag></uui-table-cell>
        <uui-table-cell>${formatDateTime(freeze.startsAt)}</uui-table-cell>
        <uui-table-cell>${formatDateTime(freeze.endedAt ?? freeze.endsAt)}</uui-table-cell>
        <uui-table-cell>${freeze.reason ?? ""}${freeze.correlationId ? html`<br /><code>${freeze.correlationId}</code>` : nothing}</uui-table-cell>
        <uui-table-cell>${freeze.createdBy.name}</uui-table-cell>
        <uui-table-cell>${freeze.endedBy?.name ?? (freeze.state === "Expired" ? "Expired" : "")}${freeze.endedEarly ? " (early)" : ""}</uui-table-cell>
        <uui-table-cell>${freeze.blockedAttempts}</uui-table-cell>
        <uui-table-cell>
          <uui-button compact look="secondary" label=${selected ? "Hide log" : "Log"} @click=${() => this.#toggleDetails(freeze.id)}></uui-button>
        </uui-table-cell>
      </uui-table-row>
      ${selected
        ? html`
            <uui-table-row class="audit-row">
              <uui-table-cell class="audit-cell" colspan="8">
                <ul class="audit">
                  ${this._audit.map((entry) => html`
                    <li>
                      <span class="audit-time">${formatDateTime(entry.timestamp)}</span>
                      <strong>${entry.action}</strong> by ${entry.actor.name}${entry.actor.isApiUser ? " (API user)" : ""}${entry.details ? html` · ${entry.details}` : nothing}
                    </li>
                  `)}
                </ul>
              </uui-table-cell>
            </uui-table-row>
          `
        : nothing}
    `;
  }

  static override styles = [
    css`
      :host {
        display: block;
        padding: var(--uui-size-layout-1);
      }

      .layout {
        display: grid;
        gap: var(--uui-size-layout-1);
        max-width: 1100px;
      }

      .status-line {
        display: flex;
        align-items: center;
        gap: var(--uui-size-space-3);
        margin-top: 0;
      }

      .countdown {
        font-size: var(--uui-type-h5-size);
        font-variant-numeric: tabular-nums;
      }

      dl {
        display: grid;
        grid-template-columns: max-content 1fr;
        gap: var(--uui-size-space-2) var(--uui-size-space-5);
      }

      dt {
        color: var(--uui-color-text-alt);
      }

      dd {
        margin: 0;
      }

      .actions {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: var(--uui-size-space-3);
      }

      .actions uui-input {
        flex: 1 1 240px;
      }

      .form {
        max-width: 480px;
      }

      .audit-cell {
        display: block;
        width: 100%;
      }

      .audit {
        list-style: none;
        margin: 0;
        padding: var(--uui-size-space-3) 0;
      }

      .audit li {
        padding: var(--uui-size-space-1) 0;
      }

      .audit-time {
        color: var(--uui-color-text-alt);
        margin-right: var(--uui-size-space-3);
        font-variant-numeric: tabular-nums;
      }
    `,
  ];
}

export { ContentFreezeDashboardElement as element };

declare global {
  interface HTMLElementTagNameMap {
    "content-freeze-dashboard": ContentFreezeDashboardElement;
  }
}
