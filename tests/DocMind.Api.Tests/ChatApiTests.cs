using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DocMind.Api.Configuration;
using DocMind.Api.Models;
using DocMind.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocMind.Api.Tests;

public sealed class ChatApiTests
{
    [Fact]
    public async Task Post_ReturnsAnswerAndSendsConfiguredProviderRequest()
    {
        var handler = new StubProviderHandler(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://provider.test/v1/chat/completions", request.RequestUri?.ToString());
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-api-key", request.Headers.Authorization?.Parameter);

            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Assert.Equal("test-model", body.RootElement.GetProperty("model").GetString());
            Assert.Equal("Hello", body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString());

            return JsonResponse(HttpStatusCode.OK, """{"choices":[{"message":{"content":"Hello from the test provider."}}]}""");
        });

        using var factory = CreateFactory(handler);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/chat", new ChatRequest { Message = "Hello" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(chatResponse);
        Assert.Equal("Hello from the test provider.", chatResponse.Answer);
        Assert.Equal("test-model", chatResponse.Model);
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task Post_RejectsEmptyMessageWithoutCallingProvider()
    {
        var handler = new StubProviderHandler((_, _) => throw new InvalidOperationException("Provider should not be called."));
        using var factory = CreateFactory(handler);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/chat", new ChatRequest { Message = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task Post_ReturnsServiceUnavailableWhenApiKeyIsMissing()
    {
        var handler = new StubProviderHandler((_, _) => throw new InvalidOperationException("Provider should not be called."));
        using var factory = CreateFactory(handler, apiKey: "");
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/chat", new ChatRequest { Message = "Hello" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("not configured", body, StringComparison.OrdinalIgnoreCase);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task Post_ReturnsSanitizedBadGatewayWhenProviderFails()
    {
        var handler = new StubProviderHandler((_, _) => Task.FromResult(
            JsonResponse(HttpStatusCode.TooManyRequests, "upstream secret detail")));
        using var factory = CreateFactory(handler);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/chat", new ChatRequest { Message = "Hello" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.DoesNotContain("upstream secret detail", body, StringComparison.Ordinal);
        Assert.DoesNotContain("test-api-key", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_MapsProviderTimeoutToGatewayTimeout()
    {
        var handler = new StubProviderHandler((_, _) => Task.FromException<HttpResponseMessage>(
            new TaskCanceledException("Simulated provider timeout.")));
        using var factory = CreateFactory(handler);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/chat", new ChatRequest { Message = "Hello" });

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
    }

    [Fact]
    public async Task ChatService_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var handler = new StubProviderHandler((_, _) => throw new InvalidOperationException("Canceled request must not reach the provider."));
        using var httpClient = new HttpClient(handler);
        var service = new OpenAiChatService(
            httpClient,
            Options.Create(new OpenAiOptions
            {
                ApiKey = "test-api-key",
                BaseUrl = "https://provider.test/v1/",
                Model = "test-model"
            }),
            NullLogger<OpenAiChatService>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.GetAnswerAsync("Hello", cancellation.Token));
        Assert.False(handler.WasCalled);
    }

    private static ChatApiFactory CreateFactory(HttpMessageHandler handler, string apiKey = "test-api-key") =>
        new(handler, apiKey);

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

    private sealed class ChatApiFactory(HttpMessageHandler handler, string apiKey) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OpenAI:ApiKey"] = apiKey,
                    ["OpenAI:BaseUrl"] = "https://provider.test/v1/",
                    ["OpenAI:Model"] = "test-model"
                }));

            builder.ConfigureTestServices(services => services
                .AddHttpClient<OpenAiChatService>()
                .ConfigurePrimaryHttpMessageHandler(() => handler));
        }
    }

    private sealed class StubProviderHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return sendAsync(request, cancellationToken);
        }
    }
}
