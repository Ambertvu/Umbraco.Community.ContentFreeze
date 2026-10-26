using Umbraco.Cms.Core.Models.Membership;

namespace ContentFreeze;

/// <summary>
/// User group permission verbs. Admins toggle them per user group in the Users section.
/// </summary>
public static class ContentFreezePermissions
{
    /// <summary>Schedule, extend, end and cancel freezes (dashboard and Management API).</summary>
    public const string Manage = "ContentFreeze.Manage";

    /// <summary>Keep editing while a freeze is active.</summary>
    public const string Bypass = "ContentFreeze.Bypass";

    public static bool HasPermission(this IUser? user, string verb)
        => user?.Groups.Any(group => group.Permissions.Contains(verb)) is true;
}
