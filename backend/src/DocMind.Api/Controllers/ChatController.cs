using DocMind.Api.Models;
using DocMind.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocMind.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(OpenAiChatService chatService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ChatResponse>> Post(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var answer = await chatService.GetAnswerAsync(request.Message, cancellationToken);
            return Ok(new ChatResponse(answer, chatService.Model));
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
