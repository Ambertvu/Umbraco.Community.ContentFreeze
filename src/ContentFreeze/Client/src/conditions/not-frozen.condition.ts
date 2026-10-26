import type {
  UmbConditionConfigBase,
  UmbConditionControllerArguments,
  UmbExtensionCondition,
} from "@umbraco-cms/backoffice/extension-api";
import { UmbConditionBase } from "@umbraco-cms/backoffice/extension-registry";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { CONTENT_FREEZE_CONTEXT } from "../context/freeze.context.js";
import { CONTENT_FREEZE_NOT_FROZEN_CONDITION_ALIAS } from "./constants.js";

export type NotFrozenConditionConfig = UmbConditionConfigBase<typeof CONTENT_FREEZE_NOT_FROZEN_CONDITION_ALIAS>;

/** Permitted unless a freeze currently blocks the current user. */
export class NotFrozenCondition extends UmbConditionBase<NotFrozenConditionConfig> implements UmbExtensionCondition {
  constructor(host: UmbControllerHost, args: UmbConditionControllerArguments<NotFrozenConditionConfig>) {
    super(host, args);
    this.permitted = true;

    this.consumeContext(CONTENT_FREEZE_CONTEXT, (context) => {
      this.observe(context?.isLocked, (locked) => {
        this.permitted = !locked;
      }, "_observeLocked");
    });
  }
}

export { NotFrozenCondition as api };

declare global {
  interface UmbExtensionConditionConfigMap {
    ContentFreezeNotFrozen: NotFrozenConditionConfig;
  }
}
