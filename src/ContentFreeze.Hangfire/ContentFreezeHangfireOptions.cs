namespace ContentFreeze.Hangfire;

/// <summary>
/// Settings bound from the <c>ContentFreeze:Hangfire</c> section in appsettings.
/// </summary>
/// <remarks>
/// Jobs are matched by recurring job id, <c>Type.Method</c>, class name or full type name.
/// </remarks>
public class ContentFreezeHangfireOptions
{
    public const string SectionName = "ContentFreeze:Hangfire";

    /// <summary>
    /// Hold back every Hangfire job during a freeze, except <see cref="ExceptJobs"/>. When false, only <see cref="Jobs"/>.
    /// </summary>
    public bool PauseAllJobs { get; set; } = true;

    /// <summary>Jobs to hold back when <see cref="PauseAllJobs"/> is false.</summary>
    public string[] Jobs { get; set; } = [];

    /// <summary>Jobs that keep running when <see cref="PauseAllJobs"/> is true.</summary>
    public string[] ExceptJobs { get; set; } = [];

    /// <summary>
    /// How long a held-back job waits before it is checked again. Jobs never wait past the freeze's end time,
    /// so this only matters when a freeze ends early.
    /// </summary>
    public int RecheckMinutes { get; set; } = 5;
}
