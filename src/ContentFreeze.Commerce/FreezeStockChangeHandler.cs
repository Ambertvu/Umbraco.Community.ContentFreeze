using ContentFreeze.Enforcement;
using ContentFreeze.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core.Events.Validation;

namespace ContentFreeze.Commerce;

/// <summary>
/// Stops stock from changing when a product save in the backoffice is blocked by a freeze.
/// </summary>
/// <remarks>
/// Commerce copies the stock value from a product to its own stock table while the product is being saved, and does
/// not check whether the save was cancelled. Without this, a blocked save still changes the stock.
/// <para>
/// Stock also changes when orders are finalized or refunded, so this only acts on requests to Umbraco's own
/// Management API, where content is saved. Checkout, payments and Commerce's backoffice are left alone.
/// </para>
/// </remarks>
internal class FreezeStockChangeHandler : ValidationEventHandlerBase<ValidateStockChange>
{
    private const string ContentManagementApiPath = "/umbraco/management/api/";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFreezeGuard _guard;
    private readonly IOptionsMonitor<ContentFreezeCommerceOptions> _options;

    public FreezeStockChangeHandler(IHttpContextAccessor httpContextAccessor, IFreezeGuard guard, IOptionsMonitor<ContentFreezeCommerceOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _guard = guard;
        _options = options;
    }

    public override void Validate(ValidateStockChange evt)
    {
        PathString? path = _httpContextAccessor.HttpContext?.Request.Path;
        if (_options.CurrentValue.FreezeStock is false
            || path?.StartsWithSegments(ContentManagementApiPath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase) is not true)
        {
            return;
        }

        Freeze? freeze = _guard.GetBlockingFreeze("commerce stock change");
        if (freeze is not null)
        {
            evt.Fail(_guard.GetBlockedMessage(freeze));
        }
    }
}
