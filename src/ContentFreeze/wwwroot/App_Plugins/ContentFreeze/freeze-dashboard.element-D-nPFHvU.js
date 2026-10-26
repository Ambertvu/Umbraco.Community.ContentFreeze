import { nothing as r, html as n, css as N, state as o, customElement as O } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as U } from "@umbraco-cms/backoffice/lit-element";
import { umbConfirmModal as v } from "@umbraco-cms/backoffice/modal";
import { tryExecute as I } from "@umbraco-cms/backoffice/resources";
import { C as L, F as z, f as x, a as p } from "./freeze.context-DB6BsqyL.js";
var P = Object.defineProperty, H = Object.getOwnPropertyDescriptor, C = (t) => {
  throw TypeError(t);
}, l = (t, e, i, c) => {
  for (var h = c > 1 ? void 0 : c ? H(e, i) : e, y = t.length - 1, f; y >= 0; y--)
    (f = t[y]) && (h = (c ? f(e, i, h) : f(h)) || h);
  return c && h && P(e, i, h), h;
}, w = (t, e, i) => e.has(t) || C("Cannot " + i), b = (t, e, i) => (w(t, e, "read from private field"), e.get(t)), k = (t, e, i) => e.has(t) ? C("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, i), W = (t, e, i, c) => (w(t, e, "write to private field"), e.set(t, i), i), s = (t, e, i) => (w(t, e, "access private method"), i), d, a, m, E, S, _, A, M, R, g, D, $, F, T, B;
const q = {
  Scheduled: "warning",
  Active: "danger",
  Ended: "default",
  Expired: "default",
  Cancelled: "default"
};
let u = class extends U {
  constructor() {
    super(), k(this, a), k(this, d), this._phase = "none", this._seconds = 0, this._history = [], this._audit = [], this._busy = !1, this._reason = "", this._startsInMinutes = 0, this._durationMinutes = 60, this._actionReason = "", this.consumeContext(L, (t) => {
      W(this, d, t), this.observe(t?.status, (e) => {
        const i = e?.freeze?.state !== this._status?.freeze?.state || e?.freeze?.key !== this._status?.freeze?.key;
        this._status = e, i && s(this, a, m).call(this);
      }, "_status"), this.observe(t?.phase, (e) => this._phase = e ?? "none", "_phase"), this.observe(t?.secondsRemaining, (e) => this._seconds = e ?? 0, "_seconds");
    });
  }
  connectedCallback() {
    super.connectedCallback(), b(this, d)?.refresh(), s(this, a, m).call(this);
  }
  render() {
    return n`
      <div class="layout">
        ${s(this, a, D).call(this)}
        ${this._status?.freeze ? r : s(this, a, F).call(this)}
        ${s(this, a, T).call(this)}
      </div>
    `;
  }
};
d = /* @__PURE__ */ new WeakMap();
a = /* @__PURE__ */ new WeakSet();
m = async function() {
  const { data: t } = await I(this, z.history({ query: { count: 20 } }));
  this._history = t ?? [], this._selectedId && await s(this, a, E).call(this, this._selectedId);
};
E = async function(t) {
  const { data: e } = await I(this, z.details({ path: { id: String(t) } }));
  this._audit = e?.audit ?? [];
};
S = async function(t) {
  if (this._selectedId === t) {
    this._selectedId = void 0, this._audit = [];
    return;
  }
  this._selectedId = t, await s(this, a, E).call(this, t);
};
_ = async function(t) {
  this._busy = !0;
  try {
    await t(), this._actionReason = "", await s(this, a, m).call(this);
  } finally {
    this._busy = !1;
  }
};
A = async function(t) {
  t.preventDefault();
  const e = this._startsInMinutes <= 0, i = e ? "right away" : `in ${this._startsInMinutes} minutes`;
  try {
    await v(this, {
      headline: e ? "Freeze editing now?" : "Schedule a freeze?",
      content: `Editors will not be able to change content or media ${i}, for ${this._durationMinutes} minutes. Editors who are working now may lose unsaved changes.`,
      color: e ? "danger" : "warning",
      confirmLabel: e ? "Freeze now" : "Schedule freeze"
    });
  } catch {
    return;
  }
  await s(this, a, _).call(this, async () => {
    const { data: c } = await b(this, d).schedule({
      startsInMinutes: this._startsInMinutes,
      durationMinutes: this._durationMinutes,
      reason: this._reason || null,
      force: !1
    });
    c && (this._reason = "");
  });
};
M = async function(t) {
  const e = t.createdBy.isApiUser ? ` This freeze was started by ${t.createdBy.name}${t.correlationId ? ` (${t.correlationId})` : ""}. Are you sure the deployment has finished?` : "";
  try {
    await v(this, {
      headline: "End the freeze now?",
      content: `Editors can make changes again straight away.${e}`,
      color: "danger",
      confirmLabel: "End freeze"
    });
  } catch {
    return;
  }
  await s(this, a, _).call(this, () => b(this, d).end(t.id, this._actionReason || void 0));
};
R = async function(t) {
  try {
    await v(this, {
      headline: "Cancel the scheduled freeze?",
      content: t.createdBy.isApiUser ? `This freeze was scheduled by ${t.createdBy.name}. A pipeline that relies on it will deploy while editors keep editing.` : "The freeze will not start.",
      color: "warning",
      confirmLabel: "Cancel freeze"
    });
  } catch {
    return;
  }
  await s(this, a, _).call(this, () => b(this, d).cancel(t.id, this._actionReason || void 0));
};
g = async function(t, e) {
  await s(this, a, _).call(this, () => b(this, d).extend(t.id, e, this._actionReason || void 0));
};
D = function() {
  const t = this._status?.freeze;
  if (!t || this._phase === "none")
    return n`
        <uui-box headline="Current status">
          <p class="status-line"><uui-icon name="icon-unlocked"></uui-icon> Editing is open. No freeze is scheduled.</p>
          ${s(this, a, $).call(this)}
        </uui-box>
      `;
  const e = this._phase === "active";
  return n`
      <uui-box headline="Current status">
        <div slot="header-actions">
          <uui-tag color=${e ? "danger" : "warning"} look="primary">${e ? "Active" : "Scheduled"}</uui-tag>
        </div>
        <p class="status-line countdown">
          <uui-icon name=${e ? "icon-lock" : "icon-alarm-clock"}></uui-icon>
          <span>${e ? n`Editing is frozen. Ends in <strong>${x(this._seconds)}</strong>.` : n`Starts in <strong>${x(this._seconds)}</strong>.`}</span>
        </p>
        ${s(this, a, $).call(this)}
        <dl>
          <dt>Started by</dt>
          <dd>${t.createdBy.name}${t.createdBy.isApiUser ? n` <uui-tag look="secondary">API user</uui-tag>` : r}</dd>
          ${t.correlationId ? n`<dt>Correlation id</dt><dd><code>${t.correlationId}</code></dd>` : r}
          ${t.reason ? n`<dt>Reason</dt><dd>${t.reason}</dd>` : r}
          <dt>From</dt>
          <dd>${p(t.startsAt)}</dd>
          <dt>Until</dt>
          <dd>${p(t.endsAt)}</dd>
          <dt>Scheduled at</dt>
          <dd>${p(t.createdAt)}</dd>
        </dl>
        <div class="actions">
          <uui-input
            label="Reason (optional)"
            placeholder="Reason (optional)"
            .value=${this._actionReason}
            @input=${(i) => this._actionReason = i.target.value}></uui-input>
          <uui-button look="secondary" label="Extend 15 minutes" ?disabled=${this._busy} @click=${() => s(this, a, g).call(this, t, 15)}></uui-button>
          <uui-button look="secondary" label="Extend 30 minutes" ?disabled=${this._busy} @click=${() => s(this, a, g).call(this, t, 30)}></uui-button>
          ${e ? n`<uui-button look="primary" color="danger" label="End now" ?disabled=${this._busy} @click=${() => s(this, a, M).call(this, t)}></uui-button>` : n`<uui-button look="primary" color="warning" label="Cancel freeze" ?disabled=${this._busy} @click=${() => s(this, a, R).call(this, t)}></uui-button>`}
        </div>
      </uui-box>
    `;
};
$ = function() {
  return this._status?.enforcementDisabled ? n`<uui-tag color="warning">Enforcement is disabled in appsettings (ContentFreeze:Disabled): freezes are recorded but nothing is blocked.</uui-tag>` : r;
};
F = function() {
  return n`
      <uui-box headline="Freeze editing">
        <form class="form" @submit=${s(this, a, A)}>
          <uui-form-layout-item>
            <uui-label slot="label" for="reason">Reason</uui-label>
            <span slot="description">Shown to editors, for example "Release 4.2".</span>
            <uui-input
              id="reason"
              label="Reason"
              .value=${this._reason}
              @input=${(t) => this._reason = t.target.value}></uui-input>
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
              @input=${(t) => this._startsInMinutes = Number(t.target.value) || 0}></uui-input>
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
              @input=${(t) => this._durationMinutes = Number(t.target.value) || 0}></uui-input>
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
};
T = function() {
  return n`
      <uui-box headline="History">
        ${this._history.length === 0 ? n`<p>No freezes yet.</p>` : n`
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
                ${this._history.map((t) => s(this, a, B).call(this, t))}
              </uui-table>
            `}
      </uui-box>
    `;
};
B = function(t) {
  const e = this._selectedId === t.id;
  return n`
      <uui-table-row>
        <uui-table-cell><uui-tag color=${q[t.state]}>${t.state}</uui-tag></uui-table-cell>
        <uui-table-cell>${p(t.startsAt)}</uui-table-cell>
        <uui-table-cell>${p(t.endedAt ?? t.endsAt)}</uui-table-cell>
        <uui-table-cell>${t.reason ?? ""}${t.correlationId ? n`<br /><code>${t.correlationId}</code>` : r}</uui-table-cell>
        <uui-table-cell>${t.createdBy.name}</uui-table-cell>
        <uui-table-cell>${t.endedBy?.name ?? (t.state === "Expired" ? "Expired" : "")}${t.endedEarly ? " (early)" : ""}</uui-table-cell>
        <uui-table-cell>${t.blockedAttempts}</uui-table-cell>
        <uui-table-cell>
          <uui-button compact look="secondary" label=${e ? "Hide log" : "Log"} @click=${() => s(this, a, S).call(this, t.id)}></uui-button>
        </uui-table-cell>
      </uui-table-row>
      ${e ? n`
            <uui-table-row class="audit-row">
              <uui-table-cell class="audit-cell" colspan="8">
                <ul class="audit">
                  ${this._audit.map((i) => n`
                    <li>
                      <span class="audit-time">${p(i.timestamp)}</span>
                      <strong>${i.action}</strong> by ${i.actor.name}${i.actor.isApiUser ? " (API user)" : ""}${i.details ? n` · ${i.details}` : r}
                    </li>
                  `)}
                </ul>
              </uui-table-cell>
            </uui-table-row>
          ` : r}
    `;
};
u.styles = [
  N`
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
    `
];
l([
  o()
], u.prototype, "_status", 2);
l([
  o()
], u.prototype, "_phase", 2);
l([
  o()
], u.prototype, "_seconds", 2);
l([
  o()
], u.prototype, "_history", 2);
l([
  o()
], u.prototype, "_selectedId", 2);
l([
  o()
], u.prototype, "_audit", 2);
l([
  o()
], u.prototype, "_busy", 2);
l([
  o()
], u.prototype, "_reason", 2);
l([
  o()
], u.prototype, "_startsInMinutes", 2);
l([
  o()
], u.prototype, "_durationMinutes", 2);
l([
  o()
], u.prototype, "_actionReason", 2);
u = l([
  O("content-freeze-dashboard")
], u);
export {
  u as ContentFreezeDashboardElement,
  u as element
};
//# sourceMappingURL=freeze-dashboard.element-D-nPFHvU.js.map
