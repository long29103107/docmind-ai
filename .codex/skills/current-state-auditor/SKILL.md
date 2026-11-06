---
name: current-state-auditor
description: Report the current state of DocMind AI implementation, documentation, verification, or task tracking without making changes.
---

# Current State Auditor

Report from the current repository state. Do not implement or update docs unless asked.

## Workflow

1. Inspect `git status --short` when worktree state matters.
2. Read `PRODUCTION_VISION.md`, `README.md`, `AGENTS.md`, and `.okf/README.md` for direction and workflow.
3. If `.okf/phase/` exists, inspect the relevant summary and task index; do not invent phase state when the folder is absent.
4. Inspect relevant source and executed verification output before calling behavior complete.
5. Distinguish implemented code from planned capabilities and executed checks.
6. Report missing or contradictory state rather than inferring completion from a plan.

## Output

Keep the report concise, link relevant repository paths, and state whether each check was run or only inspected.
