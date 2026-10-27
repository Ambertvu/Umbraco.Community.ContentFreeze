using ContentFreeze.Caching;
using ContentFreeze.Configuration;
using ContentFreeze.Models;
using ContentFreeze.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace ContentFreeze.Tests.Support;

/// <summary>A <see cref="FreezeService"/> on an in-memory repository and a clock the test controls.</summary>
internal class TestFreezeService
{
    public static readonly DateTimeOffset Start = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    public static readonly FreezeActor Pipeline = new(Guid.NewGuid(), "Azure Pipeline", true);

    public TestFreezeService(ContentFreezeOptions? options = null)
    {
        Options = options ?? new ContentFreezeOptions();

        var refresher = new ContentFreezeCacheRefresher(
            AppCaches.Disabled,
            Mock.Of<IEventAggregator>(),
            Mock.Of<ICacheRefresherNotificationFactory>());
        var distributedCache = new DistributedCache(Mock.Of<IServerMessenger>(), new CacheRefresherCollection(() => [refresher]));

        Service = new FreezeService(
            Repository,
            Events.Object,
            distributedCache,
            AppCaches.Disabled,
            Clock,
            new StaticOptionsMonitor<ContentFreezeOptions>(Options),
            NullLogger<FreezeService>.Instance);
    }

    public ContentFreezeOptions Options { get; }

    public FakeTimeProvider Clock { get; } = new(Start);

    public InMemoryFreezeRepository Repository { get; } = new();

    public Mock<IEventAggregator> Events { get; } = new();

    public FreezeService Service { get; }

    public async Task<Freeze> ScheduleAsync(int startsInMinutes = 0, int durationMinutes = 30, string? correlationId = null)
    {
        FreezeResult result = await Service.ScheduleAsync(
            new ScheduleFreezeRequest(startsInMinutes, null, durationMinutes, "Release", correlationId, Force: false),
            Pipeline);
        return result.Freeze!;
    }
}

internal class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
