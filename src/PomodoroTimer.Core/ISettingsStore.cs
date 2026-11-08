namespace PomodoroTimer.Core;

/// <summary>Loads and saves <see cref="AppSettings"/>.</summary>
public interface ISettingsStore
{
    /// <summary>Returns the saved settings, or defaults when nothing valid is stored.</summary>
    AppSettings Load();

    void Save(AppSettings settings);
}
