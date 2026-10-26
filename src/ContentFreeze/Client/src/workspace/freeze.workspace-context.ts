import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_VARIANT_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";
import { CONTENT_FREEZE_CONTEXT } from "../context/freeze.context.js";

const RULE_UNIQUE = "ContentFreeze.ReadOnly";

/**
 * Makes document, media and blueprint workspaces read-only while a freeze blocks the current user.
 */
export class ContentFreezeWorkspaceContext extends UmbControllerBase {
  #locked = false;
  #workspace?: typeof UMB_VARIANT_WORKSPACE_CONTEXT.TYPE;

  constructor(host: UmbControllerHost) {
    super(host);

    this.consumeContext(CONTENT_FREEZE_CONTEXT, (freezeContext) => {
      this.observe(freezeContext?.isLocked, (locked) => {
        this.#locked = locked ?? false;
        this.#apply();
      }, "_observeLocked");
    });

    this.consumeContext(UMB_VARIANT_WORKSPACE_CONTEXT, (workspace) => {
      this.#workspace = workspace;

      // The workspace clears all guard rules whenever it loads an item, so re-apply after each load.
      this.observe(workspace?.variantOptions, () => this.#apply(), "_observeVariantOptions");
    });
  }

  #apply() {
    const guard = this.#workspace?.readOnlyGuard;
    if (!guard) {
      return;
    }

    if (this.#locked) {
      // For the read-only guard, "permitted" means "permitted to be read-only".
      guard.addRule({ unique: RULE_UNIQUE, permitted: true, message: "Editing is temporarily blocked by a content freeze." });
    } else {
      guard.removeRule(RULE_UNIQUE);
    }
  }
}

export { ContentFreezeWorkspaceContext as api };
