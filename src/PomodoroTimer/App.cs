using System.Diagnostics;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PomodoroTimer;
using PomodoroTimer.Core;
using static Microsoft.UI.Reactor.Factories;

ReactorApp.Run<App>("Pomodoro Timer", width: 720, height: 600);

enum AppRoute { Timer, Settings }

/// <summary>
/// Root component. Owns the persisted settings, the single Pomodoro session and its
/// wall-clock ticker, so the countdown keeps running while the user is on Settings.
/// </summary>
class App : Component
{
    const string TimerTag = "timer";
    static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(200);

    static string RouteToTag(AppRoute route) => route switch
    {
        AppRoute.Settings => NavigationViewElement.SettingsTag,
        _ => TimerTag,
    };

    static AppRoute TagToRoute(string? tag) => tag switch
    {
        NavigationViewElement.SettingsTag => AppRoute.Settings,
        null => AppRoute.Settings,
        _ => AppRoute.Timer,
    };

    static Element RouteToPage(AppRoute route) => route switch
    {
        AppRoute.Settings => Component<SettingsPage>(),
        _ => Component<TimerPage>(),
    };

    public override Element Render()
    {
        var nav = UseNavigation(AppRoute.Timer);
        var (isPaneOpen, setIsPaneOpen) = UseState(true);
        var (settings, setSettings) = UseState(AppServices.InitialSettings);
        var sessionRef = UseRef<PomodoroSession?>(null);
        var lastTickRef = UseRef(0L);
        var (_, bumpVersion) = UseReducer(0);
        var window = UseWindow();

        sessionRef.Current ??= new PomodoroSession(settings.WorkDuration, settings.BreakDuration);
        var session = sessionRef.Current;
        var refresh = () => bumpVersion(v => v + 1);

        // Advance the session by the real time elapsed since the last tick (not by tick count),
        // so a late or skipped DispatcherTimer tick never makes the countdown drift.
        void AdvanceToNow()
        {
            var now = Stopwatch.GetTimestamp();
            session.Tick(Stopwatch.GetElapsedTime(lastTickRef.Current, now));
            lastTickRef.Current = now;
        }

        UseEffect(() =>
        {
            if (!session.IsRunning)
            {
                return () => { };
            }

            var timer = new DispatcherTimer { Interval = TickInterval };
            timer.Tick += (_, _) =>
            {
                AdvanceToNow();
                refresh();
            };
            timer.Start();
            return () => timer.Stop();
        }, session.IsRunning);

        var timerState = new TimerViewState(
            session.Phase,
            session.Remaining,
            session.IsRunning,
            session.IsIdle,
            session.DurationOf(session.Phase == PomodoroPhase.Work ? PomodoroPhase.Break : PomodoroPhase.Work),
            Start: () =>
            {
                if (session.IsRunning)
                {
                    return;
                }

                lastTickRef.Current = Stopwatch.GetTimestamp();
                session.Start();
                refresh();
            },
            Pause: () =>
            {
                if (!session.IsRunning)
                {
                    return;
                }

                AdvanceToNow();
                session.Pause();
                refresh();
            },
            Reset: () =>
            {
                session.Reset();
                refresh();
            });

        var settingsState = new SettingsViewState(settings, next =>
        {
            var normalized = next.Normalize();
            if (normalized == settings)
            {
                return;
            }

            // New durations apply immediately while the session is idle (never started, or Reset);
            // otherwise at the next phase switch or Reset. setSettings re-renders the root, which
            // rebuilds TimerViewState from the session so the Timer page shows the new time.
            session.UpdateDurations(normalized.WorkDuration, normalized.BreakDuration);
            AppServices.SaveSettings(normalized);
            setSettings(normalized);
        });

        var items = new[]
        {
            NavItem("Timer", icon: "Clock", tag: TimerTag),
        };

        var isDark = settings.Theme == AppTheme.Dark;
        var requestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

        // RequestedTheme does not reach the system caption buttons (min/max/close), so they
        // would follow the Windows app mode. Pin them to the app's chosen theme. (The referenced
        // Reactor package has no TitleBar.PreferredTheme modifier yet, so set it on AppWindow.)
        UseEffect(() =>
        {
            if (window?.AppWindow is { } appWindow)
            {
                appWindow.TitleBar.PreferredTheme = isDark ? TitleBarTheme.Dark : TitleBarTheme.Light;
            }

            return () => { };
        }, isDark, window);

        var titleBar = TitleBar("Pomodoro Timer")
            .WithNavigation(nav)
            .PaneToggleButtonVisible(true)
            .PaneToggleRequested(() => setIsPaneOpen(!isPaneOpen))
            .Tall()
            .Flex(shrink: 0);

        var navView = (NavigationView(items, NavigationHost(nav, RouteToPage)) with
        {
            SelectedTag = RouteToTag(nav.CurrentRoute),
            OnSelectedTagChanged = tag => nav.Navigate(TagToRoute(tag)),
            IsSettingsVisible = true,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsPaneToggleButtonVisible = false,
        })
        .IsPaneOpen(isPaneOpen, setIsPaneOpen)
        .Flex(grow: 1, basis: 0);

        return FlexColumn(titleBar, navView)
            .Background(Theme.SolidBackground)
            .RequestedTheme(requestedTheme)
            .Provide(PomodoroContexts.Timer, timerState)
            .Provide(PomodoroContexts.Settings, settingsState);
    }
}
