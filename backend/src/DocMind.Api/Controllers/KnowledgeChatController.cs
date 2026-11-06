using DocMind.Api.Models;
using DocMind.Api.Rag;
using DocMind.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocMind.Api.Controllers;

[ApiController]
[Route("api/knowledge/chat")]
public sealed class KnowledgeChatController(KnowledgeChatService chatService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<KnowledgeChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<KnowledgeChatResponse>> Post(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await chatService.AskAsync(request, cancellationToken));
        }
        catch (ChatConfigurationException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Chat is not configured", detail: exception.Message);
        }
        catch (ChatProviderException)
        {
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: "Chat provider error", detail: "The chat provider could not complete the request.");
        }
        catch (HttpRequestException)
        {
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: "Chat provider unavailable", detail: "The chat provider could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(statusCode: StatusCodes.Status504GatewayTimeout, title: "Chat provider timeout", detail: "The chat provider took too long to respond.");
        }
    }
}
