---
name: okf-compactor
description: Consolidate redundant OKF notes while preserving task identity, status, scope, decisions, verification evidence, and next steps.
---

# OKF Compactor

Reduce duplicated project notes and scan cost without losing useful state.

## Workflow

1. Read the owning index or summary and inventory related files.
2. Identify the canonical destination before editing.
3. Preserve task identity, status, goals, scope, acceptance criteria, decisions, verification evidence, and next-step direction.
4. Update links and templates that refer to the old layout.
5. Remove a source file only after its unique information is represented elsewhere and the user explicitly asked for cleanup or compaction.
6. Read back the result and check for stale references and malformed Markdown.

## Guardrails

- Do not rewrite product meaning or change an accepted decision while compacting.
- Preserve evidence and label unverified claims as unverified.
- Do not create a new phase or future task as a side effect.
- Do not run application builds for documentation-only compaction.
