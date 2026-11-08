---
description: Commit, push, and open a Draft PR that closes the issue.
---

Wrap up the current task.

1. Confirm `dotnet build` (and `dotnet test`, if tests exist) pass.
2. Review the diff and commit it in meaningful units.
3. Push the current branch.
4. Read the issue number from the branch name (e.g. `feature/12-...` → `12`).
5. Open a **Draft** PR with `gh pr create --draft` and include `Closes #<number>` in the body.
   Write the PR title and body, and the commit messages, in the **same language as the Issue**
   (do not default to English).

Do **not** merge. Leave the PR as a Draft for me to review and squash-merge manually.
