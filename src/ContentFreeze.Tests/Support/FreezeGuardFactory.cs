using ContentFreeze.Configuration;
using ContentFreeze.Enforcement;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

namespace ContentFreeze.Tests.Support;

internal static class FreezeGuardFactory
{
    /// <summary>A guard for <paramref name="test"/>, with an editor without the bypass permission logged in.</summary>
    public static FreezeGuard Create(TestFreezeService test)
    {
        var group = new Mock<IReadOnlyUserGroup>();
        group.Setup(g => g.Permissions).Returns(new HashSet<string>());
        var user = new Mock<IUser>();
        user.Setup(u => u.Groups).Returns([group.Object]);
        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(s => s.CurrentUser).Returns(user.Object);
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(a => a.BackOfficeSecurity).Returns(security.Object);

        return new FreezeGuard(
            test.Service,
            accessor.Object,
            new StaticOptionsMonitor<ContentFreezeOptions>(test.Options),
            NullLogger<FreezeGuard>.Instance);
    }
}
