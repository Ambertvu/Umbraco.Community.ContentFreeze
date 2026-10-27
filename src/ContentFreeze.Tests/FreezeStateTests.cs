using ContentFreeze.Models;

namespace ContentFreeze.Tests;

public class FreezeStateTests
{
    private static readonly DateTime StartsAt = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    private static Freeze CreateFreeze() => new() { StartsAt = StartsAt, EndsAt = StartsAt.AddMinutes(30) };

    [Theory]
    [InlineData(-1, FreezeState.Scheduled)]
    [InlineData(0, FreezeState.Active)]
    [InlineData(29, FreezeState.Active)]
    [InlineData(30, FreezeState.Expired)]
    public void State_follows_the_time_window(int minutesFromStart, FreezeState expected)
        => Assert.Equal(expected, CreateFreeze().GetState(StartsAt.AddMinutes(minutesFromStart)));

    [Fact]
    public void Ended_early_is_ended_even_before_the_end_time()
    {
        Freeze freeze = CreateFreeze();
        freeze.EndedAt = StartsAt.AddMinutes(10);
        freeze.EndedEarly = true;

        Assert.Equal(FreezeState.Ended, freeze.GetState(StartsAt.AddMinutes(11)));
    }

    [Fact]
    public void Cancelled_wins_over_the_time_window()
    {
        Freeze freeze = CreateFreeze();
        freeze.Cancelled = true;

        Assert.Equal(FreezeState.Cancelled, freeze.GetState(StartsAt.AddMinutes(5)));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(5, true)]
    [InlineData(30, false)]
    public void Only_scheduled_and_active_freezes_are_current(int minutesFromStart, bool expected)
        => Assert.Equal(expected, CreateFreeze().IsCurrent(StartsAt.AddMinutes(minutesFromStart)));
}
