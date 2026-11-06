# Repository Instructions

## Mission and sources of truth

Build DocMind AI as described in `PRODUCTION_VISION.md`. This file defines repository working conventions; `README.md` explains setup and the current API. Inspect the actual code before assuming a planned capability exists.

## Required context

Before product or architecture changes, read `PRODUCTION_VISION.md`, `.okf/README.md`, the relevant `.okf/agents/` playbook, and the affected source files. Use the matching workflow in `.okf/workflows/` and load a skill from `.codex/skills/` when its description matches the task. Keep all Markdown documentation in English.

## Project boundaries

- Backend: ASP.NET Core Web API, C#, .NET 10, kept in the single `backend/src/DocMind.Api` project. Do not split it into Clean Architecture projects.
- Frontend: React + TypeScript client in `frontend/`, built with Vite and Bun, consuming the API through its `/api` development proxy.
- Prefer cohesive feature folders and built-in .NET facilities. Add dependencies and abstractions only for a concrete requirement.
- Treat `POST /api/chat` as general LLM chat and `POST /api/knowledge/chat` as the document-grounded RAG endpoint.
- Keep document uploads and originals private, outside the web root; validate type, size, and content server-side. Do not claim public multi-user safety before authentication and workspace isolation exist.
- Keep citations aligned with retrieved passages. Do not fall back to uncited general model knowledge when retrieval finds no supporting evidence.
- Preserve unrelated user changes. Never run `git clean` or delete user files.

## Implementation conventions

- Keep HTTP binding and response mapping in controllers; put provider calls and feature behavior in services.
- Use typed request/response models, server-side validation, cancellation tokens, structured logging, and Problem Details errors.
- Read secrets through configuration providers. Use .NET User Secrets locally; do not commit credentials or log prompts, documents, or keys.
- Keep provider timeouts bounded and client-facing errors free of provider internals.
- Update README and `PRODUCTION_VISION.md` when implemented behavior or accepted product boundaries change.

## Verification

- Run `backend/scripts/verify.ps1` for backend code/configuration changes and report exact outcomes. It restores, builds, and tests the API through the test project's project reference.
- For frontend changes, run `bun run build` from `frontend/` when dependencies are available; report if it is blocked or skipped.
- CI runs the backend verification script and a frozen-lockfile Bun frontend build for pull requests and pushes to `main`.
- Keep tests deterministic; provider integration tests must use fake HTTP handlers rather than real API keys or external network calls.
- Do not claim a check passed unless it completed successfully.
- If network/dependency or environment restrictions prevent a check, report the command and blocker.
- Do not add broad tests or infrastructure unrelated to the requested feature.
