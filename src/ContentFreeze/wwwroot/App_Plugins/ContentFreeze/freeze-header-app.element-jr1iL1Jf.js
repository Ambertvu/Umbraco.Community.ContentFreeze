import { nothing as c, html as r, css as _, state as l, customElement as f } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement as b } from "@umbraco-cms/backoffice/lit-element";
import { C as g, f as h, a as y, b as z } from "./freeze.context-DB6BsqyL.js";
var E = Object.defineProperty, $ = Object.getOwnPropertyDescriptor, m = (e) => {
  throw TypeError(e);
}, i = (e, t, s, o) => {
  for (var a = o > 1 ? void 0 : o ? $(t, s) : t, p = e.length - 1, d; p >= 0; p--)
    (d = e[p]) && (a = (o ? d(t, s, a) : d(a)) || a);
  return o && a && E(t, s, a), a;
}, C = (e, t, s) => t.has(e) || m("Cannot " + s), w = (e, t, s) => t.has(e) ? m("Cannot add the same private member more than once") : t instanceof WeakSet ? t.add(e) : t.set(e, s), k = (e, t, s) => (C(e, t, "access private method"), s), u, v;
let n = class extends b {
  constructor() {
    super(), w(this, u), this._phase = "none", this._seconds = 0, this.consumeContext(g, (e) => {
      this.observe(e?.status, (t) => this._status = t, "_status"), this.observe(e?.phase, (t) => this._phase = t ?? "none", "_phase"), this.observe(e?.secondsRemaining, (t) => this._seconds = t ?? 0, "_seconds");
    });
  }
  render() {
    const e = this._status?.freeze;
    if (!e || this._phase === "none")
      return c;
    const t = this._phase === "active", s = t ? `Editing frozen · ${h(this._seconds)}` : `Freeze in ${h(this._seconds)}`;
    return r`
      <uui-button
        popovertarget="content-freeze-popover"
        look="primary"
        color=${t ? "danger" : "warning"}
        compact
        label=${s}>
        <uui-icon name=${t ? "icon-lock" : "icon-alarm-clock"}></uui-icon>
        <span class="label">${s}</span>
      </uui-button>
      <uui-popover-container id="content-freeze-popover" placement="bottom-end">
        <umb-popover-layout>
          <div class="details">
            <strong>${t ? "Editing is temporarily blocked" : "Editing will be blocked soon"}</strong>
            ${e.reason ? r`<p>${e.reason}</p>` : c}
            <dl>
              <dt>From</dt>
              <dd>${y(e.startsAt)}</dd>
              <dt>Until about</dt>
              <dd>${z(e.endsAt)}</dd>
              <dt>Started by</dt>
              <dd>${e.createdBy.name}${e.createdBy.isApiUser ? " (API user)" : ""}</dd>
            </dl>
            ${k(this, u, v).call(this)}
            ${this._status?.canManage ? r`<uui-button look="secondary" href="section/settings/dashboard/content-freeze" label="Manage freeze"></uui-button>` : c}
          </div>
        </umb-popover-layout>
      </uui-popover-container>
    `;
  }
};
u = /* @__PURE__ */ new WeakSet();
v = function() {
  return this._status?.enforcementDisabled ? r`<p class="note">Enforcement is disabled in this environment's settings, so nothing is blocked.</p>` : this._status?.canBypass ? r`<p class="note">Your user group may bypass the freeze: you can keep editing.</p>` : this._phase === "active" ? r`<p class="note">Content and media are read-only until the freeze ends.</p>` : r`<p class="note">Save your work before the freeze starts.</p>`;
};
n.styles = [
  _`
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
    `
];
i([
  l()
], n.prototype, "_status", 2);
i([
  l()
], n.prototype, "_phase", 2);
i([
  l()
], n.prototype, "_seconds", 2);
n = i([
  f("content-freeze-header-app")
], n);
export {
  n as ContentFreezeHeaderAppElement,
  n as element
};
//# sourceMappingURL=freeze-header-app.element-jr1iL1Jf.js.map
