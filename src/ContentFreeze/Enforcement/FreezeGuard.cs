using ContentFreeze.Configuration;
using ContentFreeze.Models;
using ContentFreeze.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

namespace ContentFreeze.Enforcement;

/// <summary>
/// Decides whether a write must be blocked right now.
/// </summary>
public interface IFreezeGuard
{
    /// <summary>
    /// Returns the active freeze that blocks the current write, or null when the write may go ahead.
    /// </summary>
    Freeze? GetBlockingFreeze(string operation);

    /// <summary>The message shown to the user when a write is blocked.</summary>
    string GetBlockedMessage(Freeze freeze);
}

internal class FreezeGuard : IFreezeGuard
{
    private readonly FreezeService _freezeService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IOptionsMonitor<ContentFreezeOptions> _options;
    private readonly ILogger<FreezeGuard> _logger;

    public FreezeGuard(
        FreezeService freezeService,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IOptionsMonitor<ContentFreezeOptions> options,
        ILogger<FreezeGuard> logger)
    {
        _freezeService = freezeService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _options = options;
        _logger = logger;
    }

    public Freeze? GetBlockingFreeze(string operation)
    {
        ContentFreezeOptions options = _options.CurrentValue;
        if (options.Disabled || _freezeService.WritesAllowedInScope)
        {
            return null;
        }

        // Straight from the database: the cache can lag behind on other servers.
        Freeze? freeze = _freezeService.GetCurrentFromDatabase();
        if (freeze is null || freeze.GetState(_freezeService.UtcNow) != FreezeState.Active)
        {
            return null;
        }

        IUser? user = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            if (options.Mode != EnforcementMode.Strict)
            {
                return null;
            }
        }
        else if (user.HasPermission(ContentFreezePermissions.Bypass))
        {
            return null;
        }

        _freezeService.RecordBlockedAttempt(freeze.Id);
        _logger.LogInformation(
            "Content freeze {FreezeId} blocked {Operation} by {User}",
            freeze.Id,
            operation,
            user?.Name ?? "a background process");

        return freeze;
    }

    public string GetBlockedMessage(Freeze freeze)
    {
        // No end time here: the server does not know the editor's time zone. The banner shows it in local time.
        var reason = string.IsNullOrWhiteSpace(freeze.Reason) ? string.Empty : $" ({freeze.Reason})";
        return $"Editing is temporarily blocked{reason}. Your change was not saved.";
    }
}
