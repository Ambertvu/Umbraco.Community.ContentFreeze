import { UMB_AUTH_CONTEXT as e } from "@umbraco-cms/backoffice/auth";
import { c as i } from "./client.gen-CtZ_TvPO.js";
import { F as r, C as c } from "./bundle.manifests-BkEayc_2.js";
const T = async (n, o) => {
  for (const a of r)
    o.appendCondition(a, { alias: c });
  const t = await n.getContext(e);
  if (!t) {
    console.warn("UMB_AUTH_CONTEXT not available - Content Freeze API client will not be authenticated");
    return;
  }
  t.configureClient(i);
}, _ = () => {
};
export {
  T as onInit,
  _ as onUnload
};
//# sourceMappingURL=entrypoint-OPxoXwrr.js.map
