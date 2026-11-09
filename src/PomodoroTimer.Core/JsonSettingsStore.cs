using System.Text.Json;
using System.Text.Json.Serialization;

namespace PomodoroTimer.Core;

/// <summary>Persists settings as JSON. Defaults to %LOCALAPPDATA%\PomodoroTimer\settings.json.</summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    public JsonSettingsStore(string? filePath = null)
    {
        FilePath = filePath ?? DefaultFilePath;
    }

    public static string DefaultFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PomodoroTimer",
        "settings.json");

    public string FilePath { get; }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return AppSettings.Default;
            }

            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings);
            return (settings ?? AppSettings.Default).Normalize();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return AppSettings.Default;
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings.Normalize(), SettingsJsonContext.Default.AppSettings);

        // Write to a temp file first so a crash mid-write cannot leave a truncated settings file.
        var tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, FilePath, overwrite: true);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext
{
}
