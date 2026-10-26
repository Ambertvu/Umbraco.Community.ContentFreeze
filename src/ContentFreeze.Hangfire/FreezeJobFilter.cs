using ContentFreeze.Models;
using ContentFreeze.Services;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentFreeze.Hangfire;

/// <summary>
/// Moves a job that is about to run back to Scheduled while a freeze is active, so it runs after the freeze.
/// </summary>
/// <remarks>
/// A recurring job keeps at most one held-back run: its later occurrences during the freeze are deleted,
/// so a job that runs every minute does not fire sixty times when an hour-long freeze ends.
/// </remarks>
internal class FreezeJobFilter : IElectStateFilter
{
    private const string DeferredRecurringJobsKey = "contentfreeze:deferred-recurring";

    private readonly IFreezeService _freezeService;
    private readonly IOptionsMonitor<ContentFreezeHangfireOptions> _options;
    private readonly ILogger<FreezeJobFilter> _logger;

    public FreezeJobFilter(
        IFreezeService freezeService,
        IOptionsMonitor<ContentFreezeHangfireOptions> options,
        ILogger<FreezeJobFilter> logger)
    {
        _freezeService = freezeService;
        _options = options;
        _logger = logger;
    }

    public void OnStateElection(ElectStateContext context)
    {
        // Every job passes through Processing just before it runs, whatever queued it.
        if (context.CandidateState is not ProcessingState || !_freezeService.IsFrozen)
        {
            return;
        }

        Freeze? freeze = _freezeService.GetCurrent();
        Job? job = context.BackgroundJob.Job;
        if (freeze is null || job is null)
        {
            return;
        }

        var recurringJobId = context.GetJobParameter<string>("RecurringJobId", allowStale: true);
        if (!ShouldPause(job, recurringJobId))
        {
            return;
        }

        var jobId = context.BackgroundJob.Id;
        if (recurringJobId is not null && IsAnotherRunHeldBack(context, recurringJobId, jobId))
        {
            context.CandidateState = new DeletedState { Reason = "Content freeze: an earlier run of this recurring job is already waiting" };
            _logger.LogInformation("Deleted Hangfire job {JobId} ({RecurringJobId}): an earlier run is already held back", jobId, recurringJobId);
            return;
        }

        DateTime recheckAt = _freezeService.UtcNow.AddMinutes(Math.Max(1, _options.CurrentValue.RecheckMinutes));
        DateTime enqueueAt = (freeze.EndsAt < recheckAt ? freeze.EndsAt : recheckAt).AddSeconds(5);

        context.CandidateState = new ScheduledState(enqueueAt) { Reason = "Content freeze: held back until the freeze ends" };
        if (recurringJobId is not null)
        {
            context.Transaction.SetRangeInHash(DeferredRecurringJobsKey, [new KeyValuePair<string, string>(recurringJobId, jobId)]);
        }

        _logger.LogInformation("Held back Hangfire job {JobId} ({Job}) until {EnqueueAt:u}: a content freeze is active", jobId, Describe(job), enqueueAt);
    }

    private bool ShouldPause(Job job, string? recurringJobId)
    {
        ContentFreezeHangfireOptions options = _options.CurrentValue;
        return options.PauseAllJobs
            ? !Matches(options.ExceptJobs, job, recurringJobId)
            : Matches(options.Jobs, job, recurringJobId);
    }

    private static bool Matches(string[] entries, Job job, string? recurringJobId)
        => entries.Any(entry =>
            string.Equals(entry, recurringJobId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry, Describe(job), StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry, job.Type.Name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry, job.Type.FullName, StringComparison.OrdinalIgnoreCase));

    private static bool IsAnotherRunHeldBack(ElectStateContext context, string recurringJobId, string jobId)
    {
        Dictionary<string, string>? heldBack = context.Connection.GetAllEntriesFromHash(DeferredRecurringJobsKey);
        if (heldBack is null || !heldBack.TryGetValue(recurringJobId, out var heldBackJobId) || heldBackJobId == jobId)
        {
            return false;
        }

        return context.Connection.GetStateData(heldBackJobId)?.Name == ScheduledState.StateName;
    }

    private static string Describe(Job job) => $"{job.Type.Name}.{job.Method.Name}";
}
