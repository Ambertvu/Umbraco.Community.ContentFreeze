using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace ContentFreeze.Enforcement;

/// <summary>
/// Picks out Management API actions that change something, for packages without cancelable notifications
/// (or whose controllers handle a cancelled notification badly). Matching requests are answered with a
/// 400 and the freeze message while a freeze blocks the current user.
/// </summary>
/// <remarks>
/// Called for every MVC action, website included, so keep it to cheap checks on the controller type.
/// </remarks>
public interface IFreezeEndpointRule
{
    /// <summary>A short description of the change (for logs), or null when this rule does not apply.</summary>
    string? GetOperation(ControllerActionDescriptor action, HttpRequest request);
}

/// <summary>
/// Backoffice extension aliases (workspace, entity, bulk and collection actions) to hide while editing is frozen.
/// </summary>
/// <remarks>Read once, when the backoffice manifests are built; changing the result needs a restart.</remarks>
public interface IFrozenActionsProvider
{
    IEnumerable<string> GetFrozenActionAliases();
}

/// <summary>
/// Cancels a notification with the freeze message when a freeze blocks the current write.
/// </summary>
public interface IFreezeEnforcer
{
    /// <returns>True when the notification was cancelled.</returns>
    bool Block(ICancelableNotification notification, string operation);
}

public static class FreezeHttpMethods
{
    public static bool IsWrite(string method)
        => HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method) || HttpMethods.IsPatch(method);
}

public static class ContentFreezeBuilderExtensions
{
    public static IUmbracoBuilder AddFreezeEndpointRule<TRule>(this IUmbracoBuilder builder)
        where TRule : class, IFreezeEndpointRule
    {
        builder.Services.AddSingleton<IFreezeEndpointRule, TRule>();
        return builder;
    }

    public static IUmbracoBuilder AddFrozenActions<TProvider>(this IUmbracoBuilder builder)
        where TProvider : class, IFrozenActionsProvider
    {
        builder.Services.AddSingleton<IFrozenActionsProvider, TProvider>();
        return builder;
    }
}
