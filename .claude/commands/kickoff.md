---
description: Read a GitHub Issue, create a feature branch, and propose a plan.
argument-hint: [issue-number]
---

Start work on GitHub Issue #$1.

1. Run `gh issue view $1` and read the issue body and its acceptance criteria.
2. Summarize what "done" means for this issue in one or two lines.
3. Create a branch off `main` named `feature/$1-<short-slug>`.
4. Propose a short implementation plan.

Then **stop and wait for my review**. Do not write any code until I approve the plan.
