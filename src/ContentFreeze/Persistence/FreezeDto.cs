using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace ContentFreeze.Persistence;

[TableName(TableName)]
[PrimaryKey("id", AutoIncrement = true)]
[ExplicitColumns]
internal class FreezeDto
{
    public const string TableName = "contentFreeze";

    [Column("id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("key")]
    public Guid Key { get; set; }

    [Column("startsAt")]
    public DateTime StartsAt { get; set; }

    [Column("endsAt")]
    public DateTime EndsAt { get; set; }

    [Column("reason")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(500)]
    public string? Reason { get; set; }

    [Column("correlationId")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(200)]
    public string? CorrelationId { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Column("createdByKey")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public Guid? CreatedByKey { get; set; }

    [Column("createdByName")]
    [Length(255)]
    public string CreatedByName { get; set; } = string.Empty;

    [Column("createdByApiUser")]
    public bool CreatedByApiUser { get; set; }

    [Column("endedAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? EndedAt { get; set; }

    [Column("endedByKey")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public Guid? EndedByKey { get; set; }

    [Column("endedByName")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(255)]
    public string? EndedByName { get; set; }

    [Column("endedByApiUser")]
    public bool EndedByApiUser { get; set; }

    [Column("endedEarly")]
    public bool EndedEarly { get; set; }

    [Column("endReason")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(500)]
    public string? EndReason { get; set; }

    [Column("cancelled")]
    public bool Cancelled { get; set; }

    [Column("startNotifiedAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? StartNotifiedAt { get; set; }

    [Column("endNotifiedAt")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public DateTime? EndNotifiedAt { get; set; }

    [Column("blockedAttempts")]
    public int BlockedAttempts { get; set; }
}

[TableName(TableName)]
[PrimaryKey("id", AutoIncrement = true)]
[ExplicitColumns]
internal class FreezeAuditDto
{
    public const string TableName = "contentFreezeAudit";

    [Column("id")]
    [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
    public int Id { get; set; }

    [Column("freezeId")]
    public int FreezeId { get; set; }

    [Column("action")]
    [Length(50)]
    public string Action { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Column("actorKey")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public Guid? ActorKey { get; set; }

    [Column("actorName")]
    [Length(255)]
    public string ActorName { get; set; } = string.Empty;

    [Column("actorApiUser")]
    public bool ActorApiUser { get; set; }

    [Column("details")]
    [NullSetting(NullSetting = NullSettings.Null)]
    [Length(1000)]
    public string? Details { get; set; }
}
