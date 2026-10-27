using ContentFreeze.Enforcement;
using Microsoft.Extensions.Options;

namespace ContentFreeze.Forms;

/// <summary>
/// The Forms backoffice actions to hide during a freeze. Aliases that a Forms version does not have are ignored.
/// </summary>
internal class FormsFrozenActions : IFrozenActionsProvider
{
    private static readonly string[] FormsAliases =
    [
        "Forms.WorkspaceAction.Form.Save",
        "Forms.WorkspaceAction.Form.SaveAndPreview",
        "Forms.WorkspaceAction.Form.Folder.Submit",
        "Forms.WorkspaceAction.DataSource.Save",
        "Forms.WorkspaceAction.DataSource.CreateForm",
        "Forms.WorkspaceAction.PrevalueSource.Save",
        "Forms.EntityAction.Form.Create",
        "Forms.EntityAction.Form.Copy",
        "Forms.EntityAction.Form.CopyWorkflows",
        "Forms.EntityAction.Form.Delete",
        "Forms.EntityAction.Form.Move",
        "Forms.EntityAction.Form.Import",
        "Forms.EntityAction.Folder.Delete",
        "Forms.EntityAction.Folder.Move",
        "Forms.EntityAction.Folder.Rename",
        "Forms.EntityAction.DataSource.Create",
        "Forms.EntityAction.Datasource.Delete", // Sic: lower-case "s" in Forms.
        "Forms.EntityAction.PrevalueSource.Create",
        "Forms.EntityAction.PrevalueSource.Delete",
        "Forms.CollectionAction.FormTreeItemChildren.Create",
        "Forms.CollectionAction.DataSourceTreeItemChildren.Create",
        "Forms.CollectionAction.PrevalueSourceTreeItemChildren.Create",
        "Forms.EntityCreateOptionAction.Form.Create",
        "Forms.EntityCreateOptionAction.Form.Folder",
    ];

    private static readonly string[] EntryAliases =
    [
        "Forms.EntityAction.Entry.Approve",
        "Forms.EntityAction.Entry.Reject",
        "Forms.EntityAction.Entry.Delete",
        "Forms.EntityBulkAction.Entry.Approve",
        "Forms.EntityBulkAction.Entry.Reject",
        "Forms.EntityBulkAction.Entry.Delete",
    ];

    private static readonly string[] SecurityAliases =
    [
        "Forms.WorkspaceAction.Security.User.Save",
        "Forms.WorkspaceAction.Security.UserGroup.Save",
        "Forms.EntityAction.Security.User.Create",
        "Forms.EntityAction.Security.User.Delete",
        "Forms.CollectionAction.SecurityUserFolderChildren.Create",
        "Forms.EntityCreateOptionAction.Security.User.Create",
    ];

    private readonly IOptions<ContentFreezeFormsOptions> _options;

    public FormsFrozenActions(IOptions<ContentFreezeFormsOptions> options) => _options = options;

    public IEnumerable<string> GetFrozenActionAliases()
    {
        ContentFreezeFormsOptions options = _options.Value;
        return
        [
            .. options.FreezeForms ? FormsAliases : [],
            .. options.FreezeEntries ? EntryAliases : [],
            .. options.FreezeSecurity ? SecurityAliases : [],
        ];
    }
}
