using DocMind.Api.Documents;
using Npgsql;
using Pgvector;

namespace DocMind.Api.Data;

public sealed class DocumentRepository(NpgsqlDataSource dataSource, DatabaseInitializer initializer)
{
    public async Task<DocumentRecord> CreateProcessingAsync(
        Guid id,
        string fileName,
        string storageName,
        string mediaType,
        long fileSizeBytes,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO documents (id, file_name, storage_name, media_type, file_size_bytes, status)
            VALUES (@id, @file_name, @storage_name, @media_type, @file_size_bytes, 'processing')
            RETURNING created_at;
            """;
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("file_name", fileName);
        command.Parameters.AddWithValue("storage_name", storageName);
        command.Parameters.AddWithValue("media_type", mediaType);
        command.Parameters.AddWithValue("file_size_bytes", fileSizeBytes);
        var createdAt = (DateTime)(await command.ExecuteScalarAsync(cancellationToken))!;
        return new DocumentRecord(id, fileName, mediaType, fileSizeBytes, "processing", null, createdAt, null);
    }

    public async Task MarkReadyAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE documents SET status = 'ready', failure_message = NULL, indexed_at = NOW() WHERE id = @id";
        await ExecuteAsync(sql, command => command.Parameters.AddWithValue("id", id), cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string message, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE documents SET status = 'failed', failure_message = @message WHERE id = @id";
        await ExecuteAsync(sql, command =>
        {
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("message", message);
        }, cancellationToken);
    }

    public async Task SaveChunksAsync(
        Guid documentId,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<float[]> embeddings,
        CancellationToken cancellationToken)
    {
        if (chunks.Count != embeddings.Count)
        {
            throw new ArgumentException("Each document chunk must have exactly one embedding.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var delete = new NpgsqlCommand("DELETE FROM document_chunks WHERE document_id = @document_id", connection, transaction))
        {
            delete.Parameters.AddWithValue("document_id", documentId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insertSql = """
            INSERT INTO document_chunks (id, document_id, chunk_index, page_number, content, embedding)
            VALUES (@id, @document_id, @chunk_index, @page_number, @content, @embedding)
            """;
        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            await using var insert = new NpgsqlCommand(insertSql, connection, transaction);
            insert.Parameters.AddWithValue("id", Guid.NewGuid());
            insert.Parameters.AddWithValue("document_id", documentId);
            insert.Parameters.AddWithValue("chunk_index", chunk.Index);
            insert.Parameters.AddWithValue("page_number", (object?)chunk.PageNumber ?? DBNull.Value);
            insert.Parameters.AddWithValue("content", chunk.Content);
            insert.Parameters.AddWithValue("embedding", new Vector(embeddings[index]));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentRecord>> ListAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, file_name, media_type, file_size_bytes, status, failure_message, created_at, indexed_at
            FROM documents ORDER BY created_at DESC
            """;
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var documents = new List<DocumentRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            documents.Add(ReadDocument(reader));
        }

        return documents;
    }

    public async Task<(DocumentRecord Document, string StorageName)?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, file_name, media_type, file_size_bytes, status, failure_message, created_at, indexed_at, storage_name
            FROM documents WHERE id = @id
            """;
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (ReadDocument(reader), reader.GetString(8));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM documents WHERE id = @id";
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public Task MarkDeletingAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE documents SET status = 'deleting', failure_message = NULL WHERE id = @id";
        return ExecuteAsync(sql, command => command.Parameters.AddWithValue("id", id), cancellationToken);
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        float[] embedding,
        int limit,
        double minimumSimilarity,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.document_id, d.file_name, c.chunk_index, c.page_number, c.content,
                   1 - (c.embedding <=> @embedding) AS similarity
            FROM document_chunks c
            INNER JOIN documents d ON d.id = c.document_id
            WHERE d.status = 'ready' AND 1 - (c.embedding <=> @embedding) >= @minimum_similarity
            ORDER BY c.embedding <=> @embedding
            LIMIT @limit
            """;
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("embedding", new Vector(embedding));
        command.Parameters.AddWithValue("minimum_similarity", minimumSimilarity);
        command.Parameters.AddWithValue("limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var chunks = new List<RetrievedChunk>();
        while (await reader.ReadAsync(cancellationToken))
        {
            chunks.Add(new RetrievedChunk(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetInt32(3),
                reader.GetString(4),
                reader.GetDouble(5)));
        }

        return chunks;
    }

    private async Task ExecuteAsync(string sql, Action<NpgsqlCommand> addParameters, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        addParameters(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        await initializer.InitializeAsync(cancellationToken);
        return await dataSource.OpenConnectionAsync(cancellationToken);
    }

    private static DocumentRecord ReadDocument(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetInt64(3),
        reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetString(5),
        new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)),
        reader.IsDBNull(7) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)));
}
