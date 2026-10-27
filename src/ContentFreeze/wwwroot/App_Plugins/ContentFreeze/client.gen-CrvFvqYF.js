import { umbHttpClient as G } from "@umbraco-cms/backoffice/http-client";
const M = (e) => ({
  ...e,
  ...G.getConfig()
}), Q = {
  bodySerializer: (e) => JSON.stringify(e, (t, r) => typeof r == "bigint" ? r.toString() : r)
};
function K({
  onRequest: e,
  onSseError: t,
  onSseEvent: r,
  responseTransformer: a,
  responseValidator: i,
  sseDefaultRetryDelay: c,
  sseMaxRetryAttempts: n,
  sseMaxRetryDelay: s,
  sseSleepFn: o,
  url: m,
  ...f
}) {
  let l;
  const g = o ?? ((d) => new Promise((h) => setTimeout(h, d)));
  return { stream: async function* () {
    let d = c ?? 3e3, h = 0;
    const w = f.signal ?? new AbortController().signal;
    for (; !w.aborted; ) {
      h++;
      const C = f.headers instanceof Headers ? f.headers : new Headers(f.headers);
      l !== void 0 && C.set("Last-Event-ID", l);
      try {
        const E = {
          redirect: "follow",
          ...f,
          body: f.serializedBody,
          headers: C,
          signal: w
        };
        let x = new Request(m, E);
        e && (x = await e(m, E));
        const y = await (f.fetch ?? globalThis.fetch)(x);
        if (!y.ok) throw new Error(`SSE failed: ${y.status} ${y.statusText}`);
        if (!y.body) throw new Error("No body in SSE response");
        const S = y.body.pipeThrough(new TextDecoderStream()).getReader();
        let p = "";
        const T = () => {
          try {
            S.cancel();
          } catch {
          }
        };
        w.addEventListener("abort", T);
        try {
          for (; ; ) {
            const { done: L, value: J } = await S.read();
            if (L) break;
            p += J, p = p.replace(/\r\n?/g, `
`);
            const $ = p.split(`

`);
            p = $.pop() ?? "";
            for (const _ of $) {
              const F = _.split(`
`), O = [];
              let v;
              for (const j of F)
                if (j.startsWith("data:"))
                  O.push(j.replace(/^data:\s*/, ""));
                else if (j.startsWith("event:"))
                  v = j.replace(/^event:\s*/, "");
                else if (j.startsWith("id:"))
                  l = j.replace(/^id:\s*/, "");
                else if (j.startsWith("retry:")) {
                  const B = Number.parseInt(j.replace(/^retry:\s*/, ""), 10);
                  Number.isNaN(B) || (d = B);
                }
              let z, I = !1;
              if (O.length) {
                const j = O.join(`
`);
                try {
                  z = JSON.parse(j), I = !0;
                } catch {
                  z = j;
                }
              }
              I && (i && await i(z), a && (z = await a(z))), r?.({
                data: z,
                event: v,
                id: l,
                retry: d
              }), O.length && (yield z);
            }
          }
        } finally {
          w.removeEventListener("abort", T), S.releaseLock();
        }
        break;
      } catch (E) {
        if (t?.(E), n !== void 0 && h >= n)
          break;
        const x = Math.min(d * 2 ** (h - 1), s ?? 3e4);
        await g(x);
      }
    }
  }() };
}
const X = (e) => {
  switch (e) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
}, Y = (e) => {
  switch (e) {
    case "form":
      return ",";
    case "pipeDelimited":
      return "|";
    case "spaceDelimited":
      return "%20";
    default:
      return ",";
  }
}, Z = (e) => {
  switch (e) {
    case "label":
      return ".";
    case "matrix":
      return ";";
    case "simple":
      return ",";
    default:
      return "&";
  }
}, D = ({
  allowReserved: e,
  explode: t,
  name: r,
  style: a,
  value: i
}) => {
  if (!t) {
    const s = (e ? i : i.map((o) => encodeURIComponent(o))).join(Y(a));
    switch (a) {
      case "label":
        return `.${s}`;
      case "matrix":
        return `;${r}=${s}`;
      case "simple":
        return s;
      default:
        return `${r}=${s}`;
    }
  }
  const c = X(a), n = i.map((s) => a === "label" || a === "simple" ? e ? s : encodeURIComponent(s) : q({
    allowReserved: e,
    name: r,
    value: s
  })).join(c);
  return a === "label" || a === "matrix" ? c + n : n;
}, q = ({
  allowReserved: e,
  name: t,
  value: r
}) => {
  if (r == null)
    return "";
  if (typeof r == "object")
    throw new Error(
      "Deeply-nested arrays/objects aren’t supported. Provide your own `querySerializer()` to handle these."
    );
  return `${t}=${e ? r : encodeURIComponent(r)}`;
}, P = ({
  allowReserved: e,
  explode: t,
  name: r,
  style: a,
  value: i,
  valueOnly: c
}) => {
  if (i instanceof Date)
    return c ? i.toISOString() : `${r}=${i.toISOString()}`;
  if (a !== "deepObject" && !t) {
    let o = [];
    Object.entries(i).forEach(([f, l]) => {
      o = [...o, f, e ? l : encodeURIComponent(l)];
    });
    const m = o.join(",");
    switch (a) {
      case "form":
        return `${r}=${m}`;
      case "label":
        return `.${m}`;
      case "matrix":
        return `;${r}=${m}`;
      default:
        return m;
    }
  }
  const n = Z(a), s = Object.entries(i).map(
    ([o, m]) => q({
      allowReserved: e,
      name: a === "deepObject" ? `${r}[${o}]` : o,
      value: m
    })
  ).join(n);
  return a === "label" || a === "matrix" ? n + s : s;
}, ee = /\{[^{}]+\}/g, te = ({ path: e, url: t }) => {
  let r = t;
  const a = t.match(ee);
  if (a)
    for (const i of a) {
      let c = !1, n = i.substring(1, i.length - 1), s = "simple";
      n.endsWith("*") && (c = !0, n = n.substring(0, n.length - 1)), n.startsWith(".") ? (n = n.substring(1), s = "label") : n.startsWith(";") && (n = n.substring(1), s = "matrix");
      const o = e[n];
      if (o == null)
        continue;
      if (Array.isArray(o)) {
        r = r.replace(i, D({ explode: c, name: n, style: s, value: o }));
        continue;
      }
      if (typeof o == "object") {
        r = r.replace(
          i,
          P({
            explode: c,
            name: n,
            style: s,
            value: o,
            valueOnly: !0
          })
        );
        continue;
      }
      if (s === "matrix") {
        r = r.replace(
          i,
          `;${q({
            name: n,
            value: o
          })}`
        );
        continue;
      }
      const m = encodeURIComponent(
        s === "label" ? `.${o}` : o
      );
      r = r.replace(i, m);
    }
  return r;
}, re = ({
  baseUrl: e,
  path: t,
  query: r,
  querySerializer: a,
  url: i
}) => {
  const c = i.startsWith("/") ? i : `/${i}`;
  let n = (e ?? "") + c;
  t && (n = te({ path: t, url: n }));
  let s = r ? a(r) : "";
  return s.startsWith("?") && (s = s.substring(1)), s && (n += `?${s}`), n;
};
function U(e) {
  const t = e.body !== void 0;
  if (t && e.bodySerializer)
    return "serializedBody" in e ? e.serializedBody !== void 0 && e.serializedBody !== "" ? e.serializedBody : null : e.body !== "" ? e.body : null;
  if (t)
    return e.body;
}
const se = async (e, t) => {
  const r = typeof t == "function" ? await t(e) : t;
  if (r)
    return e.scheme === "bearer" ? `Bearer ${r}` : e.scheme === "basic" ? `Basic ${btoa(r)}` : r;
}, H = ({
  parameters: e = {},
  ...t
} = {}) => (a) => {
  const i = [];
  if (a && typeof a == "object")
    for (const c in a) {
      const n = a[c];
      if (n == null)
        continue;
      const s = e[c] || t;
      if (Array.isArray(n)) {
        const o = D({
          allowReserved: s.allowReserved,
          explode: !0,
          name: c,
          style: "form",
          value: n,
          ...s.array
        });
        o && i.push(o);
      } else if (typeof n == "object") {
        const o = P({
          allowReserved: s.allowReserved,
          explode: !0,
          name: c,
          style: "deepObject",
          value: n,
          ...s.object
        });
        o && i.push(o);
      } else {
        const o = q({
          allowReserved: s.allowReserved,
          name: c,
          value: n
        });
        o && i.push(o);
      }
    }
  return i.join("&");
}, ae = (e) => {
  if (!e)
    return "stream";
  const t = e.split(";")[0]?.trim();
  if (t) {
    if (t.startsWith("application/json") || t.endsWith("+json"))
      return "json";
    if (t === "multipart/form-data")
      return "formData";
    if (["application/", "audio/", "image/", "video/"].some((r) => t.startsWith(r)))
      return "blob";
    if (t.startsWith("text/"))
      return "text";
  }
}, ne = (e, t) => t ? !!(e.headers.has(t) || e.query?.[t] || e.headers.get("Cookie")?.includes(`${t}=`)) : !1;
async function oe(e) {
  for (const t of e.security ?? []) {
    if (ne(e, t.name))
      continue;
    const r = await se(t, e.auth);
    if (!r)
      continue;
    const a = t.name ?? "Authorization";
    switch (t.in) {
      case "query":
        e.query || (e.query = {}), e.query[a] = r;
        break;
      case "cookie":
        e.headers.append("Cookie", `${a}=${r}`);
        break;
      default:
        e.headers.set(a, r);
        break;
    }
  }
}
const N = (e) => re({
  baseUrl: e.baseUrl,
  path: e.path,
  query: e.query,
  querySerializer: typeof e.querySerializer == "function" ? e.querySerializer : H(e.querySerializer),
  url: e.url
}), R = (e, t) => {
  const r = { ...e, ...t };
  return r.baseUrl?.endsWith("/") && (r.baseUrl = r.baseUrl.substring(0, r.baseUrl.length - 1)), r.headers = W(e.headers, t.headers), r;
}, ie = (e) => {
  const t = [];
  return e.forEach((r, a) => {
    t.push([a, r]);
  }), t;
}, W = (...e) => {
  const t = new Headers();
  for (const r of e) {
    if (!r)
      continue;
    const a = r instanceof Headers ? ie(r) : Object.entries(r);
    for (const [i, c] of a)
      if (c === null)
        t.delete(i);
      else if (Array.isArray(c))
        for (const n of c)
          t.append(i, n);
      else c !== void 0 && t.set(
        i,
        typeof c == "object" ? JSON.stringify(c) : c
      );
  }
  return t;
};
class A {
  constructor() {
    this.fns = [];
  }
  clear() {
    this.fns = [];
  }
  eject(t) {
    const r = this.getInterceptorIndex(t);
    this.fns[r] && (this.fns[r] = null);
  }
  exists(t) {
    const r = this.getInterceptorIndex(t);
    return !!this.fns[r];
  }
  getInterceptorIndex(t) {
    return typeof t == "number" ? this.fns[t] ? t : -1 : this.fns.indexOf(t);
  }
  update(t, r) {
    const a = this.getInterceptorIndex(t);
    return this.fns[a] ? (this.fns[a] = r, t) : !1;
  }
  use(t) {
    return this.fns.push(t), this.fns.length - 1;
  }
}
const ce = () => ({
  error: new A(),
  request: new A(),
  response: new A()
}), le = H({
  allowReserved: !1,
  array: {
    explode: !0,
    style: "form"
  },
  object: {
    explode: !0,
    style: "deepObject"
  }
}), fe = {
  "Content-Type": "application/json"
}, V = (e = {}) => ({
  ...Q,
  headers: fe,
  parseAs: "auto",
  querySerializer: le,
  ...e
}), de = (e = {}) => {
  let t = R(V(), e);
  const r = () => ({ ...t }), a = (f) => (t = R(t, f), r()), i = ce(), c = async (f) => {
    const l = {
      ...t,
      ...f,
      fetch: f.fetch ?? t.fetch ?? globalThis.fetch,
      headers: W(t.headers, f.headers),
      serializedBody: void 0
    };
    l.security && await oe(l), l.requestValidator && await l.requestValidator(l), l.body !== void 0 && l.bodySerializer && (l.serializedBody = l.bodySerializer(l.body)), (l.body === void 0 || l.serializedBody === "") && l.headers.delete("Content-Type");
    const g = l, b = N(g);
    return { opts: g, url: b };
  }, n = async (f) => {
    const l = f.throwOnError ?? t.throwOnError, g = f.responseStyle ?? t.responseStyle;
    let b, u;
    try {
      const { opts: d, url: h } = await c(f), w = {
        redirect: "follow",
        ...d,
        body: U(d)
      };
      b = new Request(h, w);
      for (const y of i.request.fns)
        y && (b = await y(b, d));
      const C = d.fetch;
      u = await C(b);
      for (const y of i.response.fns)
        y && (u = await y(u, b, d));
      const E = {
        request: b,
        response: u
      };
      if (u.ok) {
        const y = (d.parseAs === "auto" ? ae(u.headers.get("Content-Type")) : d.parseAs) ?? "json";
        if (u.status === 204 || u.headers.get("Content-Length") === "0") {
          let p;
          switch (y) {
            case "arrayBuffer":
            case "blob":
            case "text":
              p = await u[y]();
              break;
            case "formData":
              p = new FormData();
              break;
            case "stream":
              p = u.body;
              break;
            default:
              p = {};
              break;
          }
          return d.responseStyle === "data" ? p : {
            data: p,
            ...E
          };
        }
        let S;
        switch (y) {
          case "arrayBuffer":
          case "blob":
          case "formData":
          case "text":
            S = await u[y]();
            break;
          case "json": {
            const p = await u.text();
            S = p ? JSON.parse(p) : {};
            break;
          }
          case "stream":
            return d.responseStyle === "data" ? u.body : {
              data: u.body,
              ...E
            };
        }
        return y === "json" && (d.responseValidator && await d.responseValidator(S), d.responseTransformer && (S = await d.responseTransformer(S))), d.responseStyle === "data" ? S : {
          data: S,
          ...E
        };
      }
      const x = await u.text();
      let k;
      try {
        k = JSON.parse(x);
      } catch {
      }
      throw k ?? x;
    } catch (d) {
      let h = d;
      for (const w of i.error.fns)
        w && (h = await w(h, u, b, f));
      if (h = h || {}, l)
        throw h;
      return g === "data" ? void 0 : {
        error: h,
        request: b,
        response: u
      };
    }
  }, s = (f) => (l) => n({ ...l, method: f }), o = (f) => async (l) => {
    const { opts: g, url: b } = await c(l);
    return K({
      ...g,
      body: g.body,
      method: f,
      onRequest: async (u, d) => {
        let h = new Request(u, d);
        for (const w of i.request.fns)
          w && (h = await w(h, g));
        return h;
      },
      serializedBody: U(g),
      url: b
    });
  };
  return {
    buildUrl: (f) => N({ ...t, ...f }),
    connect: s("CONNECT"),
    delete: s("DELETE"),
    get: s("GET"),
    getConfig: r,
    head: s("HEAD"),
    interceptors: i,
    options: s("OPTIONS"),
    patch: s("PATCH"),
    post: s("POST"),
    put: s("PUT"),
    request: n,
    setConfig: a,
    sse: {
      connect: o("CONNECT"),
      delete: o("DELETE"),
      get: o("GET"),
      head: o("HEAD"),
      options: o("OPTIONS"),
      patch: o("PATCH"),
      post: o("POST"),
      put: o("PUT"),
      trace: o("TRACE")
    },
    trace: s("TRACE")
  };
}, he = de(M(V({ baseUrl: "https://localhost:44398/" })));
export {
  he as c
};
//# sourceMappingURL=client.gen-CrvFvqYF.js.map
