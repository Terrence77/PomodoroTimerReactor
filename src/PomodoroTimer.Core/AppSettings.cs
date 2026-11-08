using System.Text.Json.Serialization;

namespace PomodoroTimer.Core;

/// <summary>User-configurable settings. Durations are whole minutes.</summary>
public sealed record AppSettings(
    int WorkMinutes = AppSettings.DefaultWorkMinutes,
    int BreakMinutes = AppSettings.DefaultBreakMinutes,
    AppTheme Theme = AppTheme.Light)
{
    public const int DefaultWorkMinutes = 25;
    public const int DefaultBreakMinutes = 5;
    public const int MinMinutes = 1;
    public const int MaxMinutes = 120;

    public static AppSettings Default { get; } = new();

    [JsonIgnore]
    public TimeSpan WorkDuration => TimeSpan.FromMinutes(WorkMinutes);

    [JsonIgnore]
    public TimeSpan BreakDuration => TimeSpan.FromMinutes(BreakMinutes);

    /// <summary>Clamps a minute value into the supported range.</summary>
    public static int ClampMinutes(int minutes) => Math.Clamp(minutes, MinMinutes, MaxMinutes);

    /// <summary>Returns a copy with every value forced into its valid range.</summary>
    public AppSettings Normalize() => this with
    {
        WorkMinutes = ClampMinutes(WorkMinutes),
        BreakMinutes = ClampMinutes(BreakMinutes),
        Theme = Enum.IsDefined(Theme) ? Theme : AppTheme.Light,
    };
}
