namespace PomodoroTimer.Core;

/// <summary>
/// Pure Pomodoro state machine. Time only advances through <see cref="Tick"/>, so the
/// caller decides where elapsed time comes from (wall clock in the app, fixed values in tests).
/// Duration changes are deferred: they apply at the next phase switch or <see cref="Reset"/>,
/// never to the countdown that is currently running.
/// </summary>
public sealed class PomodoroSession
{
    public PomodoroSession(TimeSpan workDuration, TimeSpan breakDuration)
    {
        ValidateDuration(workDuration, nameof(workDuration));
        ValidateDuration(breakDuration, nameof(breakDuration));
        WorkDuration = workDuration;
        BreakDuration = breakDuration;
        Phase = PomodoroPhase.Work;
        Remaining = workDuration;
    }

    public PomodoroPhase Phase { get; private set; }

    public TimeSpan Remaining { get; private set; }

    public bool IsRunning { get; private set; }

    public TimeSpan WorkDuration { get; private set; }

    public TimeSpan BreakDuration { get; private set; }

    public void Start() => IsRunning = true;

    public void Pause() => IsRunning = false;

    /// <summary>Stops the timer and returns to the start of a Work phase using the latest durations.</summary>
    public void Reset()
    {
        IsRunning = false;
        Phase = PomodoroPhase.Work;
        Remaining = WorkDuration;
    }

    /// <summary>
    /// Stores new durations. They take effect at the next phase switch or Reset;
    /// the countdown in progress is left untouched.
    /// </summary>
    public void UpdateDurations(TimeSpan workDuration, TimeSpan breakDuration)
    {
        ValidateDuration(workDuration, nameof(workDuration));
        ValidateDuration(breakDuration, nameof(breakDuration));
        WorkDuration = workDuration;
        BreakDuration = breakDuration;
    }

    /// <summary>
    /// Advances the countdown by <paramref name="elapsed"/> when running. When the countdown
    /// reaches zero the session switches phase and carries any overflow into the next phase,
    /// so no time is lost or added at phase boundaries. Returns true if the phase changed.
    /// </summary>
    public bool Tick(TimeSpan elapsed)
    {
        if (!IsRunning || elapsed <= TimeSpan.Zero)
        {
            return false;
        }

        var startPhase = Phase;
        var remaining = Remaining - elapsed;
        while (remaining <= TimeSpan.Zero)
        {
            Phase = Phase == PomodoroPhase.Work ? PomodoroPhase.Break : PomodoroPhase.Work;
            remaining += DurationOf(Phase);
        }

        Remaining = remaining;
        return Phase != startPhase;
    }

    public TimeSpan DurationOf(PomodoroPhase phase) =>
        phase == PomodoroPhase.Work ? WorkDuration : BreakDuration;

    /// <summary>
    /// Formats a remaining time as mm:ss. Partial seconds round up, so the display reads
    /// 25:00 right after Start and only shows 00:00 at the boundary itself.
    /// </summary>
    public static string Format(TimeSpan remaining)
    {
        var totalSeconds = (long)Math.Ceiling(Math.Max(0, remaining.TotalSeconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private static void ValidateDuration(TimeSpan duration, string name)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(name, duration, "Duration must be positive.");
        }
    }
}
