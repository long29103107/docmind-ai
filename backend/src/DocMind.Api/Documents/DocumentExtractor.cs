using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DocMind.Api.Documents;

public sealed class DocumentExtractor
{
    public async Task<IReadOnlyList<ExtractedSection>> ExtractAsync(
        string path,
        string extension,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ExtractedSection> sections = extension switch
        {
            ".pdf" => ExtractPdf(path),
            ".docx" => ExtractDocx(path),
            ".txt" or ".md" => await ExtractTextAsync(path, cancellationToken),
            _ => throw new DocumentInputException("Supported files are PDF, DOCX, TXT, and Markdown.")
        };

        if (sections.All(section => string.IsNullOrWhiteSpace(section.Text)))
        {
            throw new DocumentInputException("No readable text was found. Scanned PDFs require OCR, which is not available yet.");
        }

        return sections;
    }

    private static IReadOnlyList<ExtractedSection> ExtractPdf(string path)
    {
        using var document = PdfDocument.Open(path);
        return document.GetPages()
            .Select(page => new ExtractedSection(page.Number, ContentOrderTextExtractor.GetText(page)))
            .ToArray();
    }

    private static IReadOnlyList<ExtractedSection> ExtractDocx(string path)
    {
        using var document = WordprocessingDocument.Open(path, false);
        var body = document.MainDocumentPart?.Document?.Body
            ?? throw new DocumentInputException("The Word document does not contain readable text.");
        var paragraphs = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
            .Select(paragraph => paragraph.InnerText.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text));
        return [new ExtractedSection(null, string.Join(Environment.NewLine, paragraphs))];
    }

    private static async Task<IReadOnlyList<ExtractedSection>> ExtractTextAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(path, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            return [new ExtractedSection(null, text)];
        }
        catch (DecoderFallbackException)
        {
            throw new DocumentInputException("Text files must use UTF-8 encoding.");
        }
    }
}
