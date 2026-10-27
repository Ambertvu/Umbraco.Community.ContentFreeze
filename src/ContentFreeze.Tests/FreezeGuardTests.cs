using ContentFreeze.Configuration;
using ContentFreeze.Enforcement;
using ContentFreeze.Models;
using ContentFreeze.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

namespace ContentFreeze.Tests;

public class FreezeGuardTests
{
    private readonly TestFreezeService _test = new();
    private IUser? _currentUser = CreateUser();

    private FreezeGuard CreateGuard()
    {
        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(s => s.CurrentUser).Returns(() => _currentUser);
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(a => a.BackOfficeSecurity).Returns(security.Object);

        return new FreezeGuard(
            _test.Service,
            accessor.Object,
            new StaticOptionsMonitor<ContentFreezeOptions>(_test.Options),
            NullLogger<FreezeGuard>.Instance);
    }

    private static IUser CreateUser(params string[] permissions)
    {
        var group = new Mock<IReadOnlyUserGroup>();
        group.Setup(g => g.Permissions).Returns(new HashSet<string>(permissions));
        var user = new Mock<IUser>();
        user.Setup(u => u.Groups).Returns([group.Object]);
        user.Setup(u => u.Name).Returns("Editor");
        return user.Object;
    }

    [Fact]
    public void Nothing_is_blocked_without_a_freeze()
        => Assert.Null(CreateGuard().GetBlockingFreeze("save"));

    [Fact]
    public async Task Nothing_is_blocked_while_the_freeze_is_only_scheduled()
    {
        await _test.ScheduleAsync(startsInMinutes: 5);

        Assert.Null(CreateGuard().GetBlockingFreeze("save"));
    }

    [Fact]
    public async Task An_editor_is_blocked_during_an_active_freeze()
    {
        Freeze freeze = await _test.ScheduleAsync();

        Assert.Same(freeze, CreateGuard().GetBlockingFreeze("save"));
    }

    [Fact]
    public async Task An_editor_with_the_bypass_permission_is_not_blocked()
    {
        await _test.ScheduleAsync();
        _currentUser = CreateUser(ContentFreezePermissions.Bypass);

        Assert.Null(CreateGuard().GetBlockingFreeze("save"));
    }

    [Theory]
    [InlineData(EnforcementMode.BackOfficeOnly, false)]
    [InlineData(EnforcementMode.Strict, true)]
    public async Task Writes_without_a_user_are_blocked_only_in_strict_mode(EnforcementMode mode, bool blocked)
    {
        await _test.ScheduleAsync();
        _test.Options.Mode = mode;
        _currentUser = null;

        Assert.Equal(blocked, CreateGuard().GetBlockingFreeze("save") is not null);
    }

    [Fact]
    public async Task AllowWrites_lets_writes_through_in_strict_mode()
    {
        await _test.ScheduleAsync();
        _test.Options.Mode = EnforcementMode.Strict;
        _currentUser = null;

        using (_test.Service.AllowWrites("import"))
        {
            Assert.Null(CreateGuard().GetBlockingFreeze("save"));
        }
    }

    [Fact]
    public async Task The_disabled_switch_turns_enforcement_off()
    {
        await _test.ScheduleAsync();
        _test.Options.Disabled = true;

        Assert.Null(CreateGuard().GetBlockingFreeze("save"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Writes_from_uSync_follow_the_AllowUSyncImports_setting(bool allowUSyncImports, bool blocked)
    {
        await _test.ScheduleAsync();
        _test.Options.AllowUSyncImports = allowUSyncImports;

        Freeze? result = uSync.Fake.FakeImporter.Import(() => CreateGuard().GetBlockingFreeze("save"));

        Assert.Equal(blocked, result is not null);
    }

    [Fact]
    public async Task Blocked_attempts_are_counted()
    {
        Freeze freeze = await _test.ScheduleAsync();
        FreezeGuard guard = CreateGuard();

        guard.GetBlockingFreeze("save");
        guard.GetBlockingFreeze("publish");
        _test.Service.FlushBlockedAttempts();

        Assert.Equal(2, freeze.BlockedAttempts);
    }

    [Fact]
    public async Task The_message_names_the_reason()
    {
        Freeze freeze = await _test.ScheduleAsync();

        Assert.Equal("Editing is temporarily blocked (Release). Your change was not saved.", CreateGuard().GetBlockedMessage(freeze));
    }
}
