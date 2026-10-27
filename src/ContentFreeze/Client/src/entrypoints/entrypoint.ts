import type {
  ManifestBase,
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "../api/client.gen.js";
import { FROZEN_ACTION_ALIASES } from "../conditions/manifest.js";
import { CONTENT_FREEZE_NOT_FROZEN_CONDITION_ALIAS } from "../conditions/constants.js";

const CONTENT_FREEZE_FROZEN_ACTIONS_TYPE = "contentFreezeFrozenActions";

interface ManifestFrozenActions extends ManifestBase {
  type: typeof CONTENT_FREEZE_FROZEN_ACTIONS_TYPE;
  meta: { aliases: Array<string> };
}

export const onInit: UmbEntryPointOnInit = async (host, extensionRegistry) => {
  // Hide write actions while frozen. appendCondition also applies to extensions registered later on.
  const frozen = new Set<string>();
  const freeze = (alias: string) => {
    if (frozen.has(alias)) return;
    frozen.add(alias);
    extensionRegistry.appendCondition(alias, { alias: CONTENT_FREEZE_NOT_FROZEN_CONDITION_ALIAS });
  };

  FROZEN_ACTION_ALIASES.forEach(freeze);

  // Add-ons list their own actions from C# (IFrozenActionsProvider), see FrozenActionsManifestReader.
  extensionRegistry
    .byType<typeof CONTENT_FREEZE_FROZEN_ACTIONS_TYPE, ManifestFrozenActions>(CONTENT_FREEZE_FROZEN_ACTIONS_TYPE)
    .subscribe((manifests) => manifests.forEach((manifest) => manifest.meta.aliases.forEach(freeze)));

  // Wire the generated API client into the backoffice auth context.
  // configureClient() sets baseUrl + credentials, attaches the auth callback
  // (cookie-based, with automatic token refresh) and binds the default
  // response interceptors (401 retry, error notifications, etc.).
  // The framework awaits onInit, so resolving the context here ensures the
  // client is fully configured before any element in this extension can use it.
  const authContext = await host.getContext(UMB_AUTH_CONTEXT);
  if (!authContext) {
    console.warn("UMB_AUTH_CONTEXT not available - Content Freeze API client will not be authenticated");
    return;
  }
  authContext.configureClient(client);
};

export const onUnload: UmbEntryPointOnUnload = () => {};
