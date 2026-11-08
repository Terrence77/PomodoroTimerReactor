# WinUI AI-Driven Development Template

A minimal starter for building a **WinUI 3** desktop app with **Claude Code**, using a
simple **Issue → PR** workflow.

> This template targets **Claude Code** (`CLAUDE.md` and `.claude/commands/`). The WinUI agent
> plugin itself also works with GitHub Copilot CLI and OpenAI Codex, but the `/kickoff` and
> `/wrapup` commands here are Claude Code slash commands — adapt them for other agents.

## What's inside

| File | Purpose |
|---|---|
| `CLAUDE.md` | Rules the agent follows every session |
| `.claude/commands/kickoff.md` | `/kickoff <issue>` — read the issue, branch, propose a plan |
| `.claude/commands/wrapup.md` | `/wrapup` — commit, push, open a Draft PR (`Closes #`) |
| `.github/ISSUE_TEMPLATE/feature.md` | A feature issue with acceptance criteria |
| `.github/ISSUE_TEMPLATE/bug.md` | A bug report with steps to reproduce and expected behavior |

No CI, auto-review, or release automation is included — this template is intentionally minimal.

## How to use

1. Create a new repository from this template (GitHub "Use this template", or
   `gh repo create <name> --template <owner>/winui-ai-dev-template --public --clone`).
2. Install the WinUI agent plugin for Claude Code (see Microsoft Learn: *AI-assisted Windows development*).
3. Open an Issue with the Feature template and write its **acceptance criteria** (what, not how).
4. Run `/kickoff <issue-number>`, review the plan, and let the agent implement.
5. Run `/wrapup`, then review the Draft PR, run the app once yourself, and **squash-merge**.

The loop is: **Issue → `/kickoff` → implement → `/wrapup` → review → squash merge**.
