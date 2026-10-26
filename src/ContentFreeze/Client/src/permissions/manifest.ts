// Must match ContentFreezePermissions in C#.
export const CONTENT_FREEZE_MANAGE_PERMISSION = "ContentFreeze.Manage";
export const CONTENT_FREEZE_BYPASS_PERMISSION = "ContentFreeze.Bypass";

// A dedicated entity type gives these permissions their own box in the user group editor.
const ENTITY_TYPE = "content-freeze";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "entityUserPermission",
    alias: "ContentFreeze.UserPermission.Manage",
    name: "Content Freeze Manage Permission",
    forEntityTypes: [ENTITY_TYPE],
    weight: 200,
    meta: {
      verbs: [CONTENT_FREEZE_MANAGE_PERMISSION],
      label: "Manage freezes",
      description: "Schedule, extend, end and cancel content freezes, from the dashboard or the API.",
      group: "administration",
    },
  },
  {
    type: "entityUserPermission",
    alias: "ContentFreeze.UserPermission.Bypass",
    name: "Content Freeze Bypass Permission",
    forEntityTypes: [ENTITY_TYPE],
    weight: 100,
    meta: {
      verbs: [CONTENT_FREEZE_BYPASS_PERMISSION],
      label: "Edit during a freeze",
      description: "Keep editing content and media while a freeze is active.",
      group: "administration",
    },
  },
  {
    type: "localization",
    alias: "ContentFreeze.Localization.En",
    name: "Content Freeze English",
    meta: {
      culture: "en",
    },
    js: () => import("./en.js"),
  },
];
