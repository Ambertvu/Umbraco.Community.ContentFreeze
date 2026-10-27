using ContentFreeze.Commerce;
using ContentFreeze.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Commerce.Common.Models;
using Umbraco.Commerce.Core.Events.Validation;

namespace ContentFreeze.Tests.Commerce;

public class CommerceFreezeTests
{
    private readonly TestFreezeService _test = new();
    private readonly ContentFreezeCommerceOptions _options = new();

    [Theory]
    [InlineData("UpdateStoreManagementApiController", "Settings")]
    [InlineData("SortCurrenciesManagementApiController", "Settings")]
    [InlineData("AutoCreateCountriesAndRegionsManagementApiController", "Settings")]
    [InlineData("DeleteRegionByIdOrCodeManagementApiController", "Settings")]
    [InlineData("DeleteOrderStatusByIdOrAliasManagementApiController", "Settings")]
    [InlineData("CreateProductAttributePresetPresetManagementApiController", "Settings")]
    [InlineData("UpdateProductByReferenceManagementApiController", "Stock")]
    [InlineData("UpdateDiscountManagementApiController", "Discounts")]
    [InlineData("DeleteGiftCardByIdOrCodeManagementApiController", "GiftCards")]
    [InlineData("UpdateCustomerManagementApiController", "Customers")]
    [InlineData("UpdateOrderManagementApiController", "Orders")]
    [InlineData("DeleteCartByIdManagementApiController", "Orders")]
    public void Write_controllers_are_sorted_into_areas(string controller, string area)
        => Assert.Equal(area, CommerceFreezeRule.GetArea(controller)?.ToString());

    [Theory]
    [InlineData("CreateCurrencyScaffoldManagementApiController")]
    [InlineData("GetStoreStatsByIdOrAliasManagementApiController")]
    [InlineData("CalculateCartManagementApiController")]
    [InlineData("FinalizeCartManagementApiController")]
    [InlineData("CaptureOrderPaymentByIdManagementApiController")]
    [InlineData("RefundOrderPaymentByIdManagementApiController")]
    [InlineData("RenderPrintTemplatesManagementApiController")]
    [InlineData("SendEmailTemplateByIdOrAliasManagementApiController")]
    [InlineData("CreateDictionaryItemManagementApiController")] // Blocked by the core package already.
    public void Reads_calculations_and_payment_actions_are_left_alone(string controller)
        => Assert.Null(CommerceFreezeRule.GetArea(controller));

    [Fact]
    public void Only_the_management_api_is_matched()
    {
        var rule = new CommerceFreezeRule(new StaticOptionsMonitor<ContentFreezeCommerceOptions>(_options));
        HttpRequest request = ActionContexts.Request("PUT");

        Assert.Equal(
            "commerce settings change",
            rule.GetOperation(ActionContexts.Action<Umbraco.Commerce.Cms.Web.Api.Management.Controllers.UpdateStoreManagementApiController>(), request));
        Assert.Null(rule.GetOperation(ActionContexts.Action<Umbraco.Commerce.Cms.Web.Api.Storefront.Controllers.UpdateOrderManagementApiController>(), request));
    }

    [Fact]
    public void Areas_follow_their_settings()
    {
        var rule = new CommerceFreezeRule(new StaticOptionsMonitor<ContentFreezeCommerceOptions>(_options));
        HttpRequest request = ActionContexts.Request("PATCH");
        string? Order() => rule.GetOperation(ActionContexts.Action<Umbraco.Commerce.Cms.Web.Api.Management.Controllers.UpdateOrderManagementApiController>(), request);

        Assert.Null(Order());

        _options.FreezeOrders = true;
        Assert.Equal("commerce orders change", Order());
    }

    [Fact]
    public void Hidden_actions_follow_the_settings()
    {
        IEnumerable<string> Aliases() => new CommerceFrozenActions(Options.Create(_options)).GetFrozenActionAliases();

        Assert.Contains("Uc.Workspace.Currency.Action.Save", Aliases());
        Assert.Contains("Uc.WorkspaceAction.PaymentMethod.Save", Aliases());
        Assert.DoesNotContain("Uc.WorkspaceAction.Order.Save", Aliases());

        _options.FreezeOrders = true;
        Assert.Contains("Uc.WorkspaceAction.Order.Save", Aliases());
    }

    [Theory]
    [InlineData("/umbraco/management/api/v1/document/123", true)]
    [InlineData("/umbraco/commerce/management/api/v1/orders/123", false)]
    [InlineData("/umbraco/commerce/storefront/api/v1/order/123", false)]
    [InlineData("/umbraco/commerce/payment/api/v1/callback/abc", false)]
    public async Task Stock_only_stays_put_for_blocked_content_saves(string path, bool fails)
    {
        await _test.ScheduleAsync();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        var handler = new FreezeStockChangeHandler(
            Mock.Of<IHttpContextAccessor>(a => a.HttpContext == httpContext),
            FreezeGuardFactory.Create(_test),
            new StaticOptionsMonitor<ContentFreezeCommerceOptions>(_options));
        var change = new ValidateStockChange(Guid.NewGuid(), "product", new ChangingValue<decimal?>(10, 42));

        handler.Validate(change);

        Assert.Equal(fails, change.HasValidationErrors);
    }

    [Fact]
    public void Stock_changes_without_a_request_are_left_alone()
    {
        var handler = new FreezeStockChangeHandler(
            Mock.Of<IHttpContextAccessor>(),
            FreezeGuardFactory.Create(_test),
            new StaticOptionsMonitor<ContentFreezeCommerceOptions>(_options));
        var change = new ValidateStockChange(Guid.NewGuid(), "product", new ChangingValue<decimal?>(10, 9));

        handler.Validate(change);

        Assert.False(change.HasValidationErrors);
    }
}
