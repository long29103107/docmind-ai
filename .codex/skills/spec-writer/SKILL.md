---
name: spec-writer
description: Turn a DocMind AI feature request into a concise, testable specification grounded in the product vision and current code.
---

# Spec Writer

Create or edit specifications using `.okf/templates/spec.md`. Read `PRODUCTION_VISION.md`, `.okf/README.md`, and relevant code and documentation first.

## Workflow

1. State the user problem and desired outcome separately from implementation.
2. Map the work to a product goal or milestone; make scope expansion explicit.
3. Identify actors and describe the main flow.
4. Turn requirements into observable acceptance criteria. Include API behavior, validation, authorization, data handling, and errors where relevant.
5. Describe API and persistence changes only as far as needed for review and verification.
6. Separate confirmed decisions, assumptions, and open questions.
7. Set explicit in-scope and out-of-scope boundaries and verification.
8. Check for contradictions with the vision, README, source, and current API contracts.

## Guardrails

- Keep behavior that affects security or data integrity enforced by the server.
- Do not imply general LLM chat is document-grounded RAG; retrieval and citations must exist before claiming them.
- Do not invent product policy to resolve an open question.
- Keep small fixes in a task note; do not create phase machinery for every change.
- If a material product decision cannot be resolved from repository evidence, mark it open rather than silently deciding.
