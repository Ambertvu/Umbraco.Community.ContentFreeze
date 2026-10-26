using ContentFreeze.Configuration;
using ContentFreeze.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ContentFreeze.Enforcement;

/// <summary>
/// Blocks writes to document blueprints. Umbraco raises no cancelable notification for blueprints,
/// so these are stopped at the Management API instead.
/// </summary>
internal class BlueprintFreezeFilter : IAsyncActionFilter
{
    private const string BlueprintControllersNamespace = "Umbraco.Cms.Api.Management.Controllers.DocumentBlueprint";

    private readonly IFreezeGuard _guard;
    private readonly IOptionsMonitor<ContentFreezeOptions> _options;

    public BlueprintFreezeFilter(IFreezeGuard guard, IOptionsMonitor<ContentFreezeOptions> options)
    {
        _guard = guard;
        _options = options;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_options.CurrentValue.FreezeBlueprints
            && IsWrite(context.HttpContext.Request.Method)
            && context.ActionDescriptor is ControllerActionDescriptor action
            && action.ControllerTypeInfo.Namespace?.StartsWith(BlueprintControllersNamespace, StringComparison.Ordinal) is true)
        {
            Freeze? freeze = _guard.GetBlockingFreeze("blueprint change");
            if (freeze is not null)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Title = "Content freeze",
                    Detail = _guard.GetBlockedMessage(freeze),
                    Status = StatusCodes.Status400BadRequest,
                    Type = "Error",
                })
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                };
                return;
            }
        }

        await next();
    }

    private static bool IsWrite(string method)
        => HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method);
}
