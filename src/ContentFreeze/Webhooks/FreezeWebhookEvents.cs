using ContentFreeze.Models;
using ContentFreeze.Notifications;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Webhooks;

namespace ContentFreeze.Webhooks;

public static class FreezeWebhookEvents
{
    /// <summary>The group these events are listed under when configuring a webhook.</summary>
    public const string EventType = "Content Freeze";
}

/// <summary>
/// Webhook events for the freeze lifecycle, so Slack or Teams can be told when editing stops and starts again.
/// </summary>
public abstract class FreezeWebhookEventBase<TNotification> : WebhookEventBase<TNotification>
    where TNotification : FreezeNotification
{
    protected FreezeWebhookEventBase(
        IWebhookFiringService webhookFiringService,
        IWebhookService webhookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor)
        : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    {
    }

    public override object ConvertNotificationToRequestPayload(TNotification notification)
    {
        Freeze freeze = notification.Freeze;
        return new
        {
            Event = Alias,
            freeze.Id,
            freeze.Key,
            State = freeze.GetState(DateTime.UtcNow).ToString(),
            freeze.StartsAt,
            freeze.EndsAt,
            freeze.Reason,
            freeze.CorrelationId,
            CreatedBy = freeze.CreatedBy.Name,
            freeze.EndedAt,
            EndedBy = freeze.EndedBy?.Name,
            freeze.EndedEarly,
            freeze.EndReason,
            Actor = notification.Actor.Name,
            ActorIsApiUser = notification.Actor.IsApiUser,
        };
    }
}

[WebhookEvent("Content Freeze Scheduled", FreezeWebhookEvents.EventType)]
public class FreezeScheduledWebhookEvent : FreezeWebhookEventBase<FreezeScheduledNotification>
{
    public FreezeScheduledWebhookEvent(IWebhookFiringService webhookFiringService, IWebhookService webhookService, IOptionsMonitor<WebhookSettings> webhookSettings, IServerRoleAccessor serverRoleAccessor)
        : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    {
    }

    public override string Alias => "ContentFreeze.Scheduled";
}

[WebhookEvent("Content Freeze Started", FreezeWebhookEvents.EventType)]
public class FreezeStartedWebhookEvent : FreezeWebhookEventBase<FreezeStartedNotification>
{
    public FreezeStartedWebhookEvent(IWebhookFiringService webhookFiringService, IWebhookService webhookService, IOptionsMonitor<WebhookSettings> webhookSettings, IServerRoleAccessor serverRoleAccessor)
        : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    {
    }

    public override string Alias => "ContentFreeze.Started";
}

[WebhookEvent("Content Freeze Extended", FreezeWebhookEvents.EventType)]
public class FreezeExtendedWebhookEvent : FreezeWebhookEventBase<FreezeExtendedNotification>
{
    public FreezeExtendedWebhookEvent(IWebhookFiringService webhookFiringService, IWebhookService webhookService, IOptionsMonitor<WebhookSettings> webhookSettings, IServerRoleAccessor serverRoleAccessor)
        : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    {
    }

    public override string Alias => "ContentFreeze.Extended";
}

[WebhookEvent("Content Freeze Ended", FreezeWebhookEvents.EventType)]
public class FreezeEndedWebhookEvent : FreezeWebhookEventBase<FreezeEndedNotification>
{
    public FreezeEndedWebhookEvent(IWebhookFiringService webhookFiringService, IWebhookService webhookService, IOptionsMonitor<WebhookSettings> webhookSettings, IServerRoleAccessor serverRoleAccessor)
        : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    {
    }

    public override string Alias => "ContentFreeze.Ended";
}

[WebhookEvent("Content Freeze Cancelled", FreezeWebhookEvents.EventType)]
public class FreezeCancelledWebhookEvent : FreezeWebhookEventBase<FreezeCancelledNotification>
{
    public FreezeCancelledWebhookEvent(IWebhookFiringService webhookFiringService, IWebhookService webhookService, IOptionsMonitor<WebhookSettings> webhookSettings, IServerRoleAccessor serverRoleAccessor)
        : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    {
    }

    public override string Alias => "ContentFreeze.Cancelled";
}
