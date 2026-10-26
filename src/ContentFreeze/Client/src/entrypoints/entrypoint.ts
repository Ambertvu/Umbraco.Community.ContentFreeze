import type {
  UmbEntryPointOnInit,
  UmbEntryPointOnUnload,
} from "@umbraco-cms/backoffice/extension-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { client } from "../api/client.gen.js";
import { FROZEN_ACTION_ALIASES } from "../conditions/manifest.js";
import { CONTENT_FREEZE_NOT_FROZEN_CONDITION_ALIAS } from "../conditions/constants.js";

export const onInit: UmbEntryPointOnInit = async (host, extensionRegistry) => {
  // Hide write actions while frozen. appendCondition also applies to extensions registered later on.
  for (const alias of FROZEN_ACTION_ALIASES) {
    extensionRegistry.appendCondition(alias, { alias: CONTENT_FREEZE_NOT_FROZEN_CONDITION_ALIAS });
  }

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
