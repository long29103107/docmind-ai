# DocMind AI

DocMind AI is an AI-powered document knowledge base. This repository currently contains a minimal ASP.NET Core Web API starter with an OpenAI-compatible chat endpoint. Document ingestion, retrieval, and persistent chat are future milestones.

## Requirements

- .NET SDK `10.0.400` or a later servicing patch in the same feature band (pinned in `global.json`)
- An OpenAI API key to use the chat endpoint

## Run locally

From the repository root:

```powershell
dotnet restore .\DocMind.slnx
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_API_KEY" --project .\src\DocMind.Api
dotnet run --project .\src\DocMind.Api --urls http://localhost:5050
```

User Secrets is already initialized in this project. The key value is stored outside the repository.

## Chat API

Send a message:

```powershell
$body = @{ message = "What is retrieval-augmented generation?" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5050/api/chat" -Method Post -ContentType "application/json" -Body $body
```

Successful response:

```json
{
  "answer": "...",
  "model": "gpt-4o-mini"
}
```

The endpoint returns Problem Details for invalid input, missing configuration, provider failures, and timeouts. Provider credentials and response bodies are never logged. The API key is read from `OpenAI:ApiKey`; use .NET User Secrets locally and a managed secret store or environment variable in deployed environments.

## Verification

Run the shared verification script from the repository root:

```powershell
.\scripts\verify.ps1
```

The script restores the solution, builds in Release configuration, and runs the test suite. To use Debug configuration, pass `-Configuration Debug`. The API integration tests use a fake provider handler and do not require a real API key or outbound provider calls. GitHub Actions runs the same script for pull requests and pushes to `main`.

## Configuration

| Key | Default | Purpose |
| --- | --- | --- |
| `OpenAI:ApiKey` | Empty | Provider API key; required for chat requests |
| `OpenAI:BaseUrl` | `https://api.openai.com/v1/` | OpenAI-compatible API base URL |
| `OpenAI:Model` | `gpt-4o-mini` | Chat completion model |
| `OpenAI:MaxCompletionTokens` | `800` | Maximum completion token budget |

Environment variables use double underscores, for example `OpenAI__ApiKey`.

## Project layout

The application is intentionally a single ASP.NET Core project. Feature folders keep the HTTP contract, models, configuration, and service code easy to navigate without introducing Clean Architecture projects.

```text
src/DocMind.Api/
  Configuration/
  Controllers/
  Models/
  Services/
  Program.cs
```

Planned directions include document upload and text extraction, chunking and embeddings, PostgreSQL with pgvector, retrieval with source citations, and a frontend client. Add these as cohesive features when they are in scope.

## Agent guidance

Read `AGENTS.md` and `PRODUCTION_VISION.md` before making product or architecture changes. Reuse the playbooks under `.okf/agents/` and keep updates small and verifiable.
