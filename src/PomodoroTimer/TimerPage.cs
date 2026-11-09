using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using PomodoroTimer.Core;
using static Microsoft.UI.Reactor.Factories;

namespace PomodoroTimer;

/// <summary>Shows the current phase, the remaining time as mm:ss and the Start / Pause / Reset controls.</summary>
class TimerPage : Component
{
    public override Element Render()
    {
        var timer = UseContext(PomodoroContexts.Timer);

        var isWork = timer.Phase == PomodoroPhase.Work;
        var phaseLabel = isWork ? "Work" : "Break";
        var phaseHint = isWork ? "Stay focused" : "Take a short rest";
        var nextLabel = isWork ? "Break" : "Work";
        var nextMinutes = (int)Math.Round(timer.NextPhaseDuration.TotalMinutes);
        var status = timer.IsRunning ? "Running" : timer.IsIdle ? "Ready" : "Paused";
        var timeText = PomodoroSession.Format(timer.Remaining);

        var phaseBadge = Border(
                // No explicit Foreground: a themed Foreground replaces the typography style,
                // and the default text brush already follows Light/Dark.
                // The id goes on the text element so UI Automation reads the bare
                // "Work"/"Break" value (the Border's Name is "Current phase: ...").
                Subtitle(phaseLabel)
                    .HeadingLevel(AutomationHeadingLevel.Level1)
                    .AutomationId("PhaseBadge"))
            .Background(isWork ? Theme.SystemAttentionBackground : Theme.SystemSuccessBackground)
            .WithBorder(Theme.CardStroke, 1)
            .CornerRadius(4)
            .Padding(horizontal: 16, vertical: 4)
            .HAlign(HorizontalAlignment.Center)
            .AutomationName($"Current phase: {phaseLabel}")
            .LiveRegion(AutomationLiveSetting.Polite);

        var clock = Display(timeText)
            .HAlign(HorizontalAlignment.Center)
            .AutomationName($"Remaining time {timeText}")
            .AutomationId("TimerClock");

        var buttons = HStack(8,
            Button("Start", timer.Start).AccentButton()
                .IsEnabled(!timer.IsRunning)
                .MinWidth(96)
                .AutomationId("BtnStart"),
            Button("Pause", timer.Pause)
                .IsEnabled(timer.IsRunning)
                .MinWidth(96)
                .AutomationId("BtnPause"),
            Button("Reset", timer.Reset)
                .MinWidth(96)
                .AutomationId("BtnReset")
        ).HAlign(HorizontalAlignment.Center);

        return Card(
                VStack(16,
                    phaseBadge,
                    Caption(phaseHint).Foreground(Theme.SecondaryText).HAlign(HorizontalAlignment.Center),
                    clock,
                    Caption($"{status} · Next: {nextLabel} ({nextMinutes} min)")
                        .Foreground(Theme.SecondaryText)
                        .HAlign(HorizontalAlignment.Center)
                        .AutomationId("StatusText"),
                    buttons))
            .MinWidth(360)
            .HAlign(HorizontalAlignment.Center)
            .VAlign(VerticalAlignment.Center)
            .Margin(24);
    }
}
