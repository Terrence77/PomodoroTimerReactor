using Microsoft.UI.Reactor.Core;
using PomodoroTimer.Core;

namespace PomodoroTimer;

/// <summary>Snapshot of the Pomodoro session plus the commands the Timer page can issue.</summary>
public sealed record TimerViewState(
    PomodoroPhase Phase,
    TimeSpan Remaining,
    bool IsRunning,
    bool IsIdle,
    TimeSpan NextPhaseDuration,
    Action Start,
    Action Pause,
    Action Reset);

/// <summary>Current settings plus a callback that validates, persists and applies a change.</summary>
public sealed record SettingsViewState(AppSettings Settings, Action<AppSettings> Update);

public static class PomodoroContexts
{
    public static readonly Context<TimerViewState> Timer = new(new TimerViewState(
        PomodoroPhase.Work,
        AppSettings.Default.WorkDuration,
        false,
        true,
        AppSettings.Default.BreakDuration,
        () => { },
        () => { },
        () => { }));

    public static readonly Context<SettingsViewState> Settings = new(
        new SettingsViewState(AppSettings.Default, _ => { }));
}

/// <summary>App-wide services that live outside the component tree.</summary>
public static class AppServices
{
    private static readonly ISettingsStore Store = new JsonSettingsStore();

    /// <summary>Settings read from disk once at startup (defaults when missing or corrupt).</summary>
    public static AppSettings InitialSettings { get; } = Store.Load();

    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            Store.Save(settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Keep the in-memory value; persistence will be retried on the next change.
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
}
