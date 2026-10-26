export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "workspaceContext",
    alias: "ContentFreeze.WorkspaceContext.ReadOnly",
    name: "Content Freeze Read-only Workspace Context",
    api: () => import("./freeze.workspace-context.js"),
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        oneOf: ["Umb.Workspace.Document", "Umb.Workspace.Media", "Umb.Workspace.DocumentBlueprint"],
      },
    ],
  },
];
