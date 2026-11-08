using PomodoroTimer.Core;

namespace PomodoroTimer.Core.Tests;

public class PomodoroSessionTests
{
    private static readonly TimeSpan Work = TimeSpan.FromMinutes(25);
    private static readonly TimeSpan Break = TimeSpan.FromMinutes(5);

    private static PomodoroSession NewSession() => new(Work, Break);

    [Fact]
    public void NewSession_StartsInWorkPhase_WithFullWorkDuration_NotRunning()
    {
        var session = NewSession();

        Assert.Equal(PomodoroPhase.Work, session.Phase);
        Assert.Equal(Work, session.Remaining);
        Assert.False(session.IsRunning);
    }

    [Fact]
    public void Tick_WhenNotStarted_DoesNotChangeRemaining()
    {
        var session = NewSession();

        session.Tick(TimeSpan.FromSeconds(10));

        Assert.Equal(Work, session.Remaining);
    }

    [Fact]
    public void Tick_WhenRunning_DecreasesRemaining()
    {
        var session = NewSession();
        session.Start();

        session.Tick(TimeSpan.FromSeconds(90));

        Assert.True(session.IsRunning);
        Assert.Equal(Work - TimeSpan.FromSeconds(90), session.Remaining);
    }

    [Fact]
    public void Pause_StopsCountdown_AndStartResumesFromSameRemaining()
    {
        var session = NewSession();
        session.Start();
        session.Tick(TimeSpan.FromSeconds(30));

        session.Pause();
        session.Tick(TimeSpan.FromMinutes(5));

        Assert.False(session.IsRunning);
        Assert.Equal(Work - TimeSpan.FromSeconds(30), session.Remaining);

        session.Start();
        session.Tick(TimeSpan.FromSeconds(10));

        Assert.Equal(Work - TimeSpan.FromSeconds(40), session.Remaining);
    }

    [Fact]
    public void Reset_ReturnsToWorkPhase_FullDuration_AndStops()
    {
        var session = NewSession();
        session.Start();
        session.Tick(Work + TimeSpan.FromMinutes(1)); // now in Break

        session.Reset();

        Assert.Equal(PomodoroPhase.Work, session.Phase);
        Assert.Equal(Work, session.Remaining);
        Assert.False(session.IsRunning);
    }

    [Fact]
    public void Tick_ReachingExactlyZero_SwitchesToBreak_WithFullBreakDuration_AndKeepsRunning()
    {
        var session = NewSession();
        session.Start();

        var changed = session.Tick(Work);

        Assert.True(changed);
        Assert.Equal(PomodoroPhase.Break, session.Phase);
        Assert.Equal(Break, session.Remaining);
        Assert.True(session.IsRunning);
    }

    [Fact]
    public void Tick_CrossingBoundary_CarriesOverflowIntoNextPhase()
    {
        var session = NewSession();
        session.Start();
        session.Tick(Work - TimeSpan.FromSeconds(2));

        var changed = session.Tick(TimeSpan.FromSeconds(5));

        Assert.True(changed);
        Assert.Equal(PomodoroPhase.Break, session.Phase);
        Assert.Equal(Break - TimeSpan.FromSeconds(3), session.Remaining);
    }

    [Fact]
    public void Tick_BreakEnding_SwitchesBackToWork()
    {
        var session = NewSession();
        session.Start();
        session.Tick(Work);

        session.Tick(Break + TimeSpan.FromSeconds(1));

        Assert.Equal(PomodoroPhase.Work, session.Phase);
        Assert.Equal(Work - TimeSpan.FromSeconds(1), session.Remaining);
    }

    [Fact]
    public void Tick_SpanningMultiplePhases_HandlesEveryBoundary()
    {
        var session = NewSession();
        session.Start();

        session.Tick(Work + Break + TimeSpan.FromMinutes(1));

        Assert.Equal(PomodoroPhase.Work, session.Phase);
        Assert.Equal(Work - TimeSpan.FromMinutes(1), session.Remaining);
    }

    [Fact]
    public void Tick_WithinPhase_ReturnsFalse()
    {
        var session = NewSession();
        session.Start();

        Assert.False(session.Tick(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void UpdateDurations_DoesNotAffectCurrentCountdown_ButAppliesToNextPhase()
    {
        var session = NewSession();
        session.Start();
        session.Tick(TimeSpan.FromMinutes(1));

        session.UpdateDurations(TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(10));

        Assert.Equal(TimeSpan.FromMinutes(24), session.Remaining);

        session.Tick(TimeSpan.FromMinutes(24));

        Assert.Equal(PomodoroPhase.Break, session.Phase);
        Assert.Equal(TimeSpan.FromMinutes(10), session.Remaining);
    }

    [Fact]
    public void UpdateDurations_AppliesOnReset()
    {
        var session = NewSession();

        session.UpdateDurations(TimeSpan.FromMinutes(40), Break);
        Assert.Equal(Work, session.Remaining);

        session.Reset();
        Assert.Equal(TimeSpan.FromMinutes(40), session.Remaining);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveDurations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PomodoroSession(TimeSpan.Zero, Break));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PomodoroSession(Work, TimeSpan.FromSeconds(-1)));
    }

    [Theory]
    [InlineData(1500.0, "25:00")]
    [InlineData(1499.2, "25:00")]
    [InlineData(1499.0, "24:59")]
    [InlineData(65.0, "01:05")]
    [InlineData(0.0, "00:00")]
    [InlineData(7200.0, "120:00")]
    public void Format_ProducesMinutesAndSeconds(double seconds, string expected)
    {
        Assert.Equal(expected, PomodoroSession.Format(TimeSpan.FromSeconds(seconds)));
    }
}
