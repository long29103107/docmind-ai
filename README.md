# DocMind AI

DocMind AI is a private document knowledge assistant. The MVP lets a local user upload supported documents, ask questions grounded in their contents, and inspect citations. The repository contains one ASP.NET Core backend project and a React + TypeScript web client built with Vite and Bun.

## Requirements

- .NET SDK `10.0.400` or a later servicing patch in the same feature band (pinned in `backend/global.json`)
- Bun `1.3.13` (pinned in `frontend/package.json`)
- Docker Desktop with Docker Compose
- An OpenAI-compatible API key for chat and embeddings

## Run locally

Start PostgreSQL/pgvector and the backend from `backend/` so the pinned SDK is selected:

```powershell
cd .\backend
docker compose up -d database
dotnet restore .\DocMind.slnx
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_API_KEY" --project .\src\DocMind.Api
dotnet run --project .\src\DocMind.Api --urls http://localhost:5050
```

The database uses a development-only local credential from `appsettings.Development.json`. Do not reuse it outside a local machine. User Secrets stores the model provider key outside the repository.

From the repository root, in a second terminal, start the frontend:

```powershell
cd .\frontend
bun install --frozen-lockfile
bun run dev
```

Open the local URL printed by Vite. The development server proxies `/api` requests to `http://localhost:5050`. Set `DOCMIND_API_URL` in the frontend environment if the API uses a different URL. Provider credentials remain on the backend.

## Chat API

Ask the knowledge base:

```powershell
$body = @{ message = "What is retrieval-augmented generation?" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5050/api/knowledge/chat" -Method Post -ContentType "application/json" -Body $body
```

Successful response:

```json
{
  "answer": "... [S1]",
  "model": "gpt-4o-mini",
  "citations": [
    { "id": "S1", "documentId": "...", "fileName": "guide.pdf", "pageNumber": 4, "chunkIndex": 2, "excerpt": "..." }
  ]
}
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
backend/
  compose.yaml
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
