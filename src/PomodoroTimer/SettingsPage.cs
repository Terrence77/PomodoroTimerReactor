using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using PomodoroTimer.Core;
using static Microsoft.UI.Reactor.Factories;

namespace PomodoroTimer;

/// <summary>Work / break durations and theme. Every change is saved immediately.</summary>
class SettingsPage : Component
{
    static readonly string[] ThemeNames = ["Light", "Dark"];

    public override Element Render()
    {
        var ctx = UseContext(PomodoroContexts.Settings);
        var settings = ctx.Settings;

        void SetMinutes(double value, Func<int, AppSettings> apply)
        {
            // An empty NumberBox reports NaN; keep the previous value in that case.
            if (double.IsNaN(value))
            {
                return;
            }

            ctx.Update(apply(AppSettings.ClampMinutes((int)Math.Round(value))));
        }

        Element MinutesBox(string header, string description, int minutes, Func<int, AppSettings> apply) =>
            (NumberBox(minutes, v => SetMinutes(v, apply), header) with
            {
                Minimum = AppSettings.MinMinutes,
                Maximum = AppSettings.MaxMinutes,
                SmallChange = 1,
                LargeChange = 5,
                Description = description,
                ValidationMode = NumberBoxValidationMode.InvalidInputOverwritten,
            })
            .SpinButtons(NumberBoxSpinButtonPlacementMode.Inline)
            .AutomationName(header)
            .MinWidth(240);

        var rangeHint = $"{AppSettings.MinMinutes}–{AppSettings.MaxMinutes} minutes";

        return ScrollView(
            VStack(24,
                Title("Settings").HeadingLevel(AutomationHeadingLevel.Level1),
                Card(
                    VStack(16,
                        Subtitle("Durations").HeadingLevel(AutomationHeadingLevel.Level2),
                        MinutesBox("Work (minutes)", rangeHint, settings.WorkMinutes,
                            m => settings with { WorkMinutes = m }),
                        MinutesBox("Break (minutes)", rangeHint, settings.BreakMinutes,
                            m => settings with { BreakMinutes = m }),
                        Caption("Duration changes apply at the next phase or after Reset.")
                            .Foreground(Theme.SecondaryText))),
                Card(
                    VStack(16,
                        Subtitle("Appearance").HeadingLevel(AutomationHeadingLevel.Level2),
                        RadioButtons(ThemeNames, (int)settings.Theme,
                            i => ctx.Update(settings with { Theme = i == 1 ? AppTheme.Dark : AppTheme.Light }))
                        with { Header = "Theme" })),
                Caption("Settings are saved automatically.").Foreground(Theme.SecondaryText)
            ).Padding(horizontal: 24, vertical: 16).MaxWidth(640).HAlign(Microsoft.UI.Xaml.HorizontalAlignment.Left));
    }
}
