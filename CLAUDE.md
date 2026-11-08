# Project rules

This repository builds a **WinUI 3** desktop app (Windows App SDK, C# / .NET) with an
AI coding agent. Follow these rules in every session.

## Workflow

- **One Issue = one branch = one PR.** Keep each task small — about half a day of work.
- **Never commit directly to `main`.** All changes go through a Pull Request.
- **Never say "done" until it builds.** `dotnet build` and `dotnet test` must pass locally first.
- Leave PRs as **Draft** and let the human review and merge (squash) manually.

## WinUI

- **Use the WinUI Agent Plugin for WinUI 3 work.** For scaffolding, XAML/UI, build/run,
  UI tests, code review, packaging, or migration, delegate to the **`winui:winui-dev`
  subagent** (Task tool `subagent_type: "winui:winui-dev"`), or run the relevant `winui-*`
  skill. Do not hand-write WinUI from general knowledge — the plugin has the correct,
  current patterns and avoids the outdated WPF/UWP APIs general models default to.
- Target **WinUI 3 / Windows App SDK**. Do **not** use WPF, UWP, or WinForms APIs.
- Use `ThemeResource` for colors so the UI stays readable in Light and Dark themes.
- Once the app is scaffolded, build and run it with `.\BuildAndRun.ps1`.
- Prefer MVVM with `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`).

## Language

- Reply to the user, present plans, and write **PR titles/bodies and commit messages** in the
  **same language as the Issue** (for example, reply in Japanese when the Issue is in Japanese).
- Keep **code** — comments and identifiers — in **English**, regardless of the Issue language.

## Style

- Describe *what* should work; let the agent choose *how* unless a method truly matters.
