import { css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { FreezeStatusResponseModel } from "../api/index.js";
import {
  CONTENT_FREEZE_CONTEXT,
  formatDateTime,
  formatDuration,
  formatTime,
  type FreezePhase,
} from "../context/freeze.context.js";

/**
 * Always-visible freeze status in the top bar: a countdown while scheduled, a red badge while active.
 */
@customElement("content-freeze-header-app")
export class ContentFreezeHeaderAppElement extends UmbLitElement {
  @state() private _status?: FreezeStatusResponseModel;
  @state() private _phase: FreezePhase = "none";
  @state() private _seconds = 0;

  constructor() {
    super();
    this.consumeContext(CONTENT_FREEZE_CONTEXT, (context) => {
      this.observe(context?.status, (status) => (this._status = status), "_status");
      this.observe(context?.phase, (phase) => (this._phase = phase ?? "none"), "_phase");
      this.observe(context?.secondsRemaining, (seconds) => (this._seconds = seconds ?? 0), "_seconds");
    });
  }

  override render() {
    const freeze = this._status?.freeze;
    if (!freeze || this._phase === "none") {
      return nothing;
    }

    const active = this._phase === "active";
    const label = active ? `Editing frozen · ${formatDuration(this._seconds)}` : `Freeze in ${formatDuration(this._seconds)}`;

    return html`
      <uui-button
        popovertarget="content-freeze-popover"
        look="primary"
        color=${active ? "danger" : "warning"}
        compact
        label=${label}>
        <uui-icon name=${active ? "icon-lock" : "icon-alarm-clock"}></uui-icon>
        <span class="label">${label}</span>
      </uui-button>
      <uui-popover-container id="content-freeze-popover" placement="bottom-end">
        <umb-popover-layout>
          <div class="details">
            <strong>${active ? "Editing is temporarily blocked" : "Editing will be blocked soon"}</strong>
            ${freeze.reason ? html`<p>${freeze.reason}</p>` : nothing}
            <dl>
              <dt>From</dt>
              <dd>${formatDateTime(freeze.startsAt)}</dd>
              <dt>Until about</dt>
              <dd>${formatTime(freeze.endsAt)}</dd>
              <dt>Started by</dt>
              <dd>${freeze.createdBy.name}${freeze.createdBy.isApiUser ? " (API user)" : ""}</dd>
            </dl>
            ${this.#renderNotes()}
            ${this._status?.canManage
              ? html`<uui-button look="secondary" href="section/settings/dashboard/content-freeze" label="Manage freeze"></uui-button>`
              : nothing}
          </div>
        </umb-popover-layout>
      </uui-popover-container>
    `;
  }

  #renderNotes() {
    if (this._status?.enforcementDisabled) {
      return html`<p class="note">Enforcement is disabled in this environment's settings, so nothing is blocked.</p>`;
    }

    if (this._status?.canBypass) {
      return html`<p class="note">Your user group may bypass the freeze: you can keep editing.</p>`;
    }

    return this._phase === "active"
      ? html`<p class="note">Content and media are read-only until the freeze ends.</p>`
      : html`<p class="note">Save your work before the freeze starts.</p>`;
  }

  static override styles = [
    css`
      :host {
        display: flex;
        align-items: center;
      }

      uui-button {
        font-variant-numeric: tabular-nums;
        white-space: nowrap;
      }

      .label {
        margin-left: var(--uui-size-space-2);
      }

      .details {
        padding: var(--uui-size-space-4);
        max-width: 320px;
      }

      dl {
        display: grid;
        grid-template-columns: auto 1fr;
        gap: var(--uui-size-space-1) var(--uui-size-space-3);
        margin: var(--uui-size-space-3) 0;
      }

      dt {
        color: var(--uui-color-text-alt);
      }

      dd {
        margin: 0;
      }

      .note {
        color: var(--uui-color-text-alt);
      }
    `,
  ];
}

export { ContentFreezeHeaderAppElement as element };

declare global {
  interface HTMLElementTagNameMap {
    "content-freeze-header-app": ContentFreezeHeaderAppElement;
  }
}
