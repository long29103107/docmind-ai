# Repository Instructions

## Mission and sources of truth

Build DocMind AI as described in `PRODUCTION_VISION.md`. This file defines repository working conventions; `README.md` explains setup and the current API. Inspect the actual code before assuming a planned capability exists.

## Required context

Before product or architecture changes, read `PRODUCTION_VISION.md`, `.okf/README.md`, the relevant `.okf/agents/` playbook, and the affected source files. Use the matching workflow in `.okf/workflows/` and load a skill from `.codex/skills/` when its description matches the task. Keep all Markdown documentation in English.

## Project boundaries

- Backend: ASP.NET Core Web API, C#, .NET 10.
- Keep the application in one project (`src/DocMind.Api`). Do not split it into Clean Architecture projects.
- Prefer cohesive feature folders and built-in .NET facilities. Add dependencies and abstractions only for a concrete requirement.
- Treat the initial chat endpoint as general LLM chat, not document-grounded RAG.
- Add PostgreSQL/pgvector, document storage, authentication, and frontend capabilities only as explicit features with clear acceptance criteria.
- Preserve unrelated user changes. Never run `git clean` or delete user files.

## Implementation conventions

- Keep HTTP binding and response mapping in controllers; put provider calls and feature behavior in services.
- Use typed request/response models, server-side validation, cancellation tokens, structured logging, and Problem Details errors.
- Read secrets through configuration providers. Use .NET User Secrets locally; do not commit credentials or log prompts, documents, or keys.
- Keep provider timeouts bounded and client-facing errors free of provider internals.
- Update README and `PRODUCTION_VISION.md` when implemented behavior or accepted product boundaries change.

## Verification

- Run relevant build or checks for code/configuration changes and report exact outcomes.
- Do not claim a check passed unless it completed successfully.
- If network/dependency or environment restrictions prevent a check, report the command and blocker.
- Do not add broad tests or infrastructure unrelated to the requested feature.
