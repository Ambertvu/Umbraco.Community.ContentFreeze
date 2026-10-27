using ContentFreeze.Enforcement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace ContentFreeze.Commerce;

internal enum CommerceArea
{
    Settings,
    Stock,
    Discounts,
    GiftCards,
    Customers,
    Orders,
}

/// <summary>
/// Stops Commerce changes at its Management API. Commerce's own validation events also run for checkout and payment
/// side effects (gift cards on order finalization, customers on member sign-up), so they cannot be used for this.
/// </summary>
/// <remarks>
/// Only Create, Update, Delete and Sort controllers are matched: the Commerce Management API has many POST and PATCH
/// endpoints that only read or calculate (scaffolds, analytics, cart calculation, template rendering), and payment
/// actions (capture, cancel, refund) and finalizing a cart must keep working. The storefront and payment APIs live in
/// other namespaces and are never matched.
/// </remarks>
internal class CommerceFreezeRule : IFreezeEndpointRule
{
    private const string ManagementApiNamespace = "Umbraco.Commerce.Cms.Web.Api.Management";
    private const string ControllerSuffix = "ManagementApiController";

    // Longest first: AutoCreate before Create.
    private static readonly string[] WriteVerbs = ["AutoCreate", "Create", "Update", "Delete", "Sort"];

    // Matched against the start of the entity name; longer names before the shorter ones they start with.
    private static readonly (string Entity, CommerceArea Area)[] Entities =
    [
        ("Store", CommerceArea.Settings),
        ("Currenc", CommerceArea.Settings),
        ("Countr", CommerceArea.Settings),
        ("Region", CommerceArea.Settings),
        ("TaxClass", CommerceArea.Settings),
        ("TaxCalculationMethod", CommerceArea.Settings),
        ("PaymentMethod", CommerceArea.Settings),
        ("ShippingMethod", CommerceArea.Settings),
        ("OrderStatus", CommerceArea.Settings),
        ("Location", CommerceArea.Settings),
        ("EmailTemplate", CommerceArea.Settings),
        ("PrintTemplate", CommerceArea.Settings),
        ("ExportTemplate", CommerceArea.Settings),
        ("ProductAttribute", CommerceArea.Settings),
        ("Product", CommerceArea.Stock),
        ("Discount", CommerceArea.Discounts),
        ("GiftCard", CommerceArea.GiftCards),
        ("Customer", CommerceArea.Customers),
        ("Order", CommerceArea.Orders),
        ("Cart", CommerceArea.Orders),
    ];

    private readonly IOptionsMonitor<ContentFreezeCommerceOptions> _options;

    public CommerceFreezeRule(IOptionsMonitor<ContentFreezeCommerceOptions> options) => _options = options;

    public string? GetOperation(ControllerActionDescriptor action, HttpRequest request)
    {
        if (action.ControllerTypeInfo.Namespace?.StartsWith(ManagementApiNamespace, StringComparison.Ordinal) is not true)
        {
            return null;
        }

        CommerceArea? area = GetArea(action.ControllerTypeInfo.Name);
        return area is not null && IsFrozen(area.Value) ? $"commerce {area.Value.ToString().ToLowerInvariant()} change" : null;
    }

    internal static CommerceArea? GetArea(string controllerName)
    {
        var suffixAt = controllerName.IndexOf(ControllerSuffix, StringComparison.Ordinal);
        if (suffixAt < 0)
        {
            return null;
        }

        var name = controllerName[..suffixAt];
        var verb = WriteVerbs.FirstOrDefault(verb => name.StartsWith(verb, StringComparison.Ordinal));
        if (verb is null || name.Contains("Scaffold", StringComparison.Ordinal))
        {
            return null;
        }

        var entity = name[verb.Length..];
        foreach ((var prefix, CommerceArea area) in Entities)
        {
            if (entity.StartsWith(prefix, StringComparison.Ordinal))
            {
                return area;
            }
        }

        return null;
    }

    private bool IsFrozen(CommerceArea area)
    {
        ContentFreezeCommerceOptions options = _options.CurrentValue;
        return area switch
        {
            CommerceArea.Settings => options.FreezeSettings,
            CommerceArea.Stock => options.FreezeStock,
            CommerceArea.Discounts => options.FreezeDiscounts,
            CommerceArea.GiftCards => options.FreezeGiftCards,
            CommerceArea.Customers => options.FreezeCustomers,
            CommerceArea.Orders => options.FreezeOrders,
            _ => false,
        };
    }
}
