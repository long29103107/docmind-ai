using System.Text.RegularExpressions;
using DocMind.Api.Data;
using DocMind.Api.Documents;
using DocMind.Api.Models;
using DocMind.Api.Services;
using Microsoft.Extensions.Options;

namespace DocMind.Api.Rag;

public sealed record ChatCitation(string Id, Guid DocumentId, string FileName, int? PageNumber, int ChunkIndex, string Excerpt);

public sealed record KnowledgeChatResponse(string Answer, string Model, IReadOnlyList<ChatCitation> Citations);

public sealed class KnowledgeChatService(
    DocumentRepository repository,
    OpenAiEmbeddingService embeddingService,
    OpenAiChatService chatService,
    IOptions<RagOptions> options)
{
    private static readonly Regex CitationPattern = new(@"\[(S\d+)\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private const string NoEvidence = "I couldn't find enough relevant information in this knowledge base to answer that.";

    public async Task<KnowledgeChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var questionEmbedding = await embeddingService.EmbedAsync([request.Message], cancellationToken);
        var chunks = await repository.SearchAsync(
            questionEmbedding[0],
            options.Value.MaxRetrievedChunks,
            options.Value.MinimumSimilarity,
            cancellationToken);
        if (chunks.Count == 0)
        {
            return new KnowledgeChatResponse(NoEvidence, chatService.Model, []);
        }

        var sources = chunks.Select((chunk, index) => ($"S{index + 1}", chunk)).ToArray();
        var answer = await chatService.GetGroundedAnswerAsync(request.Message, request.History ?? [], sources, cancellationToken);
        var usedIds = CitationPattern.Matches(answer)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var citations = sources
            .Where(source => usedIds.Contains(source.Item1))
            .Select(source => new ChatCitation(
                source.Item1,
                source.chunk.DocumentId,
                source.chunk.FileName,
                source.chunk.PageNumber,
                source.chunk.ChunkIndex,
                source.chunk.Content))
            .ToArray();

        if (citations.Length == 0)
        {
            return new KnowledgeChatResponse(NoEvidence, chatService.Model, []);
        }

        var knownIds = citations.Select(citation => citation.Id).ToHashSet(StringComparer.Ordinal);
        var safeAnswer = CitationPattern.Replace(answer, match => knownIds.Contains(match.Groups[1].Value) ? match.Value : string.Empty).Trim();
        return new KnowledgeChatResponse(safeAnswer, chatService.Model, citations);
    }
}
