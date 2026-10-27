import { UmbControllerBase as r } from "@umbraco-cms/backoffice/class-api";
import { UMB_VARIANT_WORKSPACE_CONTEXT as i } from "@umbraco-cms/backoffice/workspace";
import { C as n } from "./freeze.context-UDroHYhG.js";
const s = "ContentFreeze.ReadOnly";
class m extends r {
  #e = !1;
  #t;
  constructor(e) {
    super(e), this.consumeContext(n, (t) => {
      this.observe(t?.isLocked, (o) => {
        this.#e = o ?? !1, this.#s();
      }, "_observeLocked");
    }), this.consumeContext(i, (t) => {
      this.#t = t, this.observe(t?.variantOptions, () => this.#s(), "_observeVariantOptions");
    });
  }
  #s() {
    const e = this.#t?.readOnlyGuard;
    e && (this.#e ? e.addRule({ unique: s, permitted: !0, message: "Editing is temporarily blocked by a content freeze." }) : e.removeRule(s));
  }
}
export {
  m as ContentFreezeWorkspaceContext,
  m as api
};
//# sourceMappingURL=freeze.workspace-context-DL2ggEAV.js.map
