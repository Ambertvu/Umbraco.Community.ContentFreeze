using ContentFreeze.Enforcement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace ContentFreeze.AI;

/// <summary>
/// Stops Umbraco AI configuration changes at its Management API. The services raise cancelable notifications, but
/// most controllers answer a cancelled one with a 500 (and a cancelled rollback with a 404), so the request is stopped
/// before the controller runs. Matching by namespace covers Prompt and Agent without referencing those packages.
/// </summary>
/// <remarks>
/// Only Create, Update and Delete controllers and rollbacks are matched: the same APIs also run prompts and agents,
/// chat, embeddings, image generation and tests, which must keep working.
/// </remarks>
internal class AIFreezeRule : IFreezeEndpointRule
{
    private static readonly string[] ManagementApiNamespaces =
    [
        "Umbraco.AI.Web.Api.Management.",
        "Umbraco.AI.Prompt.Web.Api.Management.",
        "Umbraco.AI.Agent.Web.Api.Management.",
    ];

    private static readonly string[] ConfigurationAreas = ["Connection", "Profile", "Context", "Guardrail", "Settings", "Prompt", "Agent"];

    private static readonly string[] WriteVerbs = ["Create", "Update", "Delete"];

    private const string RollbackController = "EntityVersionHistoryController";

    private readonly IOptionsMonitor<ContentFreezeAIOptions> _options;

    public AIFreezeRule(IOptionsMonitor<ContentFreezeAIOptions> options) => _options = options;

    public string? GetOperation(ControllerActionDescriptor action, HttpRequest request)
    {
        var ns = action.ControllerTypeInfo.Namespace;
        var prefix = ns is null ? null : ManagementApiNamespaces.FirstOrDefault(prefix => ns.StartsWith(prefix, StringComparison.Ordinal));
        if (prefix is null)
        {
            return null;
        }

        ContentFreezeAIOptions options = _options.CurrentValue;
        var controller = action.ControllerTypeInfo.Name;

        if (controller == RollbackController)
        {
            var entityType = request.RouteValues["entityType"]?.ToString();
            var isTest = string.Equals(entityType, "test", StringComparison.OrdinalIgnoreCase);
            return (isTest ? options.FreezeTests : options.FreezeConfiguration) ? "ai rollback" : null;
        }

        var area = ns![prefix.Length..].Split('.')[0];
        if (WriteVerbs.Any(verb => controller.StartsWith(verb + area, StringComparison.Ordinal)) is false)
        {
            return null;
        }

        return area switch
        {
            "Test" => options.FreezeTests ? "ai test change" : null,
            _ when ConfigurationAreas.Contains(area) => options.FreezeConfiguration ? $"ai {area.ToLowerInvariant()} change" : null,
            _ => null,
        };
    }
}
