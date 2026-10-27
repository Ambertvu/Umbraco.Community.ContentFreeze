import { UMB_AUTH_CONTEXT as E } from "@umbraco-cms/backoffice/auth";
import { c as i } from "./client.gen-CrvFvqYF.js";
import { F as T, C as _ } from "./bundle.manifests-DFlBkIXE.js";
const s = "contentFreezeFrozenActions", f = async (r, n) => {
  const e = /* @__PURE__ */ new Set(), o = (t) => {
    e.has(t) || (e.add(t), n.appendCondition(t, { alias: _ }));
  };
  T.forEach(o), n.byType(s).subscribe((t) => t.forEach((c) => c.meta.aliases.forEach(o)));
  const a = await r.getContext(E);
  if (!a) {
    console.warn("UMB_AUTH_CONTEXT not available - Content Freeze API client will not be authenticated");
    return;
  }
  a.configureClient(i);
}, A = () => {
};
export {
  f as onInit,
  A as onUnload
};
//# sourceMappingURL=entrypoint-JX0ilPqM.js.map
