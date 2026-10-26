import { CONTENT_FREEZE_NOT_FROZEN_CONDITION_ALIAS } from "./constants.js";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "condition",
    alias: CONTENT_FREEZE_NOT_FROZEN_CONDITION_ALIAS,
    name: "Content Freeze Not Frozen Condition",
    api: () => import("./not-frozen.condition.js"),
  },
];

/**
 * Core actions that write content, media, blueprints or dictionary items. They are hidden while a freeze blocks
 * the current user, so editors do not find out on save. The server enforces the freeze regardless; anything
 * missing here (for example uploads through a dropzone) is still blocked, just with an error toast.
 */
export const FROZEN_ACTION_ALIASES = [
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
  "Umb.CollectionAction.Dictionary.Create",
];
