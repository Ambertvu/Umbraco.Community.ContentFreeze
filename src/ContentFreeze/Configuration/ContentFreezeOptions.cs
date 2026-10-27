namespace ContentFreeze.Configuration;

/// <summary>
/// Settings bound from the <c>ContentFreeze</c> section in appsettings.
/// </summary>
public class ContentFreezeOptions
{
    public const string SectionName = "ContentFreeze";

    /// <summary>
    /// Break-glass switch: when true, no freeze is enforced, whatever the database says.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Which writes a freeze blocks. See <see cref="EnforcementMode"/>.
    /// </summary>
    public EnforcementMode Mode { get; set; } = EnforcementMode.BackOfficeOnly;

    /// <summary>
    /// Duration used when a freeze request does not specify one.
    /// </summary>
    public int DefaultDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Upper bound for the total length of a freeze, extensions included.
    /// </summary>
    public int MaxDurationMinutes { get; set; } = 480;

    /// <summary>
    /// Minimum time between scheduling a freeze and its start, unless the request sets <c>force</c>.
    /// </summary>
    public int MinimumWarningMinutes { get; set; }

    /// <summary>
    /// Holds back scheduled publishing while a freeze is active; overdue items publish when it ends.
    /// </summary>
    public bool DeferScheduledPublishing { get; set; } = true;

    /// <summary>
    /// Shows a scheduled or active freeze on the backoffice login screen, with its times but not its reason.
    /// </summary>
    public bool ShowOnLoginScreen { get; set; } = true;

    /// <summary>
    /// Lets uSync imports write during a freeze, at startup and from the dashboard alike: a deploy usually
    /// imports while editing is frozen.
    /// </summary>
    public bool AllowUSyncImports { get; set; } = true;

    /// <summary>
    /// Umbraco background jobs that skip their runs while a freeze is active, by class name
    /// (<c>ContentVersionCleanupJob</c>), full type name or distributed job name. Read at startup.
    /// </summary>
    public string[] PausedJobs { get; set; } = [];

    public bool FreezeDictionary { get; set; } = true;

    public bool FreezeDomains { get; set; } = true;

    public bool FreezePublicAccess { get; set; } = true;

    public bool FreezeBlueprints { get; set; } = true;
}

public enum EnforcementMode
{
    /// <summary>
    /// Only block writes made by a backoffice or API user. Background jobs, imports and website code keep working.
    /// </summary>
    BackOfficeOnly,

    /// <summary>
    /// Block every write, unless it runs inside <see cref="Services.IFreezeService.AllowWrites"/>.
    /// </summary>
    Strict,
}
