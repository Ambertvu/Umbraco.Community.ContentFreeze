using System.Collections.Concurrent;
using ContentFreeze.Caching;
using ContentFreeze.Configuration;
using ContentFreeze.Models;
using ContentFreeze.Notifications;
using ContentFreeze.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Extensions;

namespace ContentFreeze.Services;

public interface IFreezeService
{
    /// <summary>The scheduled or active freeze, from cache. Fine for display; enforcement uses <see cref="GetCurrentFromDatabase"/>.</summary>
    Freeze? GetCurrent();

    Freeze? GetCurrentFromDatabase();

    /// <summary>
    /// True while a freeze is active and enforced. Check it in your own jobs and imports to hold them back.
    /// </summary>
    bool IsFrozen { get; }

    /// <summary>Finds a freeze by key, id or correlation id.</summary>
    Freeze? Find(string idOrCorrelationId);

    IReadOnlyList<Freeze> GetRecent(int count);

    IReadOnlyList<FreezeAuditEntry> GetAudit(int freezeId);

    Task<FreezeResult> ScheduleAsync(ScheduleFreezeRequest request, FreezeActor actor);

    Task<FreezeResult> ExtendAsync(string idOrCorrelationId, int minutes, FreezeActor actor, string? reason);

    /// <summary>Ends an active freeze, or cancels a scheduled one. Idempotent.</summary>
    Task<FreezeResult> EndAsync(string idOrCorrelationId, FreezeActor actor, string? reason);

    /// <summary>Cancels a scheduled freeze. An active freeze has to be ended instead.</summary>
    Task<FreezeResult> CancelAsync(string idOrCorrelationId, FreezeActor actor, string? reason);

    /// <summary>
    /// Allows writes on this async flow while the returned scope is alive, for imports and background jobs.
    /// </summary>
    /// <example>
    /// <code>
    /// using (freezeService.AllowWrites("nightly-import"))
    /// {
    ///     contentService.Save(content);
    /// }
    /// </code>
    /// </example>
    IDisposable AllowWrites(string reason);

    bool WritesAllowedInScope { get; }

    DateTime UtcNow { get; }
}

public record ScheduleFreezeRequest(
    int? StartsInMinutes,
    DateTimeOffset? StartsAt,
    int? DurationMinutes,
    string? Reason,
    string? CorrelationId,
    bool Force);

public enum FreezeResultStatus
{
    Created,
    Existing,
    Updated,
    AlreadyEnded,
    NothingToEnd,
    NotFound,
    Conflict,
    Invalid,
}

public record FreezeResult(FreezeResultStatus Status, Freeze? Freeze, string? Message = null);

internal class FreezeService : IFreezeService
{
    private static readonly AsyncLocal<int> AllowWritesDepth = new();

    // One freeze at a time: serialise changes on this server. A backoffice normally runs on one server.
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ConcurrentDictionary<int, int> _blockedAttempts = new();

    private readonly IFreezeRepository _repository;
    private readonly IEventAggregator _eventAggregator;
    private readonly DistributedCache _distributedCache;
    private readonly AppCaches _appCaches;
    private readonly TimeProvider _timeProvider;
    private readonly IOptionsMonitor<ContentFreezeOptions> _options;
    private readonly ILogger<FreezeService> _logger;

    public FreezeService(
        IFreezeRepository repository,
        IEventAggregator eventAggregator,
        DistributedCache distributedCache,
        AppCaches appCaches,
        TimeProvider timeProvider,
        IOptionsMonitor<ContentFreezeOptions> options,
        ILogger<FreezeService> logger)
    {
        _repository = repository;
        _eventAggregator = eventAggregator;
        _distributedCache = distributedCache;
        _appCaches = appCaches;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public bool WritesAllowedInScope => AllowWritesDepth.Value > 0;

    public Freeze? GetCurrent()
    {
        CachedFreeze? cached = _appCaches.RuntimeCache.GetCacheItem(
            ContentFreezeCacheRefresher.CacheKey,
            () => new CachedFreeze(_repository.GetCurrent(UtcNow)),
            TimeSpan.FromSeconds(30));

        // The cached freeze may have expired since it was cached.
        Freeze? freeze = cached?.Freeze;
        return freeze?.IsCurrent(UtcNow) is true ? freeze : null;
    }

    public Freeze? GetCurrentFromDatabase() => _repository.GetCurrent(UtcNow);

    public bool IsFrozen => !_options.CurrentValue.Disabled && GetCurrent()?.GetState(UtcNow) == FreezeState.Active;

    public Freeze? Find(string idOrCorrelationId) => _repository.Find(idOrCorrelationId);

    public IReadOnlyList<Freeze> GetRecent(int count) => _repository.GetRecent(count);

    public IReadOnlyList<FreezeAuditEntry> GetAudit(int freezeId) => _repository.GetAudit(freezeId);

    public async Task<FreezeResult> ScheduleAsync(ScheduleFreezeRequest request, FreezeActor actor)
    {
        ContentFreezeOptions options = _options.CurrentValue;
        DateTime now = UtcNow;

        var duration = request.DurationMinutes ?? options.DefaultDurationMinutes;
        if (duration <= 0 || duration > options.MaxDurationMinutes)
        {
            return Invalid($"Duration must be between 1 and {options.MaxDurationMinutes} minutes.");
        }

        DateTime startsAt;
        if (request.StartsAt.HasValue)
        {
            // Absolute start times are accepted, but a start in the (recent) past simply means "now".
            startsAt = request.StartsAt.Value.UtcDateTime;
            if (startsAt < now)
            {
                startsAt = now;
            }
        }
        else
        {
            if (request.StartsInMinutes < 0)
            {
                return Invalid("startsInMinutes cannot be negative.");
            }

            startsAt = now.AddMinutes(request.StartsInMinutes ?? 0);
        }

        if (request.Force is false && startsAt - now < TimeSpan.FromMinutes(options.MinimumWarningMinutes) - TimeSpan.FromSeconds(5))
        {
            return Invalid(
                $"A freeze must be announced at least {options.MinimumWarningMinutes} minutes in advance. Set force to start sooner.");
        }

        await _lock.WaitAsync();
        try
        {
            Freeze? current = _repository.GetCurrent(now);
            if (current is not null)
            {
                // Retries from the same pipeline run get the freeze they already created.
                return string.IsNullOrWhiteSpace(request.CorrelationId) is false && current.CorrelationId == request.CorrelationId
                    ? new FreezeResult(FreezeResultStatus.Existing, current)
                    : new FreezeResult(
                        FreezeResultStatus.Conflict,
                        current,
                        "Another freeze is already scheduled or active. Extend or end that one instead.");
            }

            var freeze = new Freeze
            {
                Key = Guid.NewGuid(),
                StartsAt = startsAt,
                EndsAt = startsAt.AddMinutes(duration),
                Reason = request.Reason.NullOrWhiteSpaceAsNull()?.Trim(),
                CorrelationId = request.CorrelationId.NullOrWhiteSpaceAsNull()?.Trim(),
                CreatedAt = now,
                CreatedBy = actor,
            };

            var startsNow = startsAt <= now;
            if (startsNow)
            {
                freeze.StartNotifiedAt = now;
            }

            _repository.Insert(freeze);
            Audit(freeze, FreezeAuditAction.Scheduled, actor, $"From {freeze.StartsAt:u} until {freeze.EndsAt:u}");

            if (startsNow)
            {
                Audit(freeze, FreezeAuditAction.Started, actor);
            }

            RefreshCache();
            _logger.LogInformation(
                "Content freeze {FreezeId} scheduled by {Actor} from {StartsAt} until {EndsAt}: {Reason}",
                freeze.Id,
                actor.Name,
                freeze.StartsAt,
                freeze.EndsAt,
                freeze.Reason);

            if (startsNow)
            {
                await _eventAggregator.PublishAsync(new FreezeStartedNotification(freeze, actor));
            }
            else
            {
                await _eventAggregator.PublishAsync(new FreezeScheduledNotification(freeze, actor));
            }

            return new FreezeResult(FreezeResultStatus.Created, freeze);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<FreezeResult> ExtendAsync(string idOrCorrelationId, int minutes, FreezeActor actor, string? reason)
    {
        if (minutes <= 0)
        {
            return Invalid("minutes must be positive.");
        }

        await _lock.WaitAsync();
        try
        {
            Freeze? freeze = _repository.Find(idOrCorrelationId);
            if (freeze is null)
            {
                return new FreezeResult(FreezeResultStatus.NotFound, null, "No freeze found with that id.");
            }

            if (freeze.IsCurrent(UtcNow) is false)
            {
                return new FreezeResult(FreezeResultStatus.Conflict, freeze, "This freeze has already ended and can no longer be extended.");
            }

            DateTime previousEndsAt = freeze.EndsAt;
            DateTime endsAt = previousEndsAt.AddMinutes(minutes);
            var maxDuration = _options.CurrentValue.MaxDurationMinutes;
            if (endsAt - freeze.StartsAt > TimeSpan.FromMinutes(maxDuration))
            {
                return new FreezeResult(
                    FreezeResultStatus.Invalid,
                    freeze,
                    $"A freeze can last at most {maxDuration} minutes in total.");
            }

            freeze.EndsAt = endsAt;
            _repository.Update(freeze);
            Audit(freeze, FreezeAuditAction.Extended, actor, Join($"Until {endsAt:u} (+{minutes} min)", reason));
            RefreshCache();

            await _eventAggregator.PublishAsync(new FreezeExtendedNotification(freeze, actor, previousEndsAt));
            return new FreezeResult(FreezeResultStatus.Updated, freeze);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<FreezeResult> EndAsync(string idOrCorrelationId, FreezeActor actor, string? reason)
    {
        await _lock.WaitAsync();
        try
        {
            Freeze? freeze = _repository.Find(idOrCorrelationId);
            if (freeze is null)
            {
                // A pipeline that failed before it froze anything still runs its cleanup step: that is not an error.
                return new FreezeResult(FreezeResultStatus.NothingToEnd, null, "No freeze found with that id; nothing to end.");
            }

            DateTime now = UtcNow;
            switch (freeze.GetState(now))
            {
                case FreezeState.Scheduled:
                    return await CancelInternalAsync(freeze, actor, reason, now);

                case FreezeState.Active:
                    freeze.EndedAt = now;
                    freeze.EndedBy = actor;
                    freeze.EndedEarly = true;
                    freeze.EndReason = reason.NullOrWhiteSpaceAsNull()?.Trim();
                    freeze.EndNotifiedAt = now;
                    _repository.Update(freeze);
                    Audit(freeze, FreezeAuditAction.Ended, actor, reason);
                    RefreshCache();
                    _logger.LogInformation("Content freeze {FreezeId} ended by {Actor}", freeze.Id, actor.Name);

                    await _eventAggregator.PublishAsync(new FreezeEndedNotification(freeze, actor));
                    return new FreezeResult(FreezeResultStatus.Updated, freeze);

                default:
                    return new FreezeResult(FreezeResultStatus.AlreadyEnded, freeze, "This freeze has already ended.");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<FreezeResult> CancelAsync(string idOrCorrelationId, FreezeActor actor, string? reason)
    {
        await _lock.WaitAsync();
        try
        {
            Freeze? freeze = _repository.Find(idOrCorrelationId);
            if (freeze is null)
            {
                return new FreezeResult(FreezeResultStatus.NotFound, null, "No freeze found with that id.");
            }

            DateTime now = UtcNow;
            return freeze.GetState(now) switch
            {
                FreezeState.Scheduled => await CancelInternalAsync(freeze, actor, reason, now),
                FreezeState.Active => new FreezeResult(FreezeResultStatus.Conflict, freeze, "This freeze is active; end it instead."),
                _ => new FreezeResult(FreezeResultStatus.AlreadyEnded, freeze, "This freeze has already ended."),
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    public IDisposable AllowWrites(string reason)
    {
        _logger.LogDebug("Content freeze: writes allowed for {Reason}", reason);
        AllowWritesDepth.Value++;
        return new AllowWritesScope();
    }

    /// <summary>Counts a blocked write. Kept in memory and flushed by the background job, see <see cref="FlushBlockedAttempts"/>.</summary>
    /// <remarks>
    /// Writing here would happen inside the cancelled operation's transaction and be rolled back with it.
    /// </remarks>
    internal void RecordBlockedAttempt(int freezeId) => _blockedAttempts.AddOrUpdate(freezeId, 1, (_, count) => count + 1);

    internal void FlushBlockedAttempts()
    {
        foreach (var freezeId in _blockedAttempts.Keys)
        {
            if (_blockedAttempts.TryRemove(freezeId, out var count) && count > 0)
            {
                _repository.AddBlockedAttempts(freezeId, count);
            }
        }
    }

    /// <summary>
    /// Records time-based transitions (a scheduled start or end passing) and raises their notifications.
    /// </summary>
    internal async Task ProcessTransitionsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            DateTime now = UtcNow;
            IReadOnlyList<Freeze> pending = _repository.GetPendingTransitions(now);
            foreach (Freeze freeze in pending)
            {
                if (freeze.StartNotifiedAt is null && freeze.StartsAt <= now)
                {
                    freeze.StartNotifiedAt = now;
                    _repository.Update(freeze);
                    Audit(freeze, FreezeAuditAction.Started, FreezeActor.System);
                    _logger.LogInformation("Content freeze {FreezeId} started", freeze.Id);
                    await _eventAggregator.PublishAsync(new FreezeStartedNotification(freeze, FreezeActor.System));
                }

                if (freeze.EndNotifiedAt is null && freeze.EndsAt <= now)
                {
                    freeze.EndedAt = freeze.EndsAt;
                    freeze.EndNotifiedAt = now;
                    _repository.Update(freeze);
                    Audit(freeze, FreezeAuditAction.Expired, FreezeActor.System);
                    _logger.LogInformation("Content freeze {FreezeId} expired", freeze.Id);
                    await _eventAggregator.PublishAsync(new FreezeEndedNotification(freeze, FreezeActor.System));
                }
            }

            if (pending.Count > 0)
            {
                RefreshCache();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<FreezeResult> CancelInternalAsync(Freeze freeze, FreezeActor actor, string? reason, DateTime now)
    {
        freeze.Cancelled = true;
        freeze.EndedAt = now;
        freeze.EndedBy = actor;
        freeze.EndReason = reason.NullOrWhiteSpaceAsNull()?.Trim();
        freeze.StartNotifiedAt ??= now;
        freeze.EndNotifiedAt = now;
        _repository.Update(freeze);
        Audit(freeze, FreezeAuditAction.Cancelled, actor, reason);
        RefreshCache();
        _logger.LogInformation("Content freeze {FreezeId} cancelled by {Actor}", freeze.Id, actor.Name);

        await _eventAggregator.PublishAsync(new FreezeCancelledNotification(freeze, actor));
        return new FreezeResult(FreezeResultStatus.Updated, freeze);
    }

    private void Audit(Freeze freeze, FreezeAuditAction action, FreezeActor actor, string? details = null)
        => _repository.AddAudit(new FreezeAuditEntry
        {
            FreezeId = freeze.Id,
            Action = action,
            Timestamp = UtcNow,
            Actor = actor,
            Details = details.NullOrWhiteSpaceAsNull(),
        });

    private void RefreshCache() => _distributedCache.RefreshAll(ContentFreezeCacheRefresher.UniqueId);

    private static FreezeResult Invalid(string message) => new(FreezeResultStatus.Invalid, null, message);

    private static string Join(string text, string? reason)
        => string.IsNullOrWhiteSpace(reason) ? text : $"{text}: {reason.Trim()}";

    private sealed record CachedFreeze(Freeze? Freeze);

    private sealed class AllowWritesScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            AllowWritesDepth.Value--;
        }
    }
}
