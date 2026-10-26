namespace ContentFreeze.Models;

public enum FreezeState
{
    Scheduled,
    Active,
    Ended,
    Expired,
    Cancelled,
}

/// <summary>
/// A freeze is a time window, not a flag: its state follows from the stored times and the clock.
/// </summary>
public class Freeze
{
    public int Id { get; set; }

    public Guid Key { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public string? Reason { get; set; }

    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public FreezeActor CreatedBy { get; set; } = FreezeActor.System;

    public DateTime? EndedAt { get; set; }

    public FreezeActor? EndedBy { get; set; }

    public bool EndedEarly { get; set; }

    public string? EndReason { get; set; }

    public bool Cancelled { get; set; }

    public DateTime? StartNotifiedAt { get; set; }

    public DateTime? EndNotifiedAt { get; set; }

    public int BlockedAttempts { get; set; }

    public FreezeState GetState(DateTime utcNow)
    {
        if (Cancelled)
        {
            return FreezeState.Cancelled;
        }

        if (EndedAt.HasValue)
        {
            return EndedEarly ? FreezeState.Ended : FreezeState.Expired;
        }

        if (utcNow >= EndsAt)
        {
            return FreezeState.Expired;
        }

        return utcNow < StartsAt ? FreezeState.Scheduled : FreezeState.Active;
    }

    public bool IsCurrent(DateTime utcNow) => GetState(utcNow) is FreezeState.Scheduled or FreezeState.Active;
}

/// <summary>
/// Who did something to a freeze: a backoffice user, an API user (pipeline) or Umbraco itself.
/// </summary>
public record FreezeActor(Guid? Key, string Name, bool IsApiUser)
{
    public static readonly FreezeActor System = new(null, "System", false);
}
