using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocMind.Api.Configuration;
using Microsoft.Extensions.Options;

namespace DocMind.Api.Services;

public sealed class OpenAiEmbeddingService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiEmbeddingService> logger)
{
    private readonly OpenAiOptions _options = options.Value;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ChatConfigurationException("The OpenAI API key is not configured. Set OpenAI:ApiKey using user secrets or an environment variable.");
        }

        var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : $"{_options.BaseUrl}/";
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseUrl), "embeddings"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new EmbeddingRequest(_options.EmbeddingModel, inputs, _options.EmbeddingDimensions));
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("The embedding provider returned HTTP {StatusCode}.", (int)response.StatusCode);
            throw new ChatProviderException();
        }

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
        if (result?.Data is null || result.Data.Count != inputs.Count)
        {
            throw new ChatProviderException();
        }

        var vectors = result.Data.OrderBy(item => item.Index).Select(item => item.Embedding).ToArray();
        if (vectors.Any(vector => vector.Length != _options.EmbeddingDimensions))
        {
            logger.LogWarning("The embedding provider returned a vector with an unexpected number of dimensions.");
            throw new ChatProviderException();
        }

        return vectors;
    }

    private sealed record EmbeddingRequest(string Model, IReadOnlyList<string> Input, int Dimensions);
    private sealed record EmbeddingResponse(IReadOnlyList<EmbeddingData> Data);
    private sealed record EmbeddingData(float[] Embedding, int Index);
}
