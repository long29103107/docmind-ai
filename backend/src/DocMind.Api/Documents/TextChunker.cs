using Microsoft.Extensions.Options;

namespace DocMind.Api.Documents;

public sealed class TextChunker(IOptions<RagOptions> options)
{
    public IReadOnlyList<DocumentChunk> Chunk(IReadOnlyList<ExtractedSection> sections)
    {
        var size = options.Value.ChunkSizeCharacters;
        var overlap = options.Value.ChunkOverlapCharacters;
        if (size < 100 || overlap < 0 || overlap >= size)
        {
            throw new InvalidOperationException("RAG chunk size configuration is invalid.");
        }

        var chunks = new List<DocumentChunk>();
        foreach (var section in sections)
        {
            var text = section.Text;
            var start = 0;
            while (start < text.Length)
            {
                var end = Math.Min(start + size, text.Length);
                if (end < text.Length)
                {
                    var boundary = text.LastIndexOfAny([' ', '\n', '\r', '\t'], end - 1, end - start);
                    if (boundary > start + size / 2)
                    {
                        end = boundary;
                    }
                }

                var content = text[start..end].Trim();
                if (content.Length > 0)
                {
                    chunks.Add(new DocumentChunk(chunks.Count, section.PageNumber, content));
                }

                if (end >= text.Length)
                {
                    break;
                }

                start = Math.Max(start + 1, end - overlap);
            }
        }

        return chunks;
    }
}
