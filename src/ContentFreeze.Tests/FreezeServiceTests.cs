using ContentFreeze.Configuration;
using ContentFreeze.Models;
using ContentFreeze.Notifications;
using ContentFreeze.Services;
using ContentFreeze.Tests.Support;
using Moq;

namespace ContentFreeze.Tests;

public class FreezeServiceTests
{
    private readonly TestFreezeService _test = new();

    private FreezeService Service => _test.Service;

    private ScheduleFreezeRequest Request(int startsInMinutes = 0, int? durationMinutes = 30, string? correlationId = "build-1", bool force = false)
        => new(startsInMinutes, null, durationMinutes, "Release", correlationId, force);

    [Fact]
    public async Task Schedule_creates_a_scheduled_freeze_and_announces_it()
    {
        FreezeResult result = await Service.ScheduleAsync(Request(startsInMinutes: 10), TestFreezeService.Pipeline);

        Assert.Equal(FreezeResultStatus.Created, result.Status);
        Assert.Equal(FreezeState.Scheduled, result.Freeze!.GetState(Service.UtcNow));
        _test.Events.Verify(events => events.PublishAsync(It.IsAny<FreezeScheduledNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Schedule_starting_now_is_active_and_audited_as_started()
    {
        FreezeResult result = await Service.ScheduleAsync(Request(), TestFreezeService.Pipeline);

        Assert.Equal(FreezeState.Active, result.Freeze!.GetState(Service.UtcNow));
        Assert.Equal(
            [FreezeAuditAction.Scheduled, FreezeAuditAction.Started],
            Service.GetAudit(result.Freeze.Id).Select(entry => entry.Action));
        _test.Events.Verify(events => events.PublishAsync(It.IsAny<FreezeStartedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Retry_with_the_same_correlation_id_returns_the_existing_freeze()
    {
        FreezeResult first = await Service.ScheduleAsync(Request(), TestFreezeService.Pipeline);
        FreezeResult retry = await Service.ScheduleAsync(Request(), TestFreezeService.Pipeline);

        Assert.Equal(FreezeResultStatus.Existing, retry.Status);
        Assert.Equal(first.Freeze!.Id, retry.Freeze!.Id);
        Assert.Single(_test.Repository.All);
    }

    [Fact]
    public async Task Another_freeze_while_one_is_current_is_a_conflict()
    {
        await Service.ScheduleAsync(Request(), TestFreezeService.Pipeline);

        FreezeResult other = await Service.ScheduleAsync(Request(correlationId: "build-2"), TestFreezeService.Pipeline);

        Assert.Equal(FreezeResultStatus.Conflict, other.Status);
    }

    [Fact]
    public async Task Same_correlation_id_after_the_freeze_ended_creates_a_new_freeze()
    {
        FreezeResult first = await Service.ScheduleAsync(Request(), TestFreezeService.Pipeline);
        await Service.EndAsync("build-1", TestFreezeService.Pipeline, null);

        FreezeResult rerun = await Service.ScheduleAsync(Request(), TestFreezeService.Pipeline);

        Assert.Equal(FreezeResultStatus.Created, rerun.Status);
        Assert.NotEqual(first.Freeze!.Id, rerun.Freeze!.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(481)]
    public async Task Duration_outside_the_allowed_range_is_invalid(int durationMinutes)
    {
        FreezeResult result = await Service.ScheduleAsync(Request(durationMinutes: durationMinutes), TestFreezeService.Pipeline);

        Assert.Equal(FreezeResultStatus.Invalid, result.Status);
        Assert.Empty(_test.Repository.All);
    }

    [Fact]
    public async Task Minimum_warning_time_is_enforced_unless_forced()
    {
        _test.Options.MinimumWarningMinutes = 10;

        FreezeResult tooSoon = await Service.ScheduleAsync(Request(startsInMinutes: 5), TestFreezeService.Pipeline);
        FreezeResult forced = await Service.ScheduleAsync(Request(startsInMinutes: 5, force: true), TestFreezeService.Pipeline);

        Assert.Equal(FreezeResultStatus.Invalid, tooSoon.Status);
        Assert.Equal(FreezeResultStatus.Created, forced.Status);
    }

    [Fact]
    public async Task Extend_moves_the_end_time()
    {
        Freeze freeze = await _test.ScheduleAsync(correlationId: "build-1");
        DateTime endsAt = freeze.EndsAt;

        FreezeResult result = await Service.ExtendAsync("build-1", 15, TestFreezeService.Pipeline, "Migrations");

        Assert.Equal(FreezeResultStatus.Updated, result.Status);
        Assert.Equal(endsAt.AddMinutes(15), result.Freeze!.EndsAt);
    }

    [Fact]
    public async Task Extend_past_the_maximum_duration_is_invalid()
    {
        await _test.ScheduleAsync(durationMinutes: 470, correlationId: "build-1");

        FreezeResult result = await Service.ExtendAsync("build-1", 15, TestFreezeService.Pipeline, null);

        Assert.Equal(FreezeResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task End_of_an_active_freeze_ends_it_early_and_is_idempotent()
    {
        await _test.ScheduleAsync(correlationId: "build-1");

        FreezeResult ended = await Service.EndAsync("build-1", TestFreezeService.Pipeline, "Deploy finished");
        FreezeResult again = await Service.EndAsync("build-1", TestFreezeService.Pipeline, null);

        Assert.Equal(FreezeResultStatus.Updated, ended.Status);
        Assert.Equal(FreezeState.Ended, ended.Freeze!.GetState(Service.UtcNow));
        Assert.True(ended.Freeze.EndedEarly);
        Assert.Equal(FreezeResultStatus.AlreadyEnded, again.Status);
    }

    [Fact]
    public async Task End_of_a_scheduled_freeze_cancels_it()
    {
        await _test.ScheduleAsync(startsInMinutes: 10, correlationId: "build-1");

        FreezeResult result = await Service.EndAsync("build-1", TestFreezeService.Pipeline, null);

        Assert.Equal(FreezeState.Cancelled, result.Freeze!.GetState(Service.UtcNow));
    }

    [Fact]
    public async Task End_of_an_unknown_freeze_is_not_an_error()
    {
        FreezeResult result = await Service.EndAsync("does-not-exist", TestFreezeService.Pipeline, null);

        Assert.Equal(FreezeResultStatus.NothingToEnd, result.Status);
    }

    [Fact]
    public async Task Cancel_of_an_active_freeze_is_a_conflict()
    {
        await _test.ScheduleAsync(correlationId: "build-1");

        FreezeResult result = await Service.CancelAsync("build-1", TestFreezeService.Pipeline, null);

        Assert.Equal(FreezeResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Transitions_record_the_start_and_expiry_once()
    {
        Freeze freeze = await _test.ScheduleAsync(startsInMinutes: 5, durationMinutes: 10);

        _test.Clock.Advance(TimeSpan.FromMinutes(6));
        await Service.ProcessTransitionsAsync();
        await Service.ProcessTransitionsAsync();
        _test.Clock.Advance(TimeSpan.FromMinutes(10));
        await Service.ProcessTransitionsAsync();
        await Service.ProcessTransitionsAsync();

        Assert.Equal(
            [FreezeAuditAction.Scheduled, FreezeAuditAction.Started, FreezeAuditAction.Expired],
            Service.GetAudit(freeze.Id).Select(entry => entry.Action));
        Assert.Equal(FreezeState.Expired, freeze.GetState(Service.UtcNow));
    }

    [Fact]
    public async Task IsFrozen_only_while_active_and_not_disabled()
    {
        await _test.ScheduleAsync(startsInMinutes: 5);
        Assert.False(Service.IsFrozen);

        _test.Clock.Advance(TimeSpan.FromMinutes(5));
        Assert.True(Service.IsFrozen);

        _test.Options.Disabled = true;
        Assert.False(Service.IsFrozen);
    }

    [Fact]
    public async Task AllowWrites_scopes_nest_and_flow_into_async_code()
    {
        Assert.False(Service.WritesAllowedInScope);

        using (Service.AllowWrites("import"))
        {
            using (Service.AllowWrites("nested"))
            {
                await Task.Yield();
                Assert.True(Service.WritesAllowedInScope);
            }

            Assert.True(Service.WritesAllowedInScope);
        }

        Assert.False(Service.WritesAllowedInScope);
    }

    [Fact]
    public async Task Blocked_attempts_are_counted_and_flushed()
    {
        Freeze freeze = await _test.ScheduleAsync();

        Service.RecordBlockedAttempt(freeze.Id);
        Service.RecordBlockedAttempt(freeze.Id);
        Service.FlushBlockedAttempts();
        Service.FlushBlockedAttempts();

        Assert.Equal(2, freeze.BlockedAttempts);
    }
}
