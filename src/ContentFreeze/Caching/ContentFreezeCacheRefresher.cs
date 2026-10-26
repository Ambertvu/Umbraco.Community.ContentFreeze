using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace ContentFreeze.Caching;

/// <summary>
/// Clears the cached current freeze on every server when a freeze changes.
/// </summary>
/// <remarks>
/// Other servers pick this up through the database messenger, by default within 5 seconds. Enforcement does
/// not rely on this cache; it reads the database, so that window only affects the UI.
/// </remarks>
public sealed class ContentFreezeCacheRefresher : CacheRefresherBase<ContentFreezeCacheRefresherNotification>
{
    public static readonly Guid UniqueId = Guid.Parse("8C0A3E5B-4D2F-4F0E-9C61-5E8B2D7A1F34");

    internal const string CacheKey = "ContentFreeze.Current";

    public ContentFreezeCacheRefresher(AppCaches appCaches, IEventAggregator eventAggregator, ICacheRefresherNotificationFactory factory)
        : base(appCaches, eventAggregator, factory)
    {
    }

    public override Guid RefresherUniqueId => UniqueId;

    public override string Name => "Content Freeze cache refresher";

    public override void RefreshAll()
    {
        AppCaches.RuntimeCache.Clear(CacheKey);
        base.RefreshAll();
    }

    public override void Refresh(int id) => RefreshAll();

    public override void Refresh(Guid id) => RefreshAll();

    public override void Remove(int id) => RefreshAll();
}

public class ContentFreezeCacheRefresherNotification : CacheRefresherNotification
{
    public ContentFreezeCacheRefresherNotification(object messageObject, MessageType messageType)
        : base(messageObject, messageType)
    {
    }
}
