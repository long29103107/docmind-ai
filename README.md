# DocMind AI

DocMind AI is a private document knowledge assistant. The MVP lets a local user upload supported documents, ask questions grounded in their contents, and inspect citations. The repository contains one ASP.NET Core backend project and a React + TypeScript web client built with Vite and Bun.

## Requirements

- .NET SDK `10.0.400` or a later servicing patch in the same feature band (pinned in `backend/global.json`)
- Bun `1.3.13` (pinned in `frontend/package.json`)
- Docker Desktop with Docker Compose
- An OpenAI-compatible API key for chat and embeddings

## Run locally

Open two PowerShell terminals from the repository root. Start PostgreSQL/pgvector and the backend in the first terminal:

```powershell
docker compose up -d database
Set-Location .\backend
dotnet restore .\DocMind.slnx
dotnet user-secrets set "ConnectionStrings:KnowledgeBase" "Host=localhost;Port=5433;Database=docmind;Username=docmind;Password=docmind-local-only" --project .\src\DocMind.Api
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_API_KEY" --project .\src\DocMind.Api
dotnet run --project .\src\DocMind.Api --launch-profile http --urls http://localhost:5050
```

The API is available at `http://localhost:5050`. The Compose database uses host port `5433` so it can run beside a local PostgreSQL on `5432`. At startup, the API creates the pgvector extension, tables, and indexes if they do not already exist; PostgreSQL must be running and the configured connection credentials must work. In Development, Swagger UI is at `http://localhost:5050/swagger`, and the OpenAPI document is at `http://localhost:5050/swagger/v1/swagger.json`. Swagger is disabled outside Development. The Compose database credentials are for local development only. User Secrets stores the database connection string and model provider key outside the repository.

Start the frontend in the second terminal:

```powershell
Set-Location .\frontend
bun install --frozen-lockfile
bun run dev
```

Open the URL printed by Vite (normally `http://localhost:5173`). The development server proxies `/api` requests to `http://localhost:5050`. Set `DOCMIND_API_URL` in the frontend environment if the API uses a different URL. Provider credentials remain on the backend.

If Bun reports `CouldntReadCurrentDirectory` for `bun run dev`, start Vite directly with `bun .\node_modules\vite\bin\vite.js --host 127.0.0.1` from `frontend/`.

## Chat API

Ask the knowledge base with a streaming request (PowerShell):

```powershell
$body = @{ message = "What is retrieval-augmented generation?" } | ConvertTo-Json -Compress
curl.exe -N "http://localhost:5050/api/knowledge/chat" -H "Content-Type: application/json" -d $body
```

The endpoint responds with Server-Sent Events. `token` events contain incremental text; the final `complete` event contains the validated answer, model, and citations. An `error` event contains a safe client-facing message.

```text
event: token
data: {"text":"..."}

event: complete
data: {"answer":"... [S1]","model":"gpt-4o-mini","citations":[{"id":"S1","documentId":"...","fileName":"guide.pdf","pageNumber":4,"chunkIndex":2,"excerpt":"..."}]}
```

The endpoint returns an explicit insufficient-evidence answer when no relevant document passages are found. The document API provides `GET /api/documents`, `GET /api/documents/{id}`, `POST /api/documents` with multipart field `file`, and `DELETE /api/documents/{id}`. Supported uploads are PDF, DOCX, TXT, and Markdown, up to 20 MB. Scanned PDFs need OCR and cannot be indexed by this MVP. Provider credentials and response bodies are never logged. Configure `OpenAI:ApiKey` with .NET User Secrets locally and a managed secret store or protected environment variable elsewhere.

## Verification

Run the shared backend verification command from the repository root:

```powershell
.\backend\scripts\verify.ps1
```

The command runs `dotnet test` in Release configuration, which restores and builds the API/test projects before executing the suite. The API integration tests use a fake provider handler and do not require a real API key or outbound provider calls. GitHub Actions runs this command plus a frozen-lockfile Bun install and frontend production build for pull requests and pushes to `main`.

## Configuration

| Key | Default | Purpose |
| --- | --- | --- |
| `OpenAI:ApiKey` | Empty | Provider API key; required for chat requests |
| `OpenAI:BaseUrl` | `https://api.openai.com/v1/` | OpenAI-compatible API base URL |
| `OpenAI:Model` | `gpt-4o-mini` | Chat completion model |
| `OpenAI:MaxCompletionTokens` | `800` | Maximum completion token budget |
| `OpenAI:EmbeddingModel` | `text-embedding-3-small` | Embedding model |
| `OpenAI:EmbeddingDimensions` | `1536` | Vector dimensions; must match the configured embedding model |
| `ConnectionStrings:KnowledgeBase` | Development config | PostgreSQL connection string |
| `DocumentStorage:RootPath` | `backend/storage/documents` | Private directory for original uploads |
| `Rag:ChunkSizeCharacters` | `1200` | Maximum source chunk size |
| `Rag:ChunkOverlapCharacters` | `150` | Overlap between adjacent chunks |
| `Rag:MaxRetrievedChunks` | `5` | Context passages retrieved per question |
| `Rag:MinimumSimilarity` | `0.35` | Initial minimum cosine similarity; tune against real documents and questions |

Environment variables use double underscores, for example `OpenAI__ApiKey`.
Changing the embedding model or dimensions requires clearing the local database and re-indexing documents; vectors from different embedding spaces cannot be mixed.

## Project layout

The backend is intentionally a single ASP.NET Core project. The separate `frontend/` folder is a lightweight React client, not another .NET layer or Clean Architecture project.

```text
docker-compose.yaml
backend/
  src/DocMind.Api/
  tests/DocMind.Api.Tests/
  scripts/verify.ps1
  DocMind.slnx
  global.json
frontend/
  src/
  package.json
  bun.lock
```

The frontend connects document management and citation-backed knowledge chat. Chat history is kept only in the active browser session. This is a local/private MVP, not a public multi-user service; authentication, workspace isolation, OCR, background ingestion, cloud storage, and durable conversations remain future phases.

## Agent guidance

Read `AGENTS.md` and `PRODUCTION_VISION.md` before making product or architecture changes. Reuse the playbooks under `.okf/agents/` and keep updates small and verifiable.
