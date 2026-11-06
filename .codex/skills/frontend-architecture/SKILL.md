---
name: frontend-architecture
description: Implement or refactor the optional DocMind AI web client when frontend work is requested.
---

# Frontend Architecture

Use this skill only when a frontend exists or frontend work is explicitly requested. Read `PRODUCTION_VISION.md`, the frontend source and package manifest, and the API contract before changing structure.

## Rules

- Follow the existing frontend layout and conventions; do not add a router, state library, component framework, or CSS system without a concrete need.
- Keep client request/response types aligned with `src/DocMind.Api` API contracts.
- Place API calls and reusable UI at the narrowest appropriate shared or feature boundary.
- Treat frontend validation as usability only; the backend remains authoritative for validation, authentication, and authorization.
- Make loading, empty, success, and API-error states clear.
- Keep the UI optional and do not make backend correctness depend on it.

## Verification

Run the frontend's documented build command after implementation changes when dependencies are available, and report if it was skipped.
