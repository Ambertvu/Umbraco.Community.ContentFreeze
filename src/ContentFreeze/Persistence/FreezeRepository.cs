using ContentFreeze.Models;
using Umbraco.Cms.Infrastructure.Scoping;

namespace ContentFreeze.Persistence;

internal interface IFreezeRepository
{
    /// <summary>The freeze that is scheduled or active at <paramref name="utcNow"/>, if any.</summary>
    Freeze? GetCurrent(DateTime utcNow);

    Freeze? Get(int id);

    /// <summary>Looks up by key, numeric id or correlation id (latest freeze with that correlation id).</summary>
    Freeze? Find(string idOrCorrelationId);

    IReadOnlyList<Freeze> GetRecent(int count);

    /// <summary>Freezes whose start or end has passed but has not been handled by the background job yet.</summary>
    IReadOnlyList<Freeze> GetPendingTransitions(DateTime utcNow);

    Freeze Insert(Freeze freeze);

    void Update(Freeze freeze);

    void AddBlockedAttempts(int freezeId, int count);

    void AddAudit(FreezeAuditEntry entry);

    IReadOnlyList<FreezeAuditEntry> GetAudit(int freezeId);
}

internal class FreezeRepository : IFreezeRepository
{
    private readonly IScopeProvider _scopeProvider;

    public FreezeRepository(IScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public Freeze? GetCurrent(DateTime utcNow)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        FreezeDto? dto = scope.Database.FirstOrDefault<FreezeDto>(
            $"SELECT * FROM {FreezeDto.TableName} WHERE endedAt IS NULL AND cancelled = @0 AND endsAt > @1 ORDER BY startsAt",
            false,
            utcNow);
        return dto is null ? null : Map(dto);
    }

    public Freeze? Get(int id)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        FreezeDto? dto = scope.Database.FirstOrDefault<FreezeDto>($"SELECT * FROM {FreezeDto.TableName} WHERE id = @0", id);
        return dto is null ? null : Map(dto);
    }

    public Freeze? Find(string idOrCorrelationId)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);

        // Correlation ids first: pipelines pass numeric build ids, which must not hit an unrelated freeze id.
        FreezeDto? dto = scope.Database.FirstOrDefault<FreezeDto>(
            $"SELECT * FROM {FreezeDto.TableName} WHERE correlationId = @0 ORDER BY id DESC",
            idOrCorrelationId);

        if (dto is null && Guid.TryParse(idOrCorrelationId, out Guid key))
        {
            dto = scope.Database.FirstOrDefault<FreezeDto>($"SELECT * FROM {FreezeDto.TableName} WHERE {Quote("key")} = @0", key);
        }
        else if (dto is null && int.TryParse(idOrCorrelationId, out var id))
        {
            dto = scope.Database.FirstOrDefault<FreezeDto>($"SELECT * FROM {FreezeDto.TableName} WHERE id = @0", id);
        }

        return dto is null ? null : Map(dto);
    }

    public IReadOnlyList<Freeze> GetRecent(int count)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        return scope.Database
            .SkipTake<FreezeDto>(0, count, $"SELECT * FROM {FreezeDto.TableName} ORDER BY id DESC")
            .Select(Map)
            .ToList();
    }

    public IReadOnlyList<Freeze> GetPendingTransitions(DateTime utcNow)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        return scope.Database
            .Fetch<FreezeDto>(
                $"SELECT * FROM {FreezeDto.TableName} WHERE cancelled = @0 AND ((startNotifiedAt IS NULL AND startsAt <= @1 AND endedAt IS NULL) OR (endNotifiedAt IS NULL AND endedAt IS NULL AND endsAt <= @1))",
                false,
                utcNow)
            .Select(Map)
            .ToList();
    }

    public Freeze Insert(Freeze freeze)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        FreezeDto dto = Map(freeze);
        scope.Database.Insert(dto);
        freeze.Id = dto.Id;
        return freeze;
    }

    public void Update(Freeze freeze)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);

        // blockedAttempts is only ever incremented in place, see AddBlockedAttempts.
        scope.Database.Update(Map(freeze), UpdatableColumns);
    }

    private static readonly string[] UpdatableColumns =
    [
        "startsAt", "endsAt", "reason", "correlationId", "endedAt", "endedByKey", "endedByName", "endedByApiUser",
        "endedEarly", "endReason", "cancelled", "startNotifiedAt", "endNotifiedAt",
    ];

    public void AddBlockedAttempts(int freezeId, int count)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        scope.Database.Execute(
            $"UPDATE {FreezeDto.TableName} SET blockedAttempts = blockedAttempts + @0 WHERE id = @1",
            count,
            freezeId);
    }

    public void AddAudit(FreezeAuditEntry entry)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        scope.Database.Insert(new FreezeAuditDto
        {
            FreezeId = entry.FreezeId,
            Action = entry.Action.ToString(),
            Timestamp = entry.Timestamp,
            ActorKey = entry.Actor.Key,
            ActorName = entry.Actor.Name,
            ActorApiUser = entry.Actor.IsApiUser,
            Details = Truncate(entry.Details, 1000),
        });
    }

    public IReadOnlyList<FreezeAuditEntry> GetAudit(int freezeId)
    {
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        return scope.Database
            .Fetch<FreezeAuditDto>($"SELECT * FROM {FreezeAuditDto.TableName} WHERE freezeId = @0 ORDER BY id", freezeId)
            .Select(dto => new FreezeAuditEntry
            {
                Id = dto.Id,
                FreezeId = dto.FreezeId,
                Action = Enum.TryParse(dto.Action, out FreezeAuditAction action) ? action : FreezeAuditAction.Scheduled,
                Timestamp = DateTime.SpecifyKind(dto.Timestamp, DateTimeKind.Utc),
                Actor = new FreezeActor(dto.ActorKey, dto.ActorName, dto.ActorApiUser),
                Details = dto.Details,
            })
            .ToList();
    }

    // "key" is a reserved word on SQL Server and SQLite alike.
    private static string Quote(string column) => $"[{column}]";

    private static string? Truncate(string? value, int length)
        => value is null || value.Length <= length ? value : value[..length];

    private static Freeze Map(FreezeDto dto) => new()
    {
        Id = dto.Id,
        Key = dto.Key,
        StartsAt = Utc(dto.StartsAt),
        EndsAt = Utc(dto.EndsAt),
        Reason = dto.Reason,
        CorrelationId = dto.CorrelationId,
        CreatedAt = Utc(dto.CreatedAt),
        CreatedBy = new FreezeActor(dto.CreatedByKey, dto.CreatedByName, dto.CreatedByApiUser),
        EndedAt = Utc(dto.EndedAt),
        EndedBy = dto.EndedByName is null ? null : new FreezeActor(dto.EndedByKey, dto.EndedByName, dto.EndedByApiUser),
        EndedEarly = dto.EndedEarly,
        EndReason = dto.EndReason,
        Cancelled = dto.Cancelled,
        StartNotifiedAt = Utc(dto.StartNotifiedAt),
        EndNotifiedAt = Utc(dto.EndNotifiedAt),
        BlockedAttempts = dto.BlockedAttempts,
    };

    private static FreezeDto Map(Freeze freeze) => new()
    {
        Id = freeze.Id,
        Key = freeze.Key,
        StartsAt = freeze.StartsAt,
        EndsAt = freeze.EndsAt,
        Reason = Truncate(freeze.Reason, 500),
        CorrelationId = Truncate(freeze.CorrelationId, 200),
        CreatedAt = freeze.CreatedAt,
        CreatedByKey = freeze.CreatedBy.Key,
        CreatedByName = freeze.CreatedBy.Name,
        CreatedByApiUser = freeze.CreatedBy.IsApiUser,
        EndedAt = freeze.EndedAt,
        EndedByKey = freeze.EndedBy?.Key,
        EndedByName = freeze.EndedBy?.Name,
        EndedByApiUser = freeze.EndedBy?.IsApiUser ?? false,
        EndedEarly = freeze.EndedEarly,
        EndReason = Truncate(freeze.EndReason, 500),
        Cancelled = freeze.Cancelled,
        StartNotifiedAt = freeze.StartNotifiedAt,
        EndNotifiedAt = freeze.EndNotifiedAt,
        BlockedAttempts = freeze.BlockedAttempts,
    };

    // Dates are stored as UTC; the database hands them back as Unspecified.
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? Utc(DateTime? value) => value.HasValue ? Utc(value.Value) : null;
}
