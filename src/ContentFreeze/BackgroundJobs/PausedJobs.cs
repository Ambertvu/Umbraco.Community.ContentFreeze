using ContentFreeze.Configuration;
using ContentFreeze.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.BackgroundJobs;
using Umbraco.Cms.Infrastructure.Services;

namespace ContentFreeze.BackgroundJobs;

/// <summary>
/// Decides which Umbraco background jobs skip their runs during a freeze (<see cref="ContentFreezeOptions.PausedJobs"/>).
/// </summary>
internal class JobPauser
{
    // Cache sync and our own bookkeeping must keep running, whatever the configuration says.
    private static readonly string[] ProtectedJobs = [nameof(FreezeMaintenanceJob), "InstructionProcessJob", "TouchServerJob"];

    private readonly IFreezeService _freezeService;
    private readonly IOptionsMonitor<ContentFreezeOptions> _options;
    private readonly ILogger<JobPauser> _logger;

    public JobPauser(IFreezeService freezeService, IOptionsMonitor<ContentFreezeOptions> options, ILogger<JobPauser> logger)
    {
        _freezeService = freezeService;
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured(object job, string? jobName = null)
    {
        Type type = job.GetType();
        if (ProtectedJobs.Contains(type.Name))
        {
            return false;
        }

        return _options.CurrentValue.PausedJobs.Any(entry =>
            string.Equals(entry, type.Name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry, type.FullName, StringComparison.OrdinalIgnoreCase)
            || (jobName is not null && string.Equals(entry, jobName, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>True when the job is configured to pause and a freeze is active right now.</summary>
    public bool ShouldSkip(object job, string? jobName = null)
    {
        if (!IsConfigured(job, jobName) || !_freezeService.IsFrozen)
        {
            return false;
        }

        _logger.LogInformation("Skipped background job {Job}: a content freeze is active", jobName ?? job.GetType().Name);
        return true;
    }
}

/// <summary>
/// Wraps a recurring job so a run is skipped during a freeze. The next run follows the job's normal period.
/// </summary>
internal sealed class PausableRecurringJob : IRecurringBackgroundJob
{
    private readonly IRecurringBackgroundJob _inner;
    private readonly JobPauser _pauser;

    public PausableRecurringJob(IRecurringBackgroundJob inner, JobPauser pauser)
    {
        _inner = inner;
        _pauser = pauser;
    }

    public TimeSpan Period => _inner.Period;

    public TimeSpan Delay => _inner.Delay;

    public ServerRole[] ServerRoles => _inner.ServerRoles;

    public event EventHandler PeriodChanged
    {
        add => _inner.PeriodChanged += value;
        remove => _inner.PeriodChanged -= value;
    }

    public Task RunJobAsync() => _pauser.ShouldSkip(_inner) ? Task.CompletedTask : _inner.RunJobAsync();
}

/// <summary>
/// Hands out a no-op stand-in for paused distributed jobs. The run counts as done, so the next one follows the job's period.
/// </summary>
internal sealed class PausingDistributedJobService : IDistributedJobService
{
    private readonly IDistributedJobService _inner;
    private readonly JobPauser _pauser;

    public PausingDistributedJobService(IDistributedJobService inner, JobPauser pauser)
    {
        _inner = inner;
        _pauser = pauser;
    }

    public async Task<IDistributedBackgroundJob?> TryTakeRunnableAsync()
    {
        IDistributedBackgroundJob? job = await _inner.TryTakeRunnableAsync();
        return job is not null && _pauser.ShouldSkip(job, job.Name) ? new SkippedDistributedJob(job) : job;
    }

    public Task FinishAsync(string jobName) => _inner.FinishAsync(jobName);

    public Task EnsureJobsAsync() => _inner.EnsureJobsAsync();

    private sealed class SkippedDistributedJob : IDistributedBackgroundJob
    {
        private readonly IDistributedBackgroundJob _job;

        public SkippedDistributedJob(IDistributedBackgroundJob job) => _job = job;

        public string Name => _job.Name;

        public TimeSpan Period => _job.Period;

        public Task ExecuteAsync() => Task.CompletedTask;
    }
}

internal static class PausedJobsRegistration
{
    /// <summary>
    /// Hooks into the places where Umbraco resolves its jobs at runtime, so jobs registered after this
    /// composer (by other packages or the site) are covered too.
    /// </summary>
    public static IUmbracoBuilder AddPausableJobs(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<JobPauser>();

        builder.Services.Replace(ServiceDescriptor.Singleton<Func<IRecurringBackgroundJob, IHostedService>>(services =>
        {
            Func<IRecurringBackgroundJob, IHostedService> create = RecurringBackgroundJobHostedService.CreateHostedServiceFactory(services);
            JobPauser pauser = services.GetRequiredService<JobPauser>();
            return job => create(pauser.IsConfigured(job) ? new PausableRecurringJob(job, pauser) : job);
        }));

        ServiceDescriptor? original = builder.Services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IDistributedJobService));
        if (original is not null)
        {
            builder.Services.Remove(original);
            builder.Services.Add(new ServiceDescriptor(
                typeof(IDistributedJobService),
                services => new PausingDistributedJobService(CreateInner(services, original), services.GetRequiredService<JobPauser>()),
                original.Lifetime));
        }

        return builder;
    }

    private static IDistributedJobService CreateInner(IServiceProvider services, ServiceDescriptor descriptor)
        => (IDistributedJobService)(descriptor.ImplementationInstance
            ?? descriptor.ImplementationFactory?.Invoke(services)
            ?? ActivatorUtilities.CreateInstance(services, descriptor.ImplementationType!));
}
