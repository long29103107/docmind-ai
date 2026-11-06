using DocMind.Api.Documents;
using Microsoft.AspNetCore.Mvc;

namespace DocMind.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(DocumentIngestionService documentService) : ControllerBase
{
    private const long MaxMultipartRequestBytes = 21_000_000;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentRecord>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<DocumentRecord>> List(CancellationToken cancellationToken) =>
        documentService.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DocumentRecord>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentRecord>> Get(Guid id, CancellationToken cancellationToken)
    {
        var document = await documentService.GetAsync(id, cancellationToken);
        return document is null ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Document not found.") : Ok(document);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxMultipartRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxMultipartRequestBytes)]
    [ProducesResponseType<DocumentRecord>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<DocumentRecord>> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "A document file is required.");
        }

        try
        {
            var document = await documentService.UploadAsync(file, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = document.Id }, document);
        }
        catch (DocumentInputException exception)
        {
            return Problem(statusCode: exception.StatusCode, title: "The document could not be accepted.", detail: exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await documentService.DeleteAsync(id, cancellationToken))
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Document not found.");
        }

        return NoContent();
    }
}
