using ContentFreeze.Enforcement;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Forms.Core.Services.Notifications;

namespace ContentFreeze.Forms;

/// <summary>
/// Backstop for Forms changes made outside the Management API, such as code or imports. Backoffice requests are
/// stopped earlier by <see cref="FormsFreezeRule"/>.
/// </summary>
/// <remarks>
/// Creating notifications are left alone: when cancelled, Forms returns nothing and reports success. Saving also
/// fires for new items. Record notifications are never handled: website submissions use them too.
/// </remarks>
internal class FormsFreezeHandler :
    INotificationHandler<FormSavingNotification>,
    INotificationHandler<FormDeletingNotification>,
    INotificationHandler<WorkflowSavingNotification>,
    INotificationHandler<WorkflowDeletingNotification>,
    INotificationHandler<FolderSavingNotification>,
    INotificationHandler<FolderDeletingNotification>,
    INotificationHandler<DataSourceSavingNotification>,
    INotificationHandler<DataSourceDeletingNotification>,
    INotificationHandler<PrevalueSourceSavingNotification>,
    INotificationHandler<PrevalueSourceDeletingNotification>
{
    private readonly IFreezeEnforcer _enforcer;
    private readonly IOptionsMonitor<ContentFreezeFormsOptions> _options;

    public FormsFreezeHandler(IFreezeEnforcer enforcer, IOptionsMonitor<ContentFreezeFormsOptions> options)
    {
        _enforcer = enforcer;
        _options = options;
    }

    public void Handle(FormSavingNotification notification) => Block(notification, "forms form save");

    public void Handle(FormDeletingNotification notification) => Block(notification, "forms form delete");

    public void Handle(WorkflowSavingNotification notification) => Block(notification, "forms workflow save");

    public void Handle(WorkflowDeletingNotification notification) => Block(notification, "forms workflow delete");

    public void Handle(FolderSavingNotification notification) => Block(notification, "forms folder save");

    public void Handle(FolderDeletingNotification notification) => Block(notification, "forms folder delete");

    public void Handle(DataSourceSavingNotification notification) => Block(notification, "forms data source save");

    public void Handle(DataSourceDeletingNotification notification) => Block(notification, "forms data source delete");

    public void Handle(PrevalueSourceSavingNotification notification) => Block(notification, "forms prevalue source save");

    public void Handle(PrevalueSourceDeletingNotification notification) => Block(notification, "forms prevalue source delete");

    private void Block(ICancelableNotification notification, string operation)
    {
        if (_options.CurrentValue.FreezeForms)
        {
            _enforcer.Block(notification, operation);
        }
    }
}
