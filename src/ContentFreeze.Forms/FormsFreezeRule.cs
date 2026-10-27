using ContentFreeze.Enforcement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace ContentFreeze.Forms;

/// <summary>
/// Stops writes at the Forms Management API. Forms' controllers handle a cancelled notification badly: a cancelled
/// delete still removes the form's permissions, workflows are saved before the form itself, and several endpoints
/// answer with a 500. Blocking the request before the controller runs avoids all of that.
/// </summary>
/// <remarks>
/// Fails closed: any write in the Management API is blocked unless it is known to only read.
/// Website submissions use other controllers and never get here.
/// </remarks>
internal class FormsFreezeRule : IFreezeEndpointRule
{
    private const string ManagementApiNamespace = "Umbraco.Forms.Web.Api.ManagementApi.";

    // POSTs that only read or validate.
    private static readonly string[] ReadOnlyAreas = ["Analytics", "Export", "Picker"];

    private static readonly string[] ReadOnlyControllers = ["ValidateFieldSettingsController", "ValidateWorkflowSettingsController"];

    private readonly IOptionsMonitor<ContentFreezeFormsOptions> _options;

    public FormsFreezeRule(IOptionsMonitor<ContentFreezeFormsOptions> options) => _options = options;

    public string? GetOperation(ControllerActionDescriptor action, HttpRequest request)
    {
        var ns = action.ControllerTypeInfo.Namespace;
        if (ns is null || ns.StartsWith(ManagementApiNamespace, StringComparison.Ordinal) is false
            || ReadOnlyControllers.Contains(action.ControllerTypeInfo.Name))
        {
            return null;
        }

        var area = ns[ManagementApiNamespace.Length..].Split('.')[0];
        ContentFreezeFormsOptions options = _options.CurrentValue;
        return area switch
        {
            _ when ReadOnlyAreas.Contains(area) => null,
            "Record" => options.FreezeEntries ? "forms entry change" : null,
            "Security" => options.FreezeSecurity ? "forms permission change" : null,
            _ => options.FreezeForms ? $"forms {area.ToLowerInvariant()} change" : null,
        };
    }
}
