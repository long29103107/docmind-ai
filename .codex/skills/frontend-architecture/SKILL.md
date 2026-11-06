---
name: frontend-architecture
description: Implement or refactor the optional DocMind AI web client when frontend work is requested.
---

# Frontend Architecture

Use this skill for work in `frontend/` or when frontend work is explicitly requested. The client is React + TypeScript built with Vite. Read `PRODUCTION_VISION.md`, the frontend source and `package.json`, and the API contract before changing structure.

## Rules

- Follow the existing frontend layout and conventions; do not add a router, state library, component framework, or CSS system without a concrete need.
- Keep client request/response types aligned with `backend/src/DocMind.Api` API contracts.
- Place API calls and reusable UI at the narrowest appropriate shared or feature boundary.
- Treat frontend validation as usability only; the backend remains authoritative for validation, authentication, and authorization.
- Make loading, empty, success, and API-error states clear.
- Keep the UI optional and do not make backend correctness depend on it.
- Use the Vite `/api` development proxy to call the ASP.NET Core API; never put provider credentials in client configuration.
- Use `POST /api/knowledge/chat` for the RAG UI and render citations only from returned source metadata. `POST /api/chat` is the separate general-chat endpoint.
- Send only the current browser-session history supported by the API; do not imply durable conversation storage.

## Verification

Run `bun install --frozen-lockfile` and `bun run build` from `frontend/` after implementation changes when dependencies are available, and report if either was skipped.
