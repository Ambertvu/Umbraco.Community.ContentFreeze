import { UmbConditionBase as i } from "@umbraco-cms/backoffice/extension-registry";
import { C as r } from "./freeze.context-UDroHYhG.js";
class p extends i {
  constructor(t, e) {
    super(t, e), this.permitted = !0, this.consumeContext(r, (o) => {
      this.observe(o?.isLocked, (s) => {
        this.permitted = !s;
      }, "_observeLocked");
    });
  }
}
export {
  p as NotFrozenCondition,
  p as api
};
//# sourceMappingURL=not-frozen.condition-DkW7eW7R.js.map
