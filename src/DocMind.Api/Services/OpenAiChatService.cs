using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocMind.Api.Configuration;
using Microsoft.Extensions.Options;

namespace DocMind.Api.Services;

public sealed class OpenAiChatService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiChatService> logger)
{
    private readonly OpenAiOptions _options = options.Value;
    public string Model => _options.Model;

    public async Task<string> GetAnswerAsync(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ChatConfigurationException("The OpenAI API key is not configured. Set OpenAI:ApiKey using user secrets or an environment variable.");
        }

        var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : $"{_options.BaseUrl}/";
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseUrl), "chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new ChatCompletionRequest(
            _options.Model,
            [new ChatMessage("user", message)],
            _options.MaxCompletionTokens));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("The chat provider returned HTTP {StatusCode}.", (int)response.StatusCode);
            throw new ChatProviderException();
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);
        var answer = completion?.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ChatProviderException();
        }

        return answer;
    }

    private sealed record ChatCompletionRequest(string Model, IReadOnlyList<ChatMessage> Messages, int MaxCompletionTokens);
    private sealed record ChatMessage(string Role, string Content);
    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);
    private sealed record ChatChoice(ChatMessageContent Message);
    private sealed record ChatMessageContent(string Content);
}

public sealed class ChatConfigurationException(string message) : Exception(message);
public sealed class ChatProviderException() : Exception("The chat provider could not complete the request.");
