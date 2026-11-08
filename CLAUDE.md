# Project rules

This repository builds a **WinUI 3** desktop app (Windows App SDK, C# / .NET) with an
AI coding agent. Follow these rules in every session.

## Workflow

- **One Issue = one branch = one PR.** Keep each task small — about half a day of work.
- **Never commit directly to `main`.** All changes go through a Pull Request.
- **Never say "done" until it builds.** `dotnet build` and `dotnet test` must pass locally first.
- Leave PRs as **Draft** and let the human review and merge (squash) manually.

## Microsoft.UI.Reactor

- **Use the Reactor Agent Plugin for Microsoft.UI.Reactor work.** For scaffolding,
  declarative C# UI, build/run, UI tests, code review, packaging, or migration,
  delegate to the `reactor:reactor-dev` subagent
  (Task tool `subagent_type: "reactor:reactor-dev"`), or use the relevant skills
  provided by the Reactor plugin. Do not hand-write Reactor code from general
  knowledge — follow the plugin's current APIs, patterns, and tooling.
- Target Microsoft.UI.Reactor on WinUI 3 / Windows App SDK.
  Do not use WPF, UWP, or WinForms APIs.
- Build the UI with Reactor's declarative C# components and state patterns.
  Do not introduce XAML, XAML data binding, or an MVVM/ViewModel layer for UI state.
  Keep reusable business logic and services separate from UI components.
- Use the theme-aware resources and styling patterns recommended by the Reactor
  plugin so the UI stays readable in Light and Dark themes. Avoid hard-coded colors.
- Follow the Reactor plugin's current scaffolding, build, run, and development-loop
  instructions. Use `.\BuildAndRun.ps1` only if it exists and supports the Reactor
  project; otherwise use the workflow generated or recommended by the plugin.
- When rebuilding an existing WinUI app, preserve its behavior and reuse compatible
  business logic, services, and data models. Recreate the UI using Reactor patterns
  rather than carrying over its XAML or ViewModel architecture.
- **Tooling:** The `mur` CLI is installed as a .NET global tool
  (`%USERPROFILE%\.dotnet\tools\mur.exe`) and is on PATH in both PowerShell and Bash.
  If `mur` is ever not found in a shell, verify with `Get-Command mur` (PowerShell) or
  `which mur` (Bash) before concluding it is missing, and follow the Reactor plugin's
  setup instructions to fix it. If `mur` remains unavailable, use `dotnet build` where
  supported, and report any scaffolding, run, or verification steps that could not be
  completed. Do not treat a successful build as proof that the app launches or works correctly.

## Language

- Reply to the user, present plans, and write **PR titles/bodies and commit messages** in the
  **same language as the Issue** (for example, reply in Japanese when the Issue is in Japanese).
- Keep **code** — comments and identifiers — in **English**, regardless of the Issue language.

## Style

- Describe *what* should work; let the agent choose *how* unless a method truly matters.