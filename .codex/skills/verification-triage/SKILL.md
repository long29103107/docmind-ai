---
name: verification-triage
description: Classify .NET build and test failures before deciding whether to retry, fix code, or report an environment blocker.
---

# Verification Triage

Use the actual failing command and first meaningful error to choose the smallest trustworthy next action.

## Workflow

1. Read the exact command and relevant error output, not just the exit code.
2. Classify the failure as environment/dependency restore, permission, transient process/lock, warning, docs-only no-run, or likely code/configuration regression.
3. Choose one action: retry transient issues, patch only evidenced code/configuration regressions, or report an environment blocker with the exact command.
4. Do not claim a check passed unless it completed successfully.

## Project guidance

- Restore/build: use `dotnet restore DocMind.slnx` and `dotnet build DocMind.slnx --no-restore` when relevant.
- Tests: use `dotnet test DocMind.slnx` when a test project exists; report clearly when no tests are configured.
- For documentation-only changes, read back changed files instead of running an unrelated full build.
