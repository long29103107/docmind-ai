using Npgsql;
using DocMind.Api.Configuration;
using Microsoft.Extensions.Options;

namespace DocMind.Api.Data;

public sealed class DatabaseInitializer(
    NpgsqlDataSource dataSource,
    IOptions<OpenAiOptions> openAiOptions,
    ILogger<DatabaseInitializer> logger)
{
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private bool _initialized;

    private const string Schema = """
        CREATE EXTENSION IF NOT EXISTS vector;

        CREATE TABLE IF NOT EXISTS documents (
            id UUID PRIMARY KEY,
            file_name TEXT NOT NULL,
            storage_name TEXT NOT NULL UNIQUE,
            media_type TEXT NOT NULL,
            file_size_bytes BIGINT NOT NULL,
            status TEXT NOT NULL CHECK (status IN ('processing', 'ready', 'failed', 'deleting')),
            failure_message TEXT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            indexed_at TIMESTAMPTZ NULL
        );

        CREATE TABLE IF NOT EXISTS document_chunks (
            id UUID PRIMARY KEY,
            document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
            chunk_index INTEGER NOT NULL,
            page_number INTEGER NULL,
            content TEXT NOT NULL,
            embedding VECTOR NOT NULL,
            UNIQUE (document_id, chunk_index)
        );

        CREATE INDEX IF NOT EXISTS ix_document_chunks_embedding
            ON document_chunks USING hnsw (embedding vector_cosine_ops);
        CREATE INDEX IF NOT EXISTS ix_documents_status ON documents(status);

        CREATE TABLE IF NOT EXISTS application_settings (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );
        """;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationGate.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(Schema, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await connection.ReloadTypesAsync(cancellationToken);
            await ValidateEmbeddingSettingsAsync(connection, cancellationToken);
            _initialized = true;
            logger.LogInformation("DocMind database schema is ready.");
        }
        catch (Exception exception)
        {
            logger.LogCritical("Could not initialize the DocMind database ({ExceptionType}).", exception.GetType().Name);
            throw;
        }
        finally
        {
            _initializationGate.Release();
        }
    }

    private async Task ValidateEmbeddingSettingsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var settings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["embedding_model"] = openAiOptions.Value.EmbeddingModel,
            ["embedding_dimensions"] = openAiOptions.Value.EmbeddingDimensions.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        foreach (var (key, value) in settings)
        {
            await using (var insert = new NpgsqlCommand(
                "INSERT INTO application_settings (key, value) VALUES (@key, @value) ON CONFLICT (key) DO NOTHING",
                connection))
            {
                insert.Parameters.AddWithValue("key", key);
                insert.Parameters.AddWithValue("value", value);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var select = new NpgsqlCommand("SELECT value FROM application_settings WHERE key = @key", connection);
            select.Parameters.AddWithValue("key", key);
            var storedValue = (string?)await select.ExecuteScalarAsync(cancellationToken);
            if (!string.Equals(storedValue, value, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The configured embedding model or dimensions differ from the existing knowledge base. Clear and re-index documents before changing embedding settings.");
            }
        }
    }
}
