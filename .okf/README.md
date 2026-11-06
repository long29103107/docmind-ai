# Project Working Guide

This directory contains reusable agent playbooks and lightweight planning conventions adapted from another repository. Its original booking-domain rules were intentionally excluded. `PRODUCTION_VISION.md` and the code in `backend/src/DocMind.Api/` define this product's scope and current behavior.

## Working approach

1. Read `AGENTS.md`, `PRODUCTION_VISION.md`, this guide, and the affected code.
2. State the user outcome, in-scope work, acceptance criteria, and relevant risks.
3. Implement one cohesive feature in the existing ASP.NET Core project.
4. Update API docs and product documentation when behavior changes.
5. Run relevant verification and record exact results.

## Playbooks

- `agents/leader.md`: scope and decision alignment.
- `agents/planner.md`: turn requests into bounded acceptance criteria.
- `agents/developer.md`: implementation conventions.
- `agents/qa.md`: verification guidance.
- `agents/reviewer.md`: focused change review.

## Skills and workflows

- Reusable Codex skills are stored under `.codex/skills/`.
- Use `workflows/feature.md`, `workflows/bugfix.md`, or `workflows/refactor.md` as the task type requires.
- Use `.okf/templates/spec.md` for features and `.okf/templates/task.md` for bounded work when written acceptance criteria are useful.
- Use `.okf/templates/decision.md` only for durable accepted decisions. A phase summary is optional and should be created only when the work is organized into a real multi-task phase.

Keep feature specs concise. Do not create phase machinery for small changes. Record durable product or architecture decisions in `PRODUCTION_VISION.md` when they are accepted.
