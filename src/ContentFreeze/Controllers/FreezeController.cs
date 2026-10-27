using Asp.Versioning;
using ContentFreeze.Authorization;
using ContentFreeze.Configuration;
using ContentFreeze.Models;
using ContentFreeze.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

namespace ContentFreeze.Controllers;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Freeze")]
public class FreezeController : ContentFreezeApiControllerBase
{
    private const int HistoryLimit = 50;

    private readonly IFreezeService _freezeService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IOptionsMonitor<ContentFreezeOptions> _options;

    public FreezeController(
        IFreezeService freezeService,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IOptionsMonitor<ContentFreezeOptions> options)
    {
        _freezeService = freezeService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _options = options;
    }

    private IUser? CurrentUser => _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;

    /// <summary>The scheduled or active freeze, if any. Polled by the backoffice and usable by pipelines.</summary>
    [HttpGet("freeze/status")]
    [ProducesResponseType<FreezeStatusResponseModel>(StatusCodes.Status200OK)]
    public FreezeStatusResponseModel Status()
    {
        DateTime now = _freezeService.UtcNow;
        Freeze? freeze = _freezeService.GetCurrent();
        return new FreezeStatusResponseModel
        {
            ServerTime = FreezeResponseModel.Utc(now),
            Freeze = freeze is null ? null : FreezeResponseModel.From(freeze, now),
            EnforcementDisabled = _options.CurrentValue.Disabled,
            CanManage = CurrentUser.HasPermission(ContentFreezePermissions.Manage),
            CanBypass = CurrentUser.HasPermission(ContentFreezePermissions.Bypass),
        };
    }

    /// <summary>The times of the scheduled or active freeze, for the notice on the login screen.</summary>
    [AllowAnonymous]
    [HttpGet("freeze/login-status")]
    [ProducesResponseType<FreezeLoginStatusResponseModel>(StatusCodes.Status200OK)]
    public FreezeLoginStatusResponseModel LoginStatus()
    {
        DateTime now = _freezeService.UtcNow;
        ContentFreezeOptions options = _options.CurrentValue;
        Freeze? freeze = options.ShowOnLoginScreen && options.Disabled is false ? _freezeService.GetCurrent() : null;
        return new FreezeLoginStatusResponseModel
        {
            ServerTime = FreezeResponseModel.Utc(now),
            State = freeze?.GetState(now),
            StartsAt = freeze is null ? null : FreezeResponseModel.Utc(freeze.StartsAt),
            EndsAt = freeze is null ? null : FreezeResponseModel.Utc(freeze.EndsAt),
        };
    }

    /// <summary>A freeze by key, id or correlation id.</summary>
    [HttpGet("freeze/{id}")]
    [ProducesResponseType<FreezeResponseModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Get(string id)
    {
        Freeze? freeze = _freezeService.Find(id);
        return freeze is null
            ? NotFound(Problem("No freeze found with that id.", StatusCodes.Status404NotFound))
            : Ok(FreezeResponseModel.From(freeze, _freezeService.UtcNow));
    }

    /// <summary>
    /// 200 when the freeze is active, 409 otherwise. Call before a critical pipeline step, so the step fails
    /// when someone ended the freeze early.
    /// </summary>
    [HttpGet("freeze/{id}/assert-active")]
    [ProducesResponseType<FreezeResponseModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public IActionResult AssertActive(string id)
    {
        Freeze? freeze = _freezeService.Find(id);
        if (freeze is null)
        {
            return NotFound(Problem("No freeze found with that id.", StatusCodes.Status404NotFound));
        }

        // Read fresh from the database: this guards a migration step.
        DateTime now = _freezeService.UtcNow;
        FreezeState state = freeze.GetState(now);
        return state == FreezeState.Active
            ? Ok(FreezeResponseModel.From(freeze, now))
            : Conflict(Problem($"The freeze is not active (state: {state}).", StatusCodes.Status409Conflict));
    }

    [HttpGet("freeze/{id}/details")]
    [Authorize(Policy = ContentFreezeAuthorizationPolicies.Manage)]
    [ProducesResponseType<FreezeDetailsResponseModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Details(string id)
    {
        Freeze? freeze = _freezeService.Find(id);
        if (freeze is null)
        {
            return NotFound(Problem("No freeze found with that id.", StatusCodes.Status404NotFound));
        }

        return Ok(new FreezeDetailsResponseModel
        {
            Freeze = FreezeResponseModel.From(freeze, _freezeService.UtcNow),
            Audit = _freezeService.GetAudit(freeze.Id).Select(entry => new FreezeAuditEntryResponseModel
            {
                Action = entry.Action,
                Timestamp = FreezeResponseModel.Utc(entry.Timestamp),
                Actor = FreezeActorResponseModel.From(entry.Actor),
                Details = entry.Details,
            }),
        });
    }

    /// <summary>The most recent freezes, newest first.</summary>
    [HttpGet("freeze/history")]
    [Authorize(Policy = ContentFreezeAuthorizationPolicies.Manage)]
    [ProducesResponseType<IEnumerable<FreezeResponseModel>>(StatusCodes.Status200OK)]
    public IEnumerable<FreezeResponseModel> History(int count = 20)
    {
        DateTime now = _freezeService.UtcNow;
        return _freezeService.GetRecent(Math.Clamp(count, 1, HistoryLimit)).Select(freeze => FreezeResponseModel.From(freeze, now));
    }

    /// <summary>
    /// Schedules a freeze, or starts one right away. Returns 201 for a new freeze, 200 when a freeze with the same
    /// correlation id is already scheduled or active, and 409 when another freeze is.
    /// </summary>
    [HttpPost("freeze")]
    [Authorize(Policy = ContentFreezeAuthorizationPolicies.Manage)]
    [ProducesResponseType<FreezeOperationResponseModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<FreezeOperationResponseModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<FreezeOperationResponseModel>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Schedule(ScheduleFreezeRequestModel model)
    {
        FreezeResult result = await _freezeService.ScheduleAsync(
            new ScheduleFreezeRequest(model.StartsInMinutes, model.StartsAt, model.DurationMinutes, model.Reason, model.CorrelationId, model.Force),
            GetActor());

        return ToActionResult(result);
    }

    /// <summary>Moves the end of a scheduled or active freeze.</summary>
    [HttpPost("freeze/{id}/extend")]
    [Authorize(Policy = ContentFreezeAuthorizationPolicies.Manage)]
    [ProducesResponseType<FreezeOperationResponseModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<FreezeOperationResponseModel>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Extend(string id, ExtendFreezeRequestModel model)
        => ToActionResult(await _freezeService.ExtendAsync(id, model.Minutes, GetActor(), model.Reason));

    /// <summary>
    /// Ends an active freeze, or cancels a scheduled one. Always 200 for a freeze that already ended or does not
    /// exist, so a pipeline's cleanup step never fails on it.
    /// </summary>
    [HttpPost("freeze/{id}/end")]
    [Authorize(Policy = ContentFreezeAuthorizationPolicies.Manage)]
    [ProducesResponseType<FreezeOperationResponseModel>(StatusCodes.Status200OK)]
    public async Task<IActionResult> End(string id, EndFreezeRequestModel? model)
        => ToActionResult(await _freezeService.EndAsync(id, GetActor(), model?.Reason));

    /// <summary>Cancels a scheduled freeze. An active freeze returns 409: end it instead.</summary>
    [HttpDelete("freeze/{id}")]
    [Authorize(Policy = ContentFreezeAuthorizationPolicies.Manage)]
    [ProducesResponseType<FreezeOperationResponseModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<FreezeOperationResponseModel>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(string id, string? reason = null)
        => ToActionResult(await _freezeService.CancelAsync(id, GetActor(), reason));

    private FreezeActor GetActor()
    {
        IUser? user = CurrentUser;
        return user is null ? FreezeActor.System : new FreezeActor(user.Key, user.Name ?? user.Username, user.Kind == UserKind.Api);
    }

    private IActionResult ToActionResult(FreezeResult result)
    {
        var body = new FreezeOperationResponseModel
        {
            Status = result.Status.ToString(),
            Message = result.Message,
            Freeze = result.Freeze is null ? null : FreezeResponseModel.From(result.Freeze, _freezeService.UtcNow),
        };

        return result.Status switch
        {
            FreezeResultStatus.Created => StatusCode(StatusCodes.Status201Created, body),
            FreezeResultStatus.Conflict => Conflict(body),
            FreezeResultStatus.Invalid => BadRequest(Problem(result.Message ?? "Invalid request.", StatusCodes.Status400BadRequest)),
            FreezeResultStatus.NotFound => NotFound(Problem(result.Message ?? "Not found.", StatusCodes.Status404NotFound)),
            _ => Ok(body),
        };
    }

    private static ProblemDetails Problem(string detail, int status)
        => new() { Title = "Content freeze", Detail = detail, Status = status };
}
