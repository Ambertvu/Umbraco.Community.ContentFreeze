using ContentFreeze.Models;
using Umbraco.Cms.Core.Notifications;

namespace ContentFreeze.Notifications;

/// <summary>
/// Base for the freeze lifecycle notifications. Handle these for custom integrations; they also drive the webhooks.
/// </summary>
public abstract class FreezeNotification : INotification
{
    protected FreezeNotification(Freeze freeze, FreezeActor actor)
    {
        Freeze = freeze;
        Actor = actor;
    }

    public Freeze Freeze { get; }

    /// <summary>Who caused this change; <see cref="FreezeActor.System"/> for time-based transitions.</summary>
    public FreezeActor Actor { get; }
}

/// <summary>A freeze was created with a start time in the future.</summary>
public class FreezeScheduledNotification : FreezeNotification
{
    public FreezeScheduledNotification(Freeze freeze, FreezeActor actor)
        : base(freeze, actor)
    {
    }
}

/// <summary>A freeze became active: either created to start now, or its scheduled start passed.</summary>
public class FreezeStartedNotification : FreezeNotification
{
    public FreezeStartedNotification(Freeze freeze, FreezeActor actor)
        : base(freeze, actor)
    {
    }
}

public class FreezeExtendedNotification : FreezeNotification
{
    public FreezeExtendedNotification(Freeze freeze, FreezeActor actor, DateTime previousEndsAt)
        : base(freeze, actor)
        => PreviousEndsAt = previousEndsAt;

    public DateTime PreviousEndsAt { get; }
}

/// <summary>An active freeze ended, either manually (<see cref="Freeze.EndedEarly"/>) or because its end time passed.</summary>
public class FreezeEndedNotification : FreezeNotification
{
    public FreezeEndedNotification(Freeze freeze, FreezeActor actor)
        : base(freeze, actor)
    {
    }
}

/// <summary>A scheduled freeze was called off before it started.</summary>
public class FreezeCancelledNotification : FreezeNotification
{
    public FreezeCancelledNotification(Freeze freeze, FreezeActor actor)
        : base(freeze, actor)
    {
    }
}
