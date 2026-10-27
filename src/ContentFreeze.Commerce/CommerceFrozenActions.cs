using ContentFreeze.Enforcement;
using Microsoft.Extensions.Options;

namespace ContentFreeze.Commerce;

/// <summary>
/// The Commerce backoffice actions to hide during a freeze. Aliases that a Commerce version does not have are ignored.
/// </summary>
internal class CommerceFrozenActions : IFrozenActionsProvider
{
    private static readonly string[] SettingsEntities =
    [
        "Store", "Currency", "Country", "Region", "TaxClass", "TaxCalculationMethod", "PaymentMethod", "ShippingMethod",
        "OrderStatus", "Location", "EmailTemplate", "PrintTemplate", "ExportTemplate", "ProductAttribute", "ProductAttributePreset",
    ];

    // Workspaces whose save action is named "<workspace alias>.Action.Save".
    private static readonly string[] SettingsWorkspaces =
    [
        "StoreSettings", "Currency", "Country", "Region", "TaxClass", "Location", "EmailTemplate", "PrintTemplate", "ExportTemplate",
    ];

    private readonly IOptions<ContentFreezeCommerceOptions> _options;

    public CommerceFrozenActions(IOptions<ContentFreezeCommerceOptions> options) => _options = options;

    public IEnumerable<string> GetFrozenActionAliases()
    {
        ContentFreezeCommerceOptions options = _options.Value;
        var aliases = new List<string>();

        if (options.FreezeSettings)
        {
            aliases.AddRange(SettingsEntities.SelectMany(EntityAliases));
            aliases.AddRange(SettingsWorkspaces.Select(workspace => $"Uc.Workspace.{workspace}.Action.Save"));
        }

        if (options.FreezeDiscounts)
        {
            aliases.AddRange([.. EntityAliases("Discount"), "Uc.Workspace.Discount.Action.Save"]);
        }

        if (options.FreezeGiftCards)
        {
            aliases.AddRange([.. EntityAliases("GiftCard"), "Uc.Workspace.GiftCard.Action.Save"]);
        }

        if (options.FreezeCustomers)
        {
            aliases.AddRange(["Uc.WorkspaceAction.Customer.Save", "Uc.EntityAction.Customer.Create", "Uc.CollectionAction.Customer.Create"]);
        }

        if (options.FreezeOrders)
        {
            aliases.AddRange(
            [
                "Uc.WorkspaceAction.Order.Save",
                "Uc.EntityQuickAction.ChangeStatus",
                "Uc.EntityAction.Order.Delete",
                "Uc.EntityBulkAction.Order.Delete",
                "Uc.WorkspaceAction.Cart.Save",
                "Uc.EntityAction.Cart.Create",
                "Uc.EntityAction.Cart.Delete",
                "Uc.EntityBulkAction.Cart.Delete",
                "Uc.CollectionAction.Cart.Create",
            ]);
        }

        return aliases;
    }

    private static IEnumerable<string> EntityAliases(string entity) =>
    [
        $"Uc.WorkspaceAction.{entity}.Save",
        $"Uc.EntityAction.{entity}.Create",
        $"Uc.EntityAction.{entity}.Delete",
        $"Uc.EntityAction.{entity}.Sort",
        $"Uc.EntityBulkAction.{entity}.Delete",
        $"Uc.CollectionAction.{entity}.Create",
    ];
}
