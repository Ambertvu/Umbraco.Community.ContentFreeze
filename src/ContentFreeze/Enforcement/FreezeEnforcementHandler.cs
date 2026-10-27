using ContentFreeze.Configuration;
using ContentFreeze.Models;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace ContentFreeze.Enforcement;

/// <summary>
/// Cancels content, media and related writes while a freeze is active. This is the actual guarantee;
/// the backoffice UI only makes it visible.
/// </summary>
internal class FreezeEnforcementHandler :
    INotificationHandler<ContentSavingNotification>,
    INotificationHandler<ContentPublishingNotification>,
    INotificationHandler<ContentUnpublishingNotification>,
    INotificationHandler<ContentSendingToPublishNotification>,
    INotificationHandler<ContentMovingNotification>,
    INotificationHandler<ContentMovingToRecycleBinNotification>,
    INotificationHandler<ContentDeletingNotification>,
    INotificationHandler<ContentSortingNotification>,
    INotificationHandler<ContentRollingBackNotification>,
    INotificationHandler<ContentCopyingNotification>,
    INotificationHandler<ContentEmptyingRecycleBinNotification>,
    INotificationHandler<ContentDeletingVersionsNotification>,
    INotificationHandler<MediaSavingNotification>,
    INotificationHandler<MediaMovingNotification>,
    INotificationHandler<MediaMovingToRecycleBinNotification>,
    INotificationHandler<MediaDeletingNotification>,
    INotificationHandler<MediaEmptyingRecycleBinNotification>,
    INotificationHandler<MediaDeletingVersionsNotification>,
    INotificationHandler<DictionaryItemSavingNotification>,
    INotificationHandler<DictionaryItemDeletingNotification>,
    INotificationHandler<DictionaryItemMovingNotification>,
    INotificationHandler<DomainSavingNotification>,
    INotificationHandler<DomainDeletingNotification>,
    INotificationHandler<PublicAccessEntrySavingNotification>,
    INotificationHandler<PublicAccessEntryDeletingNotification>
{
    private readonly IFreezeEnforcer _enforcer;
    private readonly IOptionsMonitor<ContentFreezeOptions> _options;

    public FreezeEnforcementHandler(IFreezeEnforcer enforcer, IOptionsMonitor<ContentFreezeOptions> options)
    {
        _enforcer = enforcer;
        _options = options;
    }

    public void Handle(ContentSavingNotification notification) => Block(notification, "content save");

    public void Handle(ContentPublishingNotification notification) => Block(notification, "content publish");

    public void Handle(ContentUnpublishingNotification notification) => Block(notification, "content unpublish");

    public void Handle(ContentSendingToPublishNotification notification) => Block(notification, "content send to publish");

    public void Handle(ContentMovingNotification notification) => Block(notification, "content move");

    public void Handle(ContentMovingToRecycleBinNotification notification) => Block(notification, "content move to recycle bin");

    public void Handle(ContentDeletingNotification notification) => Block(notification, "content delete");

    public void Handle(ContentSortingNotification notification) => Block(notification, "content sort");

    public void Handle(ContentRollingBackNotification notification) => Block(notification, "content rollback");

    public void Handle(ContentCopyingNotification notification) => Block(notification, "content copy");

    public void Handle(ContentEmptyingRecycleBinNotification notification) => Block(notification, "content recycle bin empty");

    public void Handle(ContentDeletingVersionsNotification notification) => Block(notification, "content version delete");

    public void Handle(MediaSavingNotification notification) => Block(notification, "media save");

    public void Handle(MediaMovingNotification notification) => Block(notification, "media move");

    public void Handle(MediaMovingToRecycleBinNotification notification) => Block(notification, "media move to recycle bin");

    public void Handle(MediaDeletingNotification notification) => Block(notification, "media delete");

    public void Handle(MediaEmptyingRecycleBinNotification notification) => Block(notification, "media recycle bin empty");

    public void Handle(MediaDeletingVersionsNotification notification) => Block(notification, "media version delete");

    public void Handle(DictionaryItemSavingNotification notification)
        => BlockIf(_options.CurrentValue.FreezeDictionary, notification, "dictionary save");

    public void Handle(DictionaryItemDeletingNotification notification)
        => BlockIf(_options.CurrentValue.FreezeDictionary, notification, "dictionary delete");

    public void Handle(DictionaryItemMovingNotification notification)
        => BlockIf(_options.CurrentValue.FreezeDictionary, notification, "dictionary move");

    public void Handle(DomainSavingNotification notification)
        => BlockIf(_options.CurrentValue.FreezeDomains, notification, "domain save");

    public void Handle(DomainDeletingNotification notification)
        => BlockIf(_options.CurrentValue.FreezeDomains, notification, "domain delete");

    public void Handle(PublicAccessEntrySavingNotification notification)
        => BlockIf(_options.CurrentValue.FreezePublicAccess, notification, "public access save");

    public void Handle(PublicAccessEntryDeletingNotification notification)
        => BlockIf(_options.CurrentValue.FreezePublicAccess, notification, "public access delete");

    private void BlockIf(bool enabled, ICancelableNotification notification, string operation)
    {
        if (enabled)
        {
            Block(notification, operation);
        }
    }

    private void Block(ICancelableNotification notification, string operation) => _enforcer.Block(notification, operation);
}

internal class FreezeEnforcer : IFreezeEnforcer
{
    private readonly IFreezeGuard _guard;
    private readonly IEventMessagesFactory _eventMessagesFactory;

    public FreezeEnforcer(IFreezeGuard guard, IEventMessagesFactory eventMessagesFactory)
    {
        _guard = guard;
        _eventMessagesFactory = eventMessagesFactory;
    }

    public bool Block(ICancelableNotification notification, string operation)
    {
        if (notification.Cancel)
        {
            return false;
        }

        Freeze? freeze = _guard.GetBlockingFreeze(operation);
        if (freeze is null)
        {
            return false;
        }

        notification.Cancel = true;

        // A notification's Messages are this same request-scoped collection.
        _eventMessagesFactory.Get().Add(new EventMessage("Content freeze", _guard.GetBlockedMessage(freeze), EventMessageType.Error)
        {
            IsDefaultEventMessage = true,
        });
        return true;
    }
}

internal static class FreezeEnforcementRegistration
{
    public static IUmbracoBuilder AddFreezeEnforcement(this IUmbracoBuilder builder) => builder
        .AddNotificationHandler<ContentSavingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentPublishingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentUnpublishingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentSendingToPublishNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentMovingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentMovingToRecycleBinNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentDeletingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentSortingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentRollingBackNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentCopyingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentEmptyingRecycleBinNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<ContentDeletingVersionsNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<MediaSavingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<MediaMovingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<MediaMovingToRecycleBinNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<MediaDeletingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<MediaEmptyingRecycleBinNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<MediaDeletingVersionsNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<DictionaryItemSavingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<DictionaryItemDeletingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<DictionaryItemMovingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<DomainSavingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<DomainDeletingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<PublicAccessEntrySavingNotification, FreezeEnforcementHandler>()
        .AddNotificationHandler<PublicAccessEntryDeletingNotification, FreezeEnforcementHandler>();
}
