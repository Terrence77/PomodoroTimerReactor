using PomodoroTimer.Core;

namespace PomodoroTimer.Core.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public JsonSettingsStoreTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "PomodoroTimerTests", Guid.NewGuid().ToString("N"));
        _filePath = Path.Combine(_directory, "nested", "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var store = new JsonSettingsStore(_filePath);

        var settings = store.Load();

        Assert.Equal(new AppSettings(25, 5, AppTheme.Light), settings);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllValues()
    {
        var store = new JsonSettingsStore(_filePath);
        var saved = new AppSettings(WorkMinutes: 50, BreakMinutes: 10, Theme: AppTheme.Dark);

        store.Save(saved);
        var loaded = new JsonSettingsStore(_filePath).Load();

        Assert.True(File.Exists(_filePath));
        Assert.Equal(saved, loaded);
    }

    [Fact]
    public void Save_WritesThemeAsReadableString()
    {
        var store = new JsonSettingsStore(_filePath);

        store.Save(new AppSettings(Theme: AppTheme.Dark));

        var json = File.ReadAllText(_filePath);
        Assert.Contains("\"Dark\"", json);
        Assert.DoesNotContain("Duration", json); // computed properties are not persisted
    }

    [Theory]
    [InlineData("this is not json")]
    [InlineData("{ \"WorkMinutes\": \"abc\" }")]
    [InlineData("{ \"Theme\": \"Purple\" }")]
    [InlineData("")]
    [InlineData("null")]
    public void Load_CorruptFile_ReturnsDefaults(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, content);

        var settings = new JsonSettingsStore(_filePath).Load();

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public void Load_OutOfRangeMinutes_AreClamped()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, "{ \"WorkMinutes\": 500, \"BreakMinutes\": 0, \"Theme\": \"Dark\" }");

        var settings = new JsonSettingsStore(_filePath).Load();

        Assert.Equal(new AppSettings(120, 1, AppTheme.Dark), settings);
    }

    [Fact]
    public void Save_OutOfRangeMinutes_AreClampedOnDisk()
    {
        var store = new JsonSettingsStore(_filePath);

        store.Save(new AppSettings(WorkMinutes: -3, BreakMinutes: 999));

        Assert.Equal(new AppSettings(1, 120, AppTheme.Light), store.Load());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(25, 25)]
    [InlineData(120, 120)]
    [InlineData(121, 120)]
    [InlineData(-10, 1)]
    public void ClampMinutes_KeepsValueBetween1And120(int input, int expected)
    {
        Assert.Equal(expected, AppSettings.ClampMinutes(input));
    }

    [Fact]
    public void DefaultFilePath_IsUnderLocalAppDataPomodoroTimer()
    {
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PomodoroTimer",
            "settings.json");

        Assert.Equal(expected, JsonSettingsStore.DefaultFilePath);
    }
}
