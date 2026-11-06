# DocMind AI — Product Vision and Roadmap

## 1. Product vision

DocMind AI is a private document knowledge assistant. It lets a person collect work documents in one knowledge base, ask questions in natural language, and receive concise answers grounded in those documents. Every factual answer should link to the passages that support it. When the knowledge base does not contain enough evidence, DocMind should say so instead of presenting a guess as a sourced answer.

The product is a web application backed by an ASP.NET Core API. The browser handles the document library and chat experience; the backend owns document processing, retrieval, model credentials, and access to persistent data.

## 2. Users and problem

### Primary user

An individual or small-team knowledge worker who has useful information spread across PDFs, Word documents, and text files and needs to find answers without rereading each file.

### User problem

Keyword search often misses meaning, while asking a general-purpose model can produce plausible answers that are not supported by the user's material. Users need answers that are both easy to ask for and easy to verify against a source document.

## 3. Product principles

- **Ground every answer:** use retrieved passages as the evidence for responses about uploaded content.
- **Show the evidence:** citations identify the source document and a useful location, with a passage preview.
- **Be honest about gaps:** abstain when retrieval does not find relevant evidence; do not silently fall back to general model knowledge.
- **Keep private data private:** document contents and prompts are sensitive. Do not expose them in logs, public endpoints, or browser-visible provider credentials.
- **Make capability visible:** distinguish connected, processing, failed, and ready documents; do not imply that unimplemented RAG features work.
- **Keep the system understandable:** retain one ASP.NET Core backend project, a separate React + TypeScript frontend, and feature-oriented folders rather than adding architecture layers without a concrete need.

## 4. MVP — first usable product

The first product milestone is a usable, single-user document RAG chat running locally or in a private development environment. It delivers the core promise end to end: add documents, index them, ask questions, and verify answers from citations.

### In scope

1. **One private knowledge base** for the local installation. User accounts, sharing, and multi-tenant workspaces are deferred. The MVP is for local/private use and must not be exposed to the public internet without authentication and abuse controls.
2. **Document library:** upload, list, inspect processing state, and delete documents.
3. **Initial file formats:** PDF, DOCX, TXT, and Markdown. Reject unsupported or oversized uploads with actionable errors. Do not run OCR in the MVP; scanned PDFs without extractable text must fail clearly.
4. **Document processing:** extract text, retain source location metadata where available, split text into bounded overlapping chunks, create embeddings, and store document metadata, chunks, and vectors.
5. **Grounded chat:** retrieve the best matching chunks for each question, send only bounded retrieved context to the configured chat model, and return an answer with citations and short source passages.
6. **Insufficient evidence:** when no chunk meets the configured relevance threshold, respond that the answer was not found in the knowledge base. Do not answer from model knowledge alone.
7. **Web client:** provide document upload/list/delete, an indexed/failed status, a chat view, loading/error/empty states, and expandable citations that identify the file and source location.
8. **Local persistence:** PostgreSQL with pgvector for metadata, chunks, and embeddings; local filesystem storage for uploaded originals outside the source tree. Provide a local database setup that a developer can start and stop.
9. **Provider configuration:** keep chat and embedding credentials on the backend. Use .NET User Secrets locally and protected environment configuration elsewhere. Support an OpenAI-compatible chat and embeddings API.

### MVP limits and assumptions

- Single-user, single knowledge base; no login or sharing.
- Synchronous processing is acceptable for the initial bounded file-size limit. The API must report a clear failure instead of leaving a document indefinitely in a processing state.
- Citations use page numbers for PDFs where extraction provides them. DOCX, TXT, and Markdown citations use a chunk/section location and source preview.
- Uploaded originals and extracted content stay on the local/private installation. They are not sent to a model except for the minimum retrieved passages needed to answer a question.
- The exact upload size, chunk size, retrieval count, and similarity threshold are configuration values with conservative defaults, not user-visible promises.

### MVP acceptance criteria

- A developer can start the database, backend, and frontend using documented commands and configure provider credentials without putting a secret in source control.
- A user can upload a supported document, see it become ready or failed, and delete it.
- A supported document's extracted content is chunked and embedded; a question can retrieve relevant chunks from the knowledge base.
- An answer based on retrieved content includes citations that open or display the cited source passage and its location.
- An unrelated question receives an explicit insufficient-evidence response and no fabricated citation.
- Invalid, unsupported, empty, and oversized uploads produce safe client-facing errors.
- Deleting a document removes its metadata, vectors, and stored original from the local installation.
- The chat and upload APIs honor cancellation, enforce configured limits, and never return provider secrets or internal exception details.
- The interface clearly communicates document processing, retrieval failures, and the private/local scope of this MVP.

### Out of scope for MVP

- Public multi-user hosting, account registration, organization roles, sharing, or tenant isolation.
- OCR for scanned documents; spreadsheets, images, audio, and video.
- Cloud object storage, third-party document connectors, and automated sync.
- Background queues, distributed workers, high availability, and large-scale ingestion.
- Conversation history persisted across browser sessions, team collaboration, and advanced admin dashboards.
- Fine-tuning, agentic actions, web search, or answers that silently mix retrieved material with uncited general knowledge.

## 5. User journeys

### Set up a knowledge base

1. The developer starts PostgreSQL, the API, and the web client and configures chat and embedding provider settings outside source control.
2. The user opens the local DocMind web client and sees an empty document library.
3. The user uploads a supported document and sees its processing state.
4. The user sees the document marked ready, or a clear failure explaining what needs attention.

### Ask a grounded question

1. The user asks a question in the chat view.
2. The API embeds the question and retrieves relevant passages from the knowledge base.
3. If useful evidence exists, the model answers using the retrieved passages and the UI shows citations.
4. The user opens a citation to inspect the source name, location, and passage preview.
5. If useful evidence does not exist, DocMind says the knowledge base does not contain enough information to answer.

### Remove a document

1. The user selects a document in the library and confirms deletion.
2. The API removes its original file, metadata, extracted chunks, and vectors.
3. Future retrieval cannot cite the deleted document.

## 6. Technical direction

- **Backend:** ASP.NET Core Web API on .NET 10 and C# in the single `backend/src/DocMind.Api` project.
- **Frontend:** React + TypeScript single-page application in `frontend/`, built with Vite and Bun.
- **Database and vector search:** PostgreSQL with the pgvector extension.
- **Document originals:** private local filesystem storage for the MVP, outside the public web root and source tree.
- **Text extraction:** format-specific extractors for PDF, DOCX, TXT, and Markdown, with size, text, and processing limits.
- **Embeddings and chat:** configurable OpenAI-compatible endpoints; secrets remain on the backend.
- **Local orchestration:** Docker Compose for PostgreSQL/pgvector; API and frontend run as developer processes initially.
- **API contract:** typed request/response models and Problem Details errors. Keep frontend types aligned with the actual API.

Use cohesive feature folders and built-in .NET facilities. Add a new deployable, persistence abstraction, or architecture layer only when a concrete requirement justifies it.

## 7. Phase plan

### Phase 0 — Product and local foundation

- Lock MVP scope and document accepted defaults.
- Add PostgreSQL/pgvector local setup, backend persistence configuration, and schema initialization/migration.
- Keep the existing chat API and the frontend build working while the application layout evolves.

**Exit:** a developer can start the local database and both applications from documented instructions; the backend can connect to the database without secrets committed to the repository.

**Current status:** Implemented in code; local database startup still needs an end-to-end smoke check.

### Phase 1 — MVP document ingestion

- Implement safe PDF, DOCX, TXT, and Markdown upload and extraction.
- Store originals and document metadata; report processing status and actionable failures.
- Chunk extracted text, create embeddings, persist vectors, list documents, and delete all related data safely.

**Exit:** supported files can be uploaded, indexed, inspected, and deleted; extracted text and vectors persist across API restarts.

**Current status:** Implemented in code; needs verification against a running pgvector instance and a configured embedding provider.

### Phase 2 — MVP grounded chat

- Embed each question and retrieve relevant chunks from the selected knowledge base.
- Add bounded context construction, a grounded-answer prompt, relevance threshold, insufficient-evidence behavior, and citation response models.
- Integrate chat history for the current browser session without claiming durable conversation storage.

**Exit:** relevant questions return source-backed answers; unsupported questions abstain; citations identify retrievable source passages.

**Current status:** Implemented in code; retrieval thresholds and citation quality need evaluation with representative documents.

### Phase 3 — MVP web experience and release gate

- Connect the React client to document management and RAG chat APIs.
- Implement upload progress/status, document deletion, citation inspection, and clear empty/loading/error states.
- Document local setup and limitations; add frontend build verification to CI.

**Exit:** the acceptance criteria in Section 4 pass end to end in a private local environment; public internet exposure remains disabled until the next phase.

**Current status:** Document library, grounded chat UI, Bun lockfile, and frontend CI build gate are implemented. End-to-end acceptance has not yet been verified.

### Phase 4 — Private deployment hardening

- Add authentication, authorization, and user/workspace isolation before shared or public deployment.
- Move originals to managed object storage where needed; add background ingestion, retry, and cleanup jobs.
- Add operational health, structured privacy-safe diagnostics, backups, retention controls, rate limits, and deployment automation.

**Exit:** documented threat and operational requirements are met for the chosen deployment environment.

### Phase 5 — Product expansion

- Evaluate OCR for scanned PDFs, more file types, and external document connectors.
- Add durable conversations, workspace sharing, richer retrieval controls, and user feedback/evaluation workflows when supported by product needs.

**Exit:** each expansion has its own acceptance criteria and evidence that it improves the core document-questioning journey.

## 8. Current implementation status

- Implemented in code: .NET 10 backend, PostgreSQL/pgvector setup, document upload/list/delete, PDF/DOCX/TXT/Markdown extraction, chunking and embeddings, vector retrieval, grounded chat with citations, and a React/Bun document library and chat UI.
- Also retained: `POST /api/chat` as a general LLM chat endpoint. The product UI uses `POST /api/knowledge/chat` for grounded RAG.
- Not implemented: authentication, multi-user isolation, OCR, asynchronous ingestion, cloud storage, durable conversations, and production deployment hardening.
- Local database and provider-backed end-to-end behavior still need a smoke check. A successful build alone does not verify RAG quality or privacy controls.

## 9. Security, privacy, and reliability

- Never commit API keys, tokens, or production credentials.
- Do not log API keys, full prompts, document text, retrieved passages, or provider response bodies.
- Validate file extension, declared content type, file signature where practical, size, and extracted text size; generate storage names server-side.
- Keep uploaded files outside the web root; prevent path traversal and do not trust client-provided paths or document identifiers.
- Apply cancellation and bounded timeouts to extraction, embedding, retrieval, and chat operations.
- Use private local-only defaults until authentication and authorization are implemented. Do not claim the MVP is safe for public exposure.
- Delete a document's file, metadata, chunks, and vectors as one recoverable operation; report partial cleanup failures for repair.
- Return safe Problem Details errors while retaining only non-sensitive operational context in server logs.

## 10. Definition of done

- The change implements an observable user outcome that matches the current phase.
- API validation, limits, cancellation, and safe failure behavior match the feature contract.
- The frontend reflects actual backend state and does not invent product capability.
- Secrets and document contents are not committed or logged.
- README, API contracts, and this roadmap reflect the implemented behavior and accepted boundaries.
- Relevant builds and checks are run and reported accurately; unrun checks are labeled clearly.

## 11. How to use this vision

Start with Phase 0 and then implement Phase 1 through Phase 3 in order. Keep each phase as a reviewable slice, update the status in Section 8 as behavior becomes real, and do not begin later-phase identity, deployment, or expansion work until its prerequisites and acceptance criteria are clear.

Before a multi-requirement feature, inspect the current code and write a concise feature spec from the `.okf/templates/spec.md` template. Update this file only when product scope, a phase boundary, or a durable architecture decision changes.

For local setup and commands, follow `README.md`. The API and frontend live under `backend/` and `frontend/` respectively.
