using DocMind.Api.Models;
using DocMind.Api.Rag;
using DocMind.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DocMind.Api.Controllers;

[ApiController]
[Route("api/knowledge/chat")]
[Produces("text/event-stream")]
public sealed class KnowledgeChatController(KnowledgeChatService chatService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task Post(ChatRequest request, CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            var result = await chatService.StreamAsync(request, async (text, token) =>
            {
                await WriteEventAsync("token", new { text }, token);
            }, cancellationToken);
            await WriteEventAsync("complete", result, cancellationToken);
        }
        catch (ChatConfigurationException exception)
        {
            await WriteEventAsync("error", new { message = exception.Message }, cancellationToken);
        }
        catch (ChatProviderException)
        {
            await WriteEventAsync("error", new { message = "The chat provider could not complete the request." }, cancellationToken);
        }
        catch (HttpRequestException)
        {
            await WriteEventAsync("error", new { message = "The chat provider could not be reached." }, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await WriteEventAsync("error", new { message = "The chat provider took too long to respond." }, cancellationToken);
        }
    }

    private async Task WriteEventAsync<T>(string eventName, T data, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Response.WriteAsync($"event: {eventName}\ndata: {payload}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
