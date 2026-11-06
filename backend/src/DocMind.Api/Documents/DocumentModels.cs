namespace DocMind.Api.Documents;

public sealed record DocumentRecord(
    Guid Id,
    string FileName,
    string MediaType,
    long FileSizeBytes,
    string Status,
    string? FailureMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? IndexedAt);

public sealed record ExtractedSection(int? PageNumber, string Text);

public sealed record DocumentChunk(int Index, int? PageNumber, string Content);

public sealed record RetrievedChunk(
    Guid DocumentId,
    string FileName,
    int ChunkIndex,
    int? PageNumber,
    string Content,
    double Similarity);

public sealed class DocumentStorageOptions
{
    public string RootPath { get; set; } = "storage/documents";
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
    public int MaxExtractedCharacters { get; set; } = 2_000_000;
}

public sealed class RagOptions
{
    public int ChunkSizeCharacters { get; set; } = 1200;
    public int ChunkOverlapCharacters { get; set; } = 150;
    public int MaxRetrievedChunks { get; set; } = 5;
    public double MinimumSimilarity { get; set; } = 0.25;
}

public sealed class DocumentInputException(string message, int statusCode = StatusCodes.Status400BadRequest)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
