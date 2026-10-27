using ContentFreeze.Models;

namespace ContentFreeze.Controllers;

public class FreezeStatusResponseModel
{
    /// <summary>Server clock, so clients can correct for skew when counting down.</summary>
    public DateTimeOffset ServerTime { get; init; }

    /// <summary>The scheduled or active freeze, or null.</summary>
    public FreezeResponseModel? Freeze { get; init; }

    /// <summary>True when ContentFreeze:Disabled is set: freezes are recorded but not enforced.</summary>
    public bool EnforcementDisabled { get; init; }

    public bool CanManage { get; init; }

    public bool CanBypass { get; init; }
}

/// <summary>
/// What the login screen shows. Anyone can request it, so it holds no reason, ids or names.
/// </summary>
public class FreezeLoginStatusResponseModel
{
    public DateTimeOffset ServerTime { get; init; }

    public FreezeState? State { get; init; }

    public DateTimeOffset? StartsAt { get; init; }

    public DateTimeOffset? EndsAt { get; init; }
}

public class FreezeResponseModel
{
    public int Id { get; init; }

    public Guid Key { get; init; }

    public FreezeState State { get; init; }

    public DateTimeOffset StartsAt { get; init; }

    public DateTimeOffset EndsAt { get; init; }

    public string? Reason { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public FreezeActorResponseModel CreatedBy { get; init; } = null!;

    public DateTimeOffset? EndedAt { get; init; }

    public FreezeActorResponseModel? EndedBy { get; init; }

    public bool EndedEarly { get; init; }

    public string? EndReason { get; init; }

    public int BlockedAttempts { get; init; }

    internal static FreezeResponseModel From(Freeze freeze, DateTime utcNow) => new()
    {
        Id = freeze.Id,
        Key = freeze.Key,
        State = freeze.GetState(utcNow),
        StartsAt = Utc(freeze.StartsAt),
        EndsAt = Utc(freeze.EndsAt),
        Reason = freeze.Reason,
        CorrelationId = freeze.CorrelationId,
        CreatedAt = Utc(freeze.CreatedAt),
        CreatedBy = FreezeActorResponseModel.From(freeze.CreatedBy),
        EndedAt = freeze.EndedAt is { } endedAt ? Utc(endedAt) : null,
        EndedBy = freeze.EndedBy is null ? null : FreezeActorResponseModel.From(freeze.EndedBy),
        EndedEarly = freeze.EndedEarly,
        EndReason = freeze.EndReason,
        BlockedAttempts = freeze.BlockedAttempts,
    };

    internal static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

public class FreezeActorResponseModel
{
    public string Name { get; init; } = string.Empty;

    public bool IsApiUser { get; init; }

    internal static FreezeActorResponseModel From(FreezeActor actor) => new() { Name = actor.Name, IsApiUser = actor.IsApiUser };
}

public class FreezeAuditEntryResponseModel
{
    public FreezeAuditAction Action { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    public FreezeActorResponseModel Actor { get; init; } = null!;

    public string? Details { get; init; }
}

public class FreezeDetailsResponseModel
{
    public FreezeResponseModel Freeze { get; init; } = null!;

    public IEnumerable<FreezeAuditEntryResponseModel> Audit { get; init; } = [];
}

public class ScheduleFreezeRequestModel
{
    /// <summary>Minutes from now until the freeze starts; 0 or empty starts it immediately. Preferred over <see cref="StartsAt"/>.</summary>
    public int? StartsInMinutes { get; init; }

    /// <summary>Absolute start time. Ignored when <see cref="StartsInMinutes"/> is set.</summary>
    public DateTimeOffset? StartsAt { get; init; }

    /// <summary>Length of the freeze; defaults to ContentFreeze:DefaultDurationMinutes.</summary>
    public int? DurationMinutes { get; init; }

    public string? Reason { get; init; }

    /// <summary>For example a build id. A retry with the same id returns the existing freeze.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Skip the minimum warning time.</summary>
    public bool Force { get; init; }
}

public class ExtendFreezeRequestModel
{
    public int Minutes { get; init; }

    public string? Reason { get; init; }
}

public class EndFreezeRequestModel
{
    public string? Reason { get; init; }
}

public class FreezeOperationResponseModel
{
    /// <summary>What happened, for example Created, Existing, Updated, AlreadyEnded or NothingToEnd.</summary>
    public string Status { get; init; } = string.Empty;

    public string? Message { get; init; }

    public FreezeResponseModel? Freeze { get; init; }
}
