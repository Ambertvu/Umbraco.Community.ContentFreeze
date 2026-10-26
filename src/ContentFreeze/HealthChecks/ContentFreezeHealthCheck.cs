using ContentFreeze.Configuration;
using ContentFreeze.Models;
using ContentFreeze.Services;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.HealthChecks;

namespace ContentFreeze.HealthChecks;

[HealthCheck(
    "4B3F2C1D-8E7A-4F6B-9D5C-2A1E0F9B8C7D",
    "Content Freeze",
    Description = "Shows whether content and media editing is currently frozen.",
    Group = "Content Freeze")]
public class ContentFreezeHealthCheck : HealthCheck
{
    private readonly IFreezeService _freezeService;
    private readonly IOptionsMonitor<ContentFreezeOptions> _options;

    public ContentFreezeHealthCheck(IFreezeService freezeService, IOptionsMonitor<ContentFreezeOptions> options)
    {
        _freezeService = freezeService;
        _options = options;
    }

    public override Task<IEnumerable<HealthCheckStatus>> GetStatusAsync()
    {
        var statuses = new List<HealthCheckStatus>();

        if (_options.CurrentValue.Disabled)
        {
            statuses.Add(new HealthCheckStatus("Content freeze enforcement is disabled in appsettings (ContentFreeze:Disabled).")
            {
                ResultType = StatusResultType.Warning,
            });
        }

        Freeze? freeze = _freezeService.GetCurrentFromDatabase();
        DateTime now = _freezeService.UtcNow;
        HealthCheckStatus status = freeze?.GetState(now) switch
        {
            FreezeState.Active => new HealthCheckStatus(
                $"Freeze active since {freeze.StartsAt:u}, expires at {freeze.EndsAt:u}. Started by {freeze.CreatedBy.Name}{Describe(freeze)}.")
            {
                ResultType = StatusResultType.Warning,
            },
            FreezeState.Scheduled => new HealthCheckStatus(
                $"Freeze scheduled from {freeze.StartsAt:u} until {freeze.EndsAt:u} by {freeze.CreatedBy.Name}{Describe(freeze)}.")
            {
                ResultType = StatusResultType.Info,
            },
            _ => new HealthCheckStatus("No freeze is active or scheduled.") { ResultType = StatusResultType.Success },
        };

        statuses.Add(status);
        return Task.FromResult<IEnumerable<HealthCheckStatus>>(statuses);
    }

    public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
        => throw new InvalidOperationException("Content Freeze health check has no actions.");

    private static string Describe(Freeze freeze)
        => string.IsNullOrWhiteSpace(freeze.Reason) ? string.Empty : $" ({freeze.Reason})";
}
