using ContentFreeze.BackgroundJobs;
using ContentFreeze.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.BackgroundJobs;
using Umbraco.Cms.Infrastructure.Services;

namespace ContentFreeze.Tests;

public class PausedJobsTests
{
    private readonly TestFreezeService _test = new();

    private JobPauser CreatePauser(params string[] pausedJobs)
    {
        _test.Options.PausedJobs = pausedJobs;
        return new JobPauser(_test.Service, new StaticOptionsMonitor<Configuration.ContentFreezeOptions>(_test.Options), NullLogger<JobPauser>.Instance);
    }

    [Theory]
    [InlineData("CountingJob")]
    [InlineData("countingjob")]
    [InlineData("ContentFreeze.Tests.PausedJobsTests+CountingJob")]
    public void Jobs_are_matched_by_class_name_or_full_type_name(string entry)
        => Assert.True(CreatePauser(entry).IsConfigured(new CountingJob()));

    [Fact]
    public void Distributed_jobs_are_also_matched_by_their_job_name()
        => Assert.True(CreatePauser("Nightly import").IsConfigured(new CountingDistributedJob(), "Nightly import"));

    [Fact]
    public void Jobs_that_keep_servers_in_sync_cannot_be_paused()
        => Assert.False(CreatePauser("FreezeMaintenanceJob").IsConfigured(new FreezeMaintenanceJob(null!, null!, null!, null!)));

    [Fact]
    public async Task A_paused_recurring_job_skips_its_runs_only_during_a_freeze()
    {
        var job = new CountingJob();
        var pausable = new PausableRecurringJob(job, CreatePauser("CountingJob"));

        await pausable.RunJobAsync();
        await _test.ScheduleAsync();
        await pausable.RunJobAsync();

        Assert.Equal(1, job.Runs);
    }

    [Fact]
    public void A_paused_recurring_job_keeps_its_schedule()
    {
        var job = new CountingJob();
        var pausable = new PausableRecurringJob(job, CreatePauser("CountingJob"));

        Assert.Equal(job.Period, pausable.Period);
        Assert.Equal(job.Delay, pausable.Delay);
        Assert.Equal(job.ServerRoles, pausable.ServerRoles);
    }

    [Fact]
    public async Task A_paused_distributed_job_is_handed_out_as_a_stand_in_that_does_nothing()
    {
        var job = new CountingDistributedJob();
        var inner = new Mock<IDistributedJobService>();
        inner.Setup(service => service.TryTakeRunnableAsync()).ReturnsAsync(job);
        var service = new PausingDistributedJobService(inner.Object, CreatePauser("CountingDistributedJob"));
        await _test.ScheduleAsync();

        IDistributedBackgroundJob? taken = await service.TryTakeRunnableAsync();
        await taken!.ExecuteAsync();

        Assert.NotSame(job, taken);
        Assert.Equal(job.Name, taken.Name);
        Assert.Equal(0, job.Runs);
    }

    [Fact]
    public async Task Other_distributed_jobs_are_handed_out_unchanged()
    {
        var job = new CountingDistributedJob();
        var inner = new Mock<IDistributedJobService>();
        inner.Setup(service => service.TryTakeRunnableAsync()).ReturnsAsync(job);
        var service = new PausingDistributedJobService(inner.Object, CreatePauser("SomethingElse"));
        await _test.ScheduleAsync();

        Assert.Same(job, await service.TryTakeRunnableAsync());
    }

    private sealed class CountingJob : IRecurringBackgroundJob
    {
        public int Runs { get; private set; }

        public TimeSpan Period => TimeSpan.FromMinutes(5);

        public TimeSpan Delay => TimeSpan.FromSeconds(30);

        public ServerRole[] ServerRoles => [ServerRole.Single];

        public event EventHandler PeriodChanged { add { } remove { } }

        public Task RunJobAsync()
        {
            Runs++;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingDistributedJob : IDistributedBackgroundJob
    {
        public int Runs { get; private set; }

        public string Name => "Nightly import";

        public TimeSpan Period => TimeSpan.FromDays(1);

        public Task ExecuteAsync()
        {
            Runs++;
            return Task.CompletedTask;
        }
    }
}
