using System.Reflection;
using ContentFreeze.Hangfire;
using ContentFreeze.Models;
using ContentFreeze.Tests.Support;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ContentFreeze.Tests.Hangfire;

public class FreezeJobFilterTests
{
    private const string HeldBackKey = "contentfreeze:deferred-recurring";

    private readonly TestFreezeService _test = new();
    private readonly ContentFreezeHangfireOptions _options = new();
    private readonly Mock<IStorageConnection> _connection = new();
    private readonly Mock<IWriteOnlyTransaction> _transaction = new();

    private FreezeJobFilter CreateFilter()
        => new(_test.Service, new StaticOptionsMonitor<ContentFreezeHangfireOptions>(_options), NullLogger<FreezeJobFilter>.Instance);

    /// <summary>Runs the filter for a job about to start, and returns the state it ends up in.</summary>
    private IState Elect(string jobId = "1", string? recurringJobId = null)
    {
        Dictionary<string, string> parameters = recurringJobId is null
            ? []
            : new() { ["RecurringJobId"] = SerializationHelper.Serialize(recurringJobId) };
        var backgroundJob = new BackgroundJob(jobId, Job.FromExpression(() => ImportJobs.RunImport()), _test.Service.UtcNow, parameters);
        var applyContext = new ApplyStateContext(
            Mock.Of<JobStorage>(),
            _connection.Object,
            _transaction.Object,
            backgroundJob,
            CreateProcessingState(),
            EnqueuedState.StateName);
        var context = new ElectStateContext(applyContext);

        CreateFilter().OnStateElection(context);
        return context.CandidateState;
    }

    // Only Hangfire's worker creates a ProcessingState; its constructor is internal.
    private static ProcessingState CreateProcessingState()
        => (ProcessingState)Activator.CreateInstance(
            typeof(ProcessingState),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: ["server", "worker"],
            culture: null)!;

    [Fact]
    public void Jobs_run_normally_without_a_freeze()
        => Assert.IsType<ProcessingState>(Elect());

    [Fact]
    public async Task Jobs_wait_until_the_freeze_ends()
    {
        Freeze freeze = await _test.ScheduleAsync(durationMinutes: 3);

        var scheduled = Assert.IsType<ScheduledState>(Elect());

        Assert.True(scheduled.EnqueueAt > _test.Service.UtcNow);
        Assert.True(scheduled.EnqueueAt <= freeze.EndsAt.AddSeconds(5));
    }

    [Fact]
    public async Task Long_freezes_are_checked_again_after_the_recheck_interval()
    {
        await _test.ScheduleAsync(durationMinutes: 120);

        var scheduled = Assert.IsType<ScheduledState>(Elect());

        Assert.Equal(_test.Service.UtcNow.AddMinutes(_options.RecheckMinutes).AddSeconds(5), scheduled.EnqueueAt);
    }

    [Fact]
    public async Task Excepted_jobs_keep_running()
    {
        await _test.ScheduleAsync();
        _options.ExceptJobs = ["ImportJobs.RunImport"];

        Assert.IsType<ProcessingState>(Elect());
    }

    [Fact]
    public async Task Only_listed_jobs_wait_when_not_pausing_all()
    {
        await _test.ScheduleAsync();
        _options.PauseAllJobs = false;
        _options.Jobs = ["nightly-import"];

        Assert.IsType<ProcessingState>(Elect());
        Assert.IsType<ScheduledState>(Elect(recurringJobId: "nightly-import"));
    }

    [Fact]
    public async Task A_recurring_job_keeps_one_waiting_run()
    {
        await _test.ScheduleAsync();
        _connection.Setup(c => c.GetAllEntriesFromHash(HeldBackKey)).Returns(new Dictionary<string, string> { ["nightly-import"] = "1" });
        _connection.Setup(c => c.GetStateData("1")).Returns(new StateData { Name = ScheduledState.StateName });

        Assert.IsType<ScheduledState>(Elect(jobId: "1", recurringJobId: "nightly-import"));
        Assert.IsType<DeletedState>(Elect(jobId: "2", recurringJobId: "nightly-import"));
    }

    [Fact]
    public async Task A_held_back_recurring_run_is_remembered()
    {
        await _test.ScheduleAsync();

        Elect(jobId: "7", recurringJobId: "nightly-import");

        _transaction.Verify(t => t.SetRangeInHash(
            HeldBackKey,
            It.Is<IEnumerable<KeyValuePair<string, string>>>(entries => entries.Single().Key == "nightly-import" && entries.Single().Value == "7")));
    }
}

public static class ImportJobs
{
    public static void RunImport()
    {
    }
}
