using ContentFreeze.Configuration;
using ContentFreeze.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ContentFreeze.Enforcement;

/// <summary>
/// Answers Management API writes picked out by an <see cref="IFreezeEndpointRule"/> with a 400 and the freeze
/// message. The backoffice shows a 400 ProblemDetails as an error toast.
/// </summary>
internal class FreezeEndpointFilter : IAsyncActionFilter
{
    private readonly IEnumerable<IFreezeEndpointRule> _rules;
    private readonly IFreezeGuard _guard;

    public FreezeEndpointFilter(IEnumerable<IFreezeEndpointRule> rules, IFreezeGuard guard)
    {
        _rules = rules;
        _guard = guard;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor action && FreezeHttpMethods.IsWrite(context.HttpContext.Request.Method))
        {
            var operation = _rules.Select(rule => rule.GetOperation(action, context.HttpContext.Request)).FirstOrDefault(name => name is not null);
            Freeze? freeze = operation is null ? null : _guard.GetBlockingFreeze(operation);
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
}

/// <summary>
/// Umbraco raises no cancelable notification for document blueprints, so they are stopped at the Management API.
/// </summary>
internal class BlueprintFreezeRule : IFreezeEndpointRule
{
    private const string BlueprintControllersNamespace = "Umbraco.Cms.Api.Management.Controllers.DocumentBlueprint";

    private readonly IOptionsMonitor<ContentFreezeOptions> _options;

    public BlueprintFreezeRule(IOptionsMonitor<ContentFreezeOptions> options) => _options = options;

    public string? GetOperation(ControllerActionDescriptor action, HttpRequest request)
        => _options.CurrentValue.FreezeBlueprints
            && action.ControllerTypeInfo.Namespace?.StartsWith(BlueprintControllersNamespace, StringComparison.Ordinal) is true
                ? "blueprint change"
                : null;
}
