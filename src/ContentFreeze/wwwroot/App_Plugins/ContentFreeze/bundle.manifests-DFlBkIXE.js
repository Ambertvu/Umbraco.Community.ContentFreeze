const o = "ContentFreeze.Condition.NotFrozen", i = [
  {
    type: "condition",
    alias: o,
    name: "Content Freeze Not Frozen Condition",
    api: () => import("./not-frozen.condition-ce2P6R-O.js")
  }
], y = [
  // Document workspace
  "Umb.WorkspaceAction.Document.Save",
  "Umb.WorkspaceAction.Document.SaveAndPublish",
  "Umb.WorkspaceAction.Document.SaveAndPreview",
  "Umb.Document.WorkspaceActionMenuItem.PublishWithDescendants",
  "Umb.Document.WorkspaceActionMenuItem.SchedulePublishing",
  "Umb.Document.WorkspaceActionMenuItem.Unpublish",
  "Umb.Document.WorkspaceActionMenuItem.SaveAndPreview",
  // Document entity, bulk and collection actions
  "Umb.EntityAction.Document.Create",
  "Umb.EntityAction.Document.CreateBlueprint",
  "Umb.EntityAction.Document.CultureAndHostnames",
  "Umb.EntityAction.Document.DuplicateTo",
  "Umb.EntityAction.Document.MoveTo",
  "Umb.EntityAction.Document.SortChildrenOf",
  "Umb.EntityAction.Document.Publish",
  "Umb.EntityAction.Document.Unpublish",
  "Umb.EntityAction.Document.Rollback",
  // The Rollback button in the workspace's history box (newer 17.x versions)
  "Umb.AuditLogAction.Document.Rollback",
  "Umb.EntityAction.Document.PublicAccess",
  "Umb.EntityAction.Document.RecycleBin.Trash",
  "Umb.EntityAction.Document.RecycleBin.Restore",
  "Umb.EntityAction.Document.RecycleBin.Empty",
  "Umb.EntityAction.Document.Delete",
  "Umb.EntityBulkAction.Document.DuplicateTo",
  "Umb.EntityBulkAction.Document.MoveTo",
  "Umb.EntityBulkAction.Document.Publish",
  "Umb.EntityBulkAction.Document.Unpublish",
  "Umb.EntityBulkAction.Document.Trash",
  "Umb.CollectionAction.Document.Create",
  "Umb.CollectionAction.Document.EmptyRecycleBin",
  // Media
  "Umb.WorkspaceAction.Media.Save",
  "Umb.EntityAction.Media.Create",
  "Umb.EntityAction.Media.MoveTo",
  "Umb.EntityAction.Media.SortChildrenOf",
  "Umb.EntityAction.Media.RecycleBin.Trash",
  "Umb.EntityAction.Media.RecycleBin.Restore",
  "Umb.EntityAction.Media.RecycleBin.Empty",
  "Umb.EntityAction.Media.Delete",
  "Umb.EntityBulkAction.Media.MoveTo",
  "Umb.EntityBulkAction.Media.Trash",
  "Umb.CollectionAction.Media.Create",
  "Umb.CollectionAction.Media.EmptyRecycleBin",
  // Document blueprints
  "Umb.WorkspaceAction.DocumentBlueprint.Save",
  "Umb.EntityAction.DocumentBlueprint.Create",
  "Umb.EntityAction.DocumentBlueprint.MoveTo",
  "Umb.EntityAction.DocumentBlueprint.Folder.Rename",
  "Umb.EntityAction.DocumentBlueprint.Folder.Delete",
  "Umb.EntityAction.DocumentBlueprintItem.Delete",
  // Dictionary
  "Umb.WorkspaceAction.Dictionary.Save",
  "Umb.EntityAction.Dictionary.Create",
  "Umb.EntityAction.Dictionary.Delete",
  "Umb.EntityAction.Dictionary.MoveTo",
  "Umb.EntityAction.Dictionary.Import",
  "Umb.CollectionAction.Dictionary.Create"
], c = [
  {
    type: "globalContext",
    alias: "ContentFreeze.GlobalContext",
    name: "Content Freeze Global Context",
    api: () => import("./freeze.context-BxXhCb2B.js").then((n) => n.c)
  }
], t = "ContentFreeze.Manage", a = "ContentFreeze.Bypass", e = "content-freeze", m = [
  {
    type: "entityUserPermission",
    alias: "ContentFreeze.UserPermission.Manage",
    name: "Content Freeze Manage Permission",
    forEntityTypes: [e],
    weight: 200,
    meta: {
      verbs: [t],
      label: "Manage freezes",
      description: "Schedule, extend, end and cancel content freezes, from the dashboard or the API.",
      group: "administration"
    }
  },
  {
    type: "entityUserPermission",
    alias: "ContentFreeze.UserPermission.Bypass",
    name: "Content Freeze Bypass Permission",
    forEntityTypes: [e],
    weight: 100,
    meta: {
      verbs: [a],
      label: "Edit during a freeze",
      description: "Keep editing content and media while a freeze is active.",
      group: "administration"
    }
  },
  {
    type: "localization",
    alias: "ContentFreeze.Localization.En",
    name: "Content Freeze English",
    meta: {
      culture: "en"
    },
    js: () => import("./en-CqVsBEG1.js")
  }
], s = [
  {
    type: "dashboard",
    alias: "ContentFreeze.Dashboard",
    name: "Content Freeze Dashboard",
    element: () => import("./freeze-dashboard.element-DuJ2dcoi.js"),
    weight: 50,
    meta: {
      label: "Content Freeze",
      pathname: "content-freeze"
    },
    conditions: [
      { alias: "Umb.Condition.SectionAlias", match: "Umb.Section.Settings" },
      { alias: "Umb.Condition.UserPermission.Fallback", allOf: [t] }
    ]
  }
], r = [
  {
    name: "Content Freeze Entrypoint",
    alias: "ContentFreeze.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint-JX0ilPqM.js")
  }
], l = [
  {
    type: "headerApp",
    alias: "ContentFreeze.HeaderApp",
    name: "Content Freeze Header App",
    element: () => import("./freeze-header-app.element-3d0GmU1_.js"),
    // Left of search (900), so it is the first thing in the top bar.
    weight: 950
  }
], b = [
  {
    type: "workspaceContext",
    alias: "ContentFreeze.WorkspaceContext.ReadOnly",
    name: "Content Freeze Read-only Workspace Context",
    api: () => import("./freeze.workspace-context-BrEYbOe-.js"),
    conditions: [
      {
        alias: "Umb.Condition.WorkspaceAlias",
        oneOf: ["Umb.Workspace.Document", "Umb.Workspace.Media", "Umb.Workspace.DocumentBlueprint"]
      }
    ]
  }
], u = [
  ...r,
  ...c,
  ...i,
  ...l,
  ...b,
  ...m,
  ...s
];
export {
  o as C,
  y as F,
  u as m
};
//# sourceMappingURL=bundle.manifests-DFlBkIXE.js.map
