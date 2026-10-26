using NPoco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace ContentFreeze.Migrations;

internal class ContentFreezeMigrationPlan : MigrationPlan
{
    public ContentFreezeMigrationPlan()
        : base("ContentFreeze")
    {
        From(string.Empty)
            .To<CreateFreezeTables>("contentfreeze-tables")
            .To<GrantManagePermissionToAdministrators>("contentfreeze-admin-permission");
    }
}

/// <summary>
/// Runs the migration plan on startup, so installing the package needs no manual step.
/// </summary>
internal class RunContentFreezeMigrations : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly ICoreScopeProvider _coreScopeProvider;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;

    public RunContentFreezeMigrations(
        IMigrationPlanExecutor migrationPlanExecutor,
        ICoreScopeProvider coreScopeProvider,
        IKeyValueService keyValueService,
        IRuntimeState runtimeState)
    {
        _migrationPlanExecutor = migrationPlanExecutor;
        _coreScopeProvider = coreScopeProvider;
        _keyValueService = keyValueService;
        _runtimeState = runtimeState;
    }

    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        var upgrader = new Upgrader(new ContentFreezeMigrationPlan());
        await upgrader.ExecuteAsync(_migrationPlanExecutor, _coreScopeProvider, _keyValueService);
    }
}

internal class CreateFreezeTables : AsyncMigrationBase
{
    public CreateFreezeTables(IMigrationContext context)
        : base(context)
    {
    }

    protected override Task MigrateAsync()
    {
        if (TableExists(FreezeSchema.TableName) is false)
        {
            Create.Table<FreezeSchema>().Do();
        }

        if (TableExists(FreezeAuditSchema.TableName) is false)
        {
            Create.Table<FreezeAuditSchema>().Do();
        }

        return Task.CompletedTask;
    }

    // Schema snapshots for this migration only; the DTOs in Persistence may evolve, these must not.
    [TableName(TableName)]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    private class FreezeSchema
    {
        public const string TableName = "contentFreeze";

        [Column("id")]
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        public int Id { get; set; }

        [Column("key")]
        [Index(IndexTypes.UniqueNonClustered, Name = "IX_contentFreeze_key")]
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
        [Index(IndexTypes.NonClustered, Name = "IX_contentFreeze_correlationId")]
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
    private class FreezeAuditSchema
    {
        public const string TableName = "contentFreezeAudit";

        [Column("id")]
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        public int Id { get; set; }

        [Column("freezeId")]
        [ForeignKey(typeof(FreezeSchema), Name = "FK_contentFreezeAudit_contentFreeze")]
        [Index(IndexTypes.NonClustered, Name = "IX_contentFreezeAudit_freezeId")]
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
}

/// <summary>
/// Gives the Administrators group the manage permission once, at install. Admins can take it away again;
/// later upgrades never re-add it.
/// </summary>
internal class GrantManagePermissionToAdministrators : AsyncMigrationBase
{
    private readonly IUserGroupService _userGroupService;

    public GrantManagePermissionToAdministrators(IMigrationContext context, IUserGroupService userGroupService)
        : base(context)
        => _userGroupService = userGroupService;

    protected override async Task MigrateAsync()
    {
        IUserGroup? adminGroup = await _userGroupService.GetAsync(Umbraco.Cms.Core.Constants.Security.AdminGroupAlias);
        if (adminGroup is null || adminGroup.Permissions.Contains(ContentFreezePermissions.Manage))
        {
            return;
        }

        adminGroup.Permissions.Add(ContentFreezePermissions.Manage);
        await _userGroupService.UpdateAsync(adminGroup, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
    }
}
