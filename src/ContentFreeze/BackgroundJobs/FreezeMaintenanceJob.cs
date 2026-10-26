using ContentFreeze.Configuration;
using ContentFreeze.Models;
using ContentFreeze.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace ContentFreeze.BackgroundJobs;

/// <summary>
/// Runs on every server:
/// <list type="bullet">
/// <item>holds back scheduled publishing while a freeze is active (Umbraco's job may run on any server);</item>
/// <item>stores the blocked-attempt counts this server collected;</item>
/// <item>on the scheduling server only: records starts and expiries and raises their notifications and webhooks.</item>
/// </list>
/// </summary>
internal class FreezeMaintenanceJob : IRecurringBackgroundJob
{
    // Only resume scheduled publishing if this package suspended it, never when something else did.
    private static bool _suspendedScheduledPublishing;

    private readonly FreezeService _freezeService;
    private readonly IServerRoleAccessor _serverRoleAccessor;
    private readonly IOptionsMonitor<ContentFreezeOptions> _options;
    private readonly ILogger<FreezeMaintenanceJob> _logger;

    public FreezeMaintenanceJob(
        FreezeService freezeService,
        IServerRoleAccessor serverRoleAccessor,
        IOptionsMonitor<ContentFreezeOptions> options,
        ILogger<FreezeMaintenanceJob> logger)
    {
        _freezeService = freezeService;
        _serverRoleAccessor = serverRoleAccessor;
        _options = options;
        _logger = logger;
    }

    public TimeSpan Period => TimeSpan.FromSeconds(10);

    public TimeSpan Delay => TimeSpan.FromSeconds(15);

    public ServerRole[] ServerRoles => Enum.GetValues<ServerRole>();

    // Period is fixed.
    public event EventHandler PeriodChanged { add { } remove { } }

    public async Task RunJobAsync()
    {
        try
        {
            _freezeService.FlushBlockedAttempts();
            UpdateScheduledPublishing();

            if (_serverRoleAccessor.CurrentServerRole is ServerRole.Single or ServerRole.SchedulingPublisher)
            {
                await _freezeService.ProcessTransitionsAsync();
            }
        }
        catch (Exception ex)
        {
            // Catch everything so the job keeps repeating.
            _logger.LogError(ex, "Content freeze maintenance failed");
        }
    }

    private void UpdateScheduledPublishing()
    {
        ContentFreezeOptions options = _options.CurrentValue;
        Freeze? freeze = _freezeService.GetCurrentFromDatabase();
        var shouldDefer = options.DeferScheduledPublishing
            && options.Disabled is false
            && freeze?.GetState(_freezeService.UtcNow) == FreezeState.Active;

        if (shouldDefer && _suspendedScheduledPublishing is false)
        {
            Suspendable.ScheduledPublishing.Suspend();
            _suspendedScheduledPublishing = true;
        }
        else if (shouldDefer is false && _suspendedScheduledPublishing)
        {
            // Overdue releases and expiries are picked up by the next scheduled publishing run.
            Suspendable.ScheduledPublishing.Resume();
            _suspendedScheduledPublishing = false;
        }
    }
}
