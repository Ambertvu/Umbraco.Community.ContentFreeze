using System.Text.Json;
using ContentFreeze.Configuration;
using ContentFreeze.Controllers;
using ContentFreeze.Models;
using ContentFreeze.Tests.Support;
using Moq;
using Umbraco.Cms.Core.Security;

namespace ContentFreeze.Tests;

public class LoginStatusTests
{
    private readonly TestFreezeService _test = new();

    private FreezeLoginStatusResponseModel LoginStatus()
        => new FreezeController(
                _test.Service,
                Mock.Of<IBackOfficeSecurityAccessor>(),
                new StaticOptionsMonitor<ContentFreezeOptions>(_test.Options))
            .LoginStatus();

    [Fact]
    public async Task Shows_the_times_of_the_current_freeze()
    {
        Freeze freeze = await _test.ScheduleAsync(startsInMinutes: 5);

        FreezeLoginStatusResponseModel status = LoginStatus();

        Assert.Equal(FreezeState.Scheduled, status.State);
        Assert.Equal(freeze.StartsAt, status.StartsAt!.Value.UtcDateTime);
        Assert.Equal(freeze.EndsAt, status.EndsAt!.Value.UtcDateTime);
    }

    [Fact]
    public async Task Never_exposes_the_reason_or_who_started_it()
    {
        await _test.ScheduleAsync();

        var json = JsonSerializer.Serialize(LoginStatus());

        Assert.DoesNotContain("Release", json);
        Assert.DoesNotContain("Pipeline", json);
    }

    [Fact]
    public async Task Shows_nothing_when_switched_off()
    {
        await _test.ScheduleAsync();
        _test.Options.ShowOnLoginScreen = false;

        Assert.Null(LoginStatus().State);
    }

    [Fact]
    public async Task Shows_nothing_when_enforcement_is_disabled()
    {
        await _test.ScheduleAsync();
        _test.Options.Disabled = true;

        Assert.Null(LoginStatus().State);
    }
}
