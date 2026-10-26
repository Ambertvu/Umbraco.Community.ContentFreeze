namespace ContentFreeze.Models;

public enum FreezeAuditAction
{
    Scheduled,
    Started,
    Extended,
    Ended,
    Expired,
    Cancelled,
}

public class FreezeAuditEntry
{
    public int Id { get; set; }

    public int FreezeId { get; set; }

    public FreezeAuditAction Action { get; set; }

    public DateTime Timestamp { get; set; }

    public FreezeActor Actor { get; set; } = FreezeActor.System;

    public string? Details { get; set; }
}
