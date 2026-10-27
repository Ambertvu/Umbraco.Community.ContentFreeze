import { css as E, property as D, customElement as O, LitElement as T, nothing as S, html as A } from "@umbraco-cms/backoffice/external/lit";
var x = Object.defineProperty, L = Object.getOwnPropertyDescriptor, b = (t) => {
  throw TypeError(t);
}, m = (t, e, s, o) => {
  for (var i = o > 1 ? void 0 : o ? L(e, s) : e, f = t.length - 1, p; f >= 0; f--)
    (p = t[f]) && (i = (o ? p(e, s, i) : p(i)) || i);
  return o && i && x(e, s, i), i;
}, w = (t, e, s) => e.has(t) || b("Cannot " + s), n = (t, e, s) => (w(t, e, "read from private field"), s ? s.call(t) : e.get(t)), v = (t, e, s) => e.has(t) ? b("Cannot add the same private member more than once") : e instanceof WeakSet ? e.add(t) : e.set(t, s), k = (t, e, s, o) => (w(t, e, "write to private field"), e.set(t, s), s), r = (t, e, s) => (w(t, e, "access private method"), s), c, l, a, z, d, _, y, h, g;
const P = async (t) => {
  if (t.tagName !== "UMB-AUTH") return;
  const e = await C();
  if (!e?.state) return;
  const s = document.createElement("content-freeze-login-notice");
  s.status = e, s.clockOffset = Date.parse(e.serverTime) - Date.now(), t.insertAdjacentElement("afterbegin", s);
};
async function C() {
  try {
    const t = await fetch("/umbraco/contentfreeze/api/v1/freeze/login-status");
    return t.ok ? await t.json() : void 0;
  } catch {
    return;
  }
}
function M(t) {
  return t.toDateString() === (/* @__PURE__ */ new Date()).toDateString() ? t.toLocaleTimeString(void 0, { hour: "2-digit", minute: "2-digit" }) : t.toLocaleString(void 0, { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });
}
let u = class extends T {
  constructor() {
    super(...arguments), v(this, a), this.clockOffset = 0, v(this, c), v(this, l);
  }
  connectedCallback() {
    super.connectedCallback(), r(this, a, h).call(this), k(this, l, window.setInterval(() => r(this, a, z).call(this), 6e4));
  }
  disconnectedCallback() {
    super.disconnectedCallback(), window.clearTimeout(n(this, c)), window.clearInterval(n(this, l));
  }
  render() {
    if (!this.status?.startsAt || !this.status.endsAt || n(this, a, y)) return S;
    const t = r(this, a, g).call(this, this.status.endsAt);
    return n(this, a, _) ? A`<div class="notice active" role="status">
          <strong>Editing is frozen</strong>
          <span>A deployment is running until about ${t}. You can log in, but changes cannot be saved until then.</span>
        </div>` : A`<div class="notice" role="status">
          <strong>Editing will be frozen soon</strong>
          <span>From ${r(this, a, g).call(this, this.status.startsAt)} until about ${t}, for a deployment. Save your work before it starts.</span>
        </div>`;
  }
};
c = /* @__PURE__ */ new WeakMap();
l = /* @__PURE__ */ new WeakMap();
a = /* @__PURE__ */ new WeakSet();
z = async function() {
  const t = await C();
  t && (this.clockOffset = Date.parse(t.serverTime) - Date.now(), this.status = t, window.clearTimeout(n(this, c)), r(this, a, h).call(this));
};
d = function() {
  return Date.now() + this.clockOffset;
};
_ = function() {
  return this.status?.startsAt != null && n(this, a, d) >= Date.parse(this.status.startsAt);
};
y = function() {
  return this.status?.endsAt != null && n(this, a, d) >= Date.parse(this.status.endsAt);
};
h = function() {
  if (!this.status?.startsAt || !this.status.endsAt) return;
  const t = n(this, a, _) ? Date.parse(this.status.endsAt) : Date.parse(this.status.startsAt);
  k(this, c, window.setTimeout(
    () => {
      this.requestUpdate(), n(this, a, y) || r(this, a, h).call(this);
    },
    Math.max(0, t - n(this, a, d)) + 500
  ));
};
g = function(t) {
  return M(new Date(Date.parse(t) - this.clockOffset));
};
u.styles = E`
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
m([
  D({ attribute: !1 })
], u.prototype, "status", 2);
m([
  D({ attribute: !1 })
], u.prototype, "clockOffset", 2);
u = m([
  O("content-freeze-login-notice")
], u);
export {
  u as ContentFreezeLoginNoticeElement,
  P as onInit
};
//# sourceMappingURL=login-notice.js.map
