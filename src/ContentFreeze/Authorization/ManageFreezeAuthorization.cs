using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;

namespace ContentFreeze.Authorization;

public static class ContentFreezeAuthorizationPolicies
{
    /// <summary>Requires the <see cref="ContentFreezePermissions.Manage"/> permission on one of the user's groups.</summary>
    public const string Manage = "ContentFreeze.Manage";
}

internal class ManageFreezeRequirement : IAuthorizationRequirement
{
}

internal class ManageFreezeAuthorizationHandler : AuthorizationHandler<ManageFreezeRequirement>
{
    private readonly IAuthorizationHelper _authorizationHelper;

    public ManageFreezeAuthorizationHandler(IAuthorizationHelper authorizationHelper)
        => _authorizationHelper = authorizationHelper;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ManageFreezeRequirement requirement)
    {
        if (_authorizationHelper.TryGetUmbracoUser(context.User, out IUser? user)
            && user.HasPermission(ContentFreezePermissions.Manage))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
