namespace ContentFreeze.Commerce;

/// <summary>
/// Settings bound from the <c>ContentFreeze:Commerce</c> section in appsettings. Checkout, payments and payment
/// provider callbacks are never blocked, whatever these say.
/// </summary>
public class ContentFreezeCommerceOptions
{
    public const string SectionName = "ContentFreeze:Commerce";

    /// <summary>
    /// Stores, currencies, countries and regions, tax classes and calculation methods, payment and shipping methods,
    /// order statuses, locations, email, print and export templates, and product attributes and presets.
    /// </summary>
    public bool FreezeSettings { get; set; } = true;

    /// <summary>
    /// Product stock edited in the backoffice. Also stops stock from changing when a blocked product save would
    /// otherwise still pass its stock value on to Commerce.
    /// </summary>
    public bool FreezeStock { get; set; } = true;

    public bool FreezeDiscounts { get; set; }

    public bool FreezeGiftCards { get; set; }

    public bool FreezeCustomers { get; set; }

    /// <summary>
    /// Editing, deleting and changing the status of orders and carts in the backoffice. Capturing, cancelling and
    /// refunding payments, and finalizing a cart, keep working.
    /// </summary>
    public bool FreezeOrders { get; set; }
}
