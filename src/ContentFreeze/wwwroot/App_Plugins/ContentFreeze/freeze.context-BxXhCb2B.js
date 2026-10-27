import { UmbContextBase as z } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken as O } from "@umbraco-cms/backoffice/context-api";
import { UMB_NOTIFICATION_CONTEXT as $ } from "@umbraco-cms/backoffice/notification";
import { UmbObjectState as y, UmbBooleanState as E, UmbNumberState as A } from "@umbraco-cms/backoffice/observable-api";
import { tryExecute as h } from "@umbraco-cms/backoffice/resources";
import { c as a } from "./client.gen-CrvFvqYF.js";
class c {
  static schedule(e) {
    return (e.client ?? a).post({
      security: [{ scheme: "bearer", type: "http" }],
      url: "/umbraco/contentfreeze/api/v1/freeze",
      ...e,
      headers: {
        "Content-Type": "application/json",
        ...e.headers
      }
    });
  }
  static cancel(e) {
    return (e.client ?? a).delete({
      security: [{ scheme: "bearer", type: "http" }],
      url: "/umbraco/contentfreeze/api/v1/freeze/{id}",
      ...e
    });
  }
  static get(e) {
    return (e.client ?? a).get({
      security: [{ scheme: "bearer", type: "http" }],
      url: "/umbraco/contentfreeze/api/v1/freeze/{id}",
      ...e
    });
  }
  static assertActive(e) {
    return (e.client ?? a).get({
      security: [{ scheme: "bearer", type: "http" }],
      url: "/umbraco/contentfreeze/api/v1/freeze/{id}/assert-active",
      ...e
    });
  }
  static details(e) {
    return (e.client ?? a).get({
      security: [{ scheme: "bearer", type: "http" }],
      url: "/umbraco/contentfreeze/api/v1/freeze/{id}/details",
      ...e
    });
  }
  static end(e) {
    return (e.client ?? a).post({
      security: [{ scheme: "bearer", type: "http" }],
      url: "/umbraco/contentfreeze/api/v1/freeze/{id}/end",
      ...e,
      headers: {
        "Content-Type": "application/json",
        ...e.headers
      }
    });
  }
  static extend(e) {
    return (e.client ?? a).post({
      security: [{ scheme: "bearer", type: "http" }],
      url: "/umbraco/contentfreeze/api/v1/freeze/{id}/extend",
      ...e,
      headers: {
        "Content-Type": "application/json",
        ...e.headers
      }
    });
  }
  static history(e) {
    return (e?.client ?? a).get({
      security: [{ scheme: "bearer", type: "http" }],
      url: "/umbraco/contentfreeze/api/v1/freeze/history",
      ...e
    });
  }
  static loginStatus(e) {
    return (e?.client ?? a).get({ url: "/umbraco/contentfreeze/api/v1/freeze/login-status", ...e });
  }
  static status(e) {
    return (e?.client ?? a).get({
      security: [{ scheme: "bearer", type: "http" }],
      url: "/umbraco/contentfreeze/api/v1/freeze/status",
      ...e
    });
  }
}
const C = 3e4, p = [5, 1], g = "ContentFreeze.Announced";
class v extends z {
  constructor(e) {
    super(e, w), this.#e = new y(void 0), this.status = this.#e.asObservable(), this.freeze = this.#e.asObservablePart((t) => t?.freeze ?? void 0), this.canManage = this.#e.asObservablePart((t) => t?.canManage ?? !1), this.#a = new y("none"), this.phase = this.#a.asObservable(), this.#n = new E(!1), this.isLocked = this.#n.asObservable(), this.#r = new A(0), this.secondsRemaining = this.#r.asObservable(), this.#c = 0, this.#t = this.#p(), this.#s = "none", this.#o = () => {
      document.visibilityState === "visible" && this.refresh();
    }, this.refresh(), this.#h = window.setInterval(() => this.refresh(), C), this.#u = window.setInterval(() => this.#d(), 1e3), document.addEventListener("visibilitychange", this.#o);
  }
  #e;
  #a;
  #n;
  #r;
  #c;
  #h;
  #u;
  #t;
  #l;
  #s;
  /** The current time on the server's clock. */
  now() {
    return Date.now() + this.#c;
  }
  async refresh() {
    const e = await c.status().then((t) => t.data).catch(() => {
    });
    e && (this.#c = Date.parse(e.serverTime) - Date.now(), this.#e.setValue(e), this.#d());
  }
  async schedule(e) {
    const t = await h(this, c.schedule({ body: e }));
    return await this.refresh(), t;
  }
  async extend(e, t, s) {
    const i = await h(this, c.extend({ path: { id: String(e) }, body: { minutes: t, reason: s } }));
    return await this.refresh(), i;
  }
  async end(e, t) {
    const s = await h(this, c.end({ path: { id: String(e) }, body: { reason: t } }));
    return await this.refresh(), s;
  }
  async cancel(e, t) {
    const s = await h(this, c.cancel({ path: { id: String(e) }, query: { reason: t } }));
    return await this.refresh(), s;
  }
  getPhase(e, t = this.now()) {
    if (!e)
      return "none";
    const s = Date.parse(e.startsAt), i = Date.parse(e.endsAt);
    return t >= i ? "none" : t >= s ? "active" : s - t <= p[0] * 6e4 ? "warning" : "scheduled";
  }
  #d() {
    const e = this.#e.getValue(), t = e?.freeze ?? void 0, s = this.now(), i = this.getPhase(t, s);
    this.#a.setValue(i), this.#n.setValue(i === "active" && !e?.canBypass && !e?.enforcementDisabled);
    let r = 0;
    if (t && i !== "none") {
      const o = i === "active" ? Date.parse(t.endsAt) : Date.parse(t.startsAt);
      r = Math.max(0, Math.ceil((o - s) / 1e3));
    }
    this.#r.setValue(r), this.#m(t, i, s), this.#l === t?.key && this.#s !== i && (i === "active" || i === "none") && window.setTimeout(() => this.refresh(), 2e3), this.#l = t?.key, this.#s = i;
  }
  async #m(e, t, s) {
    const r = this.#e.getValue()?.canBypass ? " You can keep editing: your user group may bypass the freeze." : "", o = e?.reason ? ` (${e.reason})` : "";
    if (!e) {
      this.#s === "active" && await this.#i("positive", "Editing is available again", "The content freeze has ended.");
      return;
    }
    if (t === "none" && this.#s === "active") {
      await this.#i("positive", "Editing is available again", "The content freeze has ended.");
      return;
    }
    if (t === "active") {
      this.#f(e, "active") && await this.#i(
        "danger",
        "Editing is temporarily blocked",
        `Content and media cannot be changed${o} until about ${u(e.endsAt)}.${r}`
      );
      return;
    }
    const l = (Date.parse(e.startsAt) - s) / 6e4, d = p.filter((m) => l <= m), f = d.length > 0 ? d[d.length - 1] : void 0, T = f === void 0 ? "scheduled" : `warn-${f}`;
    if (this.#f(e, T)) {
      for (const b of p)
        l <= b && this.#t.add(`${e.key}:warn-${b}`);
      this.#t.add(`${e.key}:scheduled`);
      const m = f === void 0 ? `at ${u(e.startsAt)}` : `in ${S(Math.ceil(l * 60))}`;
      await this.#i(
        "warning",
        "Editing will be blocked soon",
        `Editing will be blocked ${m}${o}, until about ${u(e.endsAt)}. Save your work.${r}`
      );
    }
  }
  #f(e, t) {
    const s = `${e.key}:${t}`;
    return this.#t.has(s) ? !1 : (this.#t.add(s), this.#b(), !0);
  }
  // Remembered per browser tab, so a page reload does not repeat toasts the editor has already seen.
  #p() {
    try {
      const e = sessionStorage.getItem(g);
      return new Set(e ? JSON.parse(e) : []);
    } catch {
      return /* @__PURE__ */ new Set();
    }
  }
  #b() {
    try {
      sessionStorage.setItem(g, JSON.stringify([...this.#t].slice(-50)));
    } catch {
    }
  }
  async #i(e, t, s) {
    (await this.getContext($))?.peek(e, { data: { headline: t, message: s } });
  }
  #o;
  destroy() {
    window.clearInterval(this.#h), window.clearInterval(this.#u), document.removeEventListener("visibilitychange", this.#o), super.destroy();
  }
}
const w = new O("ContentFreezeContext");
function u(n) {
  return new Date(n).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}
function k(n) {
  return new Date(n).toLocaleString([], { dateStyle: "medium", timeStyle: "short" });
}
function S(n) {
  const e = Math.floor(n / 3600), t = Math.floor(n % 3600 / 60), s = n % 60;
  return e > 0 ? `${e}:${String(t).padStart(2, "0")}:${String(s).padStart(2, "0")}` : `${t}:${String(s).padStart(2, "0")}`;
}
const L = /* @__PURE__ */ Object.freeze(/* @__PURE__ */ Object.defineProperty({
  __proto__: null,
  CONTENT_FREEZE_CONTEXT: w,
  ContentFreezeContext: v,
  api: v,
  formatDateTime: k,
  formatDuration: S,
  formatTime: u
}, Symbol.toStringTag, { value: "Module" }));
export {
  w as C,
  c as F,
  k as a,
  u as b,
  L as c,
  S as f
};
//# sourceMappingURL=freeze.context-BxXhCb2B.js.map
