# DocMind AI — Production Vision

## 1. Product vision

DocMind AI helps people build a searchable knowledge base from their documents and ask questions in natural language. Answers should be grounded in the user's material and point back to the relevant sources. The product should make document knowledge easier to find while being clear when the available evidence does not support an answer.

## 2. Product goals

- Let a user upload and manage documents in a private knowledge base.
- Extract, normalize, and index document content for semantic retrieval.
- Answer questions using retrieved passages and provide source citations.
- Keep the first implementation small enough to understand and extend.
- Protect document content, credentials, and user data throughout processing.

## 3. Initial scope

The first engineering milestone is a basic chat API that sends a user message to a configured LLM provider. This establishes the ASP.NET Core host, configuration, request validation, provider integration, and error handling before document retrieval is introduced.

The current starter includes `POST /api/chat`, an OpenAI-compatible Chat Completions client, configurable model/base URL, and secure local API key configuration through .NET User Secrets. It does not yet perform RAG and must not imply that answers are based on uploaded documents.

## 4. Target users and core journeys

### Initial chat journey

1. A developer configures an LLM API key outside source control.
2. A client submits a non-empty message to the chat endpoint.
3. The API validates the request and sends it to the configured provider.
4. The API returns the answer and model name, or a safe Problem Details error.

### Future document question journey

1. A user creates or selects a knowledge base.
2. The user uploads a supported document and sees its processing status.
3. The system extracts text, divides it into searchable chunks, and creates embeddings.
4. The user asks a question; the system retrieves relevant chunks and generates a grounded answer with citations.
5. The user can inspect cited source passages and manage or remove documents.

## 5. Product principles

- **Grounded answers:** Once RAG is available, distinguish retrieved evidence from general model knowledge and say when evidence is insufficient.
- **Traceable sources:** Citations should resolve to a document and a meaningful location such as a page or section.
- **Private by default:** Documents and prompts are sensitive user data. Minimize collection, access, logging, and retention.
- **Honest capability:** The initial chat API is general LLM chat; document-grounded answers arrive only with retrieval.
- **Useful errors:** Report actionable client-safe errors without exposing provider secrets, internal exceptions, or sensitive request content.
- **Simple architecture:** Keep the application in one ASP.NET Core project. Use cohesive feature folders and add abstractions only when they address a real need.

## 6. Technical direction

- **Backend:** ASP.NET Core Web API on .NET 10, C#.
- **Application shape:** One deployable project (`DocMind.Api`) with feature-oriented folders.
- **Initial model provider:** OpenAI-compatible Chat Completions API through `IHttpClientFactory`.
- **Future data store:** PostgreSQL for application data and pgvector for embeddings, subject to deployment needs.
- **Future document pipeline:** Upload validation, safe storage, format-specific extraction, chunking, embedding, indexing, and observable processing status.
- **Future client:** A separate web client may be added when a usable end-user interface is in scope.

The current starter deliberately has no database, file storage, authentication, or vector search. These are not silently simulated by the chat endpoint.

## 7. API direction

### Implemented

`POST /api/chat`

Request:

```json
{ "message": "What is RAG?" }
```

Response:

```json
{ "answer": "...", "model": "gpt-4o-mini" }
```

The request message must contain 1–8000 characters. Errors use Problem Details. The endpoint currently has no authentication and is intended as a local development starter; deployments must add authentication, authorization, abuse controls, and operational safeguards before public exposure.

### Future API capabilities

- Knowledge base and document management.
- Document processing status and retry operations.
- Conversation and message history.
- Retrieval-grounded chat responses with citations.

Define detailed contracts when each feature is implemented.

## 8. Security and reliability requirements

- Never commit API keys, tokens, or production credentials.
- Use .NET User Secrets for local development and a managed secret store or protected environment configuration in deployment.
- Do not log API keys, document content, full prompts, or provider response bodies.
- Apply input limits, upload size/type validation, rate limits, authentication, authorization, and tenant isolation as the corresponding features are introduced.
- Use cancellation tokens and bounded provider timeouts.
- Return sanitized Problem Details and preserve diagnostic context in structured server logs without sensitive payloads.
- Do not claim public production readiness until authentication, authorization, privacy, abuse controls, monitoring, and deployment practices are in place.

## 9. Milestone outline

1. **Chat API foundation:** ASP.NET Core host, provider configuration, chat request/response, validation, safe errors, and developer setup.
2. **Document ingestion:** supported formats, storage, extraction, processing status, and content limits.
3. **Chunking and embeddings:** consistent chunk metadata and embedding generation.
4. **Vector retrieval:** PostgreSQL/pgvector schema, similarity search, and relevance controls.
5. **Grounded answers:** retrieval prompt, citations, insufficient-evidence behavior, and traceability.
6. **User experience and operations:** knowledge base UI, identity and access controls, observability, deployment, and lifecycle management.

This outline is directional. Implement a milestone only when requested and after refining its acceptance criteria.

## 10. Definition of done

- The change fulfills a documented user outcome and stays within the current milestone.
- Input validation and safe failure behavior are covered for the changed feature.
- Secrets and sensitive content are not committed or written to logs.
- README and API documentation match actual behavior.
- Relevant build or checks are run and their results reported accurately.
- The feature does not imply capabilities that have not been implemented.

## 11. How to use this file

Before implementation, read this vision and `AGENTS.md`. Identify the milestone and user outcome, inspect the existing code, and state the acceptance criteria. Keep changes in the single `DocMind.Api` project unless a concrete user need justifies an additional deployable. Update this document when an accepted product boundary or durable architecture decision changes.

To run the current starter, follow the setup and chat API instructions in `README.md`. The first endpoint requires an OpenAI API key configured via User Secrets or a protected environment variable.
