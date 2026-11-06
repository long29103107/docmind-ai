using System.Text;
using DocMind.Api.Configuration;
using DocMind.Api.Data;
using DocMind.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DocMind.Api.Documents;

public sealed class DocumentIngestionService(
    DocumentRepository repository,
    DocumentStorage storage,
    DocumentExtractor extractor,
    TextChunker chunker,
    OpenAiEmbeddingService embeddingService,
    IOptions<DocumentStorageOptions> options,
    IOptions<OpenAiOptions> openAiOptions,
    ILogger<DocumentIngestionService> logger)
{
    private const string IndexingFailure = "The document could not be indexed. Check that it is valid and that the embedding provider is configured.";
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".docx", ".txt", ".md"];

    public async Task<DocumentRecord> UploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new DocumentInputException("The uploaded file is empty.");
        }

        if (file.Length > options.Value.MaxFileSizeBytes)
        {
            throw new DocumentInputException("The file exceeds the configured upload limit.", StatusCodes.Status413PayloadTooLarge);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new DocumentInputException("Supported files are PDF, DOCX, TXT, and Markdown.");
        }

        await ValidateSignatureAsync(file, extension, cancellationToken);
        var id = Guid.NewGuid();
        var storageName = $"{id:N}{extension}";
        var fileName = NormalizeFileName(file.FileName, extension);
        var mediaType = GetMediaType(extension);
        var stored = false;
        DocumentRecord document;

        try
        {
            await using (var stream = file.OpenReadStream())
            {
                await storage.SaveAsync(storageName, stream, cancellationToken);
                stored = true;
            }

            document = await repository.CreateProcessingAsync(id, fileName, storageName, mediaType, file.Length, cancellationToken);
        }
        catch
        {
            if (stored)
            {
                storage.Delete(storageName);
            }

            throw;
        }

        try
        {
            var sections = await extractor.ExtractAsync(storage.GetPath(storageName), extension, cancellationToken);
            var extractedCharacters = sections.Sum(section => (long)section.Text.Length);
            if (extractedCharacters > options.Value.MaxExtractedCharacters)
            {
                throw new DocumentInputException("The extracted text exceeds the configured processing limit.", StatusCodes.Status413PayloadTooLarge);
            }

            var chunks = chunker.Chunk(sections);
            if (chunks.Count == 0)
            {
                throw new DocumentInputException("The document does not contain enough readable text to index.");
            }

            var embeddings = new List<float[]>(chunks.Count);
            foreach (var batch in chunks.Chunk(64))
            {
                var vectors = await embeddingService.EmbedAsync(batch.Select(chunk => chunk.Content).ToArray(), cancellationToken);
                embeddings.AddRange(vectors);
            }

            if (embeddings.Any(vector => vector.Length != openAiOptions.Value.EmbeddingDimensions))
            {
                throw new ChatProviderException();
            }

            await repository.SaveChunksAsync(id, chunks, embeddings, cancellationToken);
            await repository.MarkReadyAsync(id, cancellationToken);
            return document with { Status = "ready", IndexedAt = DateTimeOffset.UtcNow };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await repository.MarkFailedAsync(id, "Processing was cancelled. Upload the document again to retry.", CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            var failure = exception is DocumentInputException inputException ? inputException.Message : IndexingFailure;
            logger.LogWarning("Document {DocumentId} indexing failed ({ExceptionType}).", id, exception.GetType().Name);
            await repository.MarkFailedAsync(id, failure, CancellationToken.None);
            return document with { Status = "failed", FailureMessage = failure };
        }
    }

    public Task<IReadOnlyList<DocumentRecord>> ListAsync(CancellationToken cancellationToken) => repository.ListAsync(cancellationToken);

    public async Task<DocumentRecord?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await repository.FindAsync(id, cancellationToken);
        return found?.Document;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await repository.FindAsync(id, cancellationToken);
        if (found is null)
        {
            return false;
        }

        await repository.MarkDeletingAsync(id, cancellationToken);
        storage.Delete(found.Value.StorageName);
        await repository.DeleteAsync(id, cancellationToken);
        return true;
    }

    private static async Task ValidateSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var prefix = new byte[(int)Math.Min(8, file.Length)];
        await stream.ReadExactlyAsync(prefix, cancellationToken);

        var valid = extension switch
        {
            ".pdf" => Encoding.ASCII.GetString(prefix).StartsWith("%PDF-", StringComparison.Ordinal),
            ".docx" => prefix.Length >= 4 && prefix[0] == 0x50 && prefix[1] == 0x4B && prefix[2] == 0x03 && prefix[3] == 0x04,
            ".txt" or ".md" => !prefix.Contains((byte)0),
            _ => false
        };

        if (!valid)
        {
            throw new DocumentInputException("The file contents do not match the selected file type.");
        }
    }

    private static string NormalizeFileName(string input, string extension)
    {
        var name = Path.GetFileName(input.Replace('\\', '/'));
        var safe = new string(name.Where(character => !char.IsControl(character)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = $"document{extension}";
        }

        return safe.Length <= 255 ? safe : safe[..255];
    }

    private static string GetMediaType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".md" => "text/markdown",
        _ => "text/plain"
    };
}
