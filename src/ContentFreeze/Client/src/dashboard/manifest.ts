import { CONTENT_FREEZE_MANAGE_PERMISSION } from "../permissions/manifest.js";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "dashboard",
    alias: "ContentFreeze.Dashboard",
    name: "Content Freeze Dashboard",
    element: () => import("./freeze-dashboard.element.js"),
    weight: 50,
    meta: {
      label: "Content Freeze",
      pathname: "content-freeze",
    },
    conditions: [
      { alias: "Umb.Condition.SectionAlias", match: "Umb.Section.Settings" },
      { alias: "Umb.Condition.UserPermission.Fallback", allOf: [CONTENT_FREEZE_MANAGE_PERMISSION] },
    ],
  },
];
