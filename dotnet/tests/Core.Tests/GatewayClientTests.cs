using System.Net;
using System.Text;
using System.Text.Json;
using OpenAiServiceClients.Core;
using OpenAiServiceClients.Core.Models;

namespace OpenAiServiceClients.Core.Tests;

public sealed class GatewayClientTests
{
    [Fact]
    public void ModelQueryPolicy_ReturnsFalse_WhenApiKeyMissing()
    {
        var result = ModelQueryPolicy.CanRunModelQueries(string.Empty, ["gpt-5.4"]);
        Assert.False(result);
    }

    [Fact]
    public void ModelQueryPolicy_ReturnsFalse_WhenModelsMissing()
    {
        var result = ModelQueryPolicy.CanRunModelQueries("client-secret", []);
        Assert.False(result);
    }

    [Fact]
    public void ModelQueryPolicy_ReturnsTrue_WhenApiKeyAndModelsPresent()
    {
        var result = ModelQueryPolicy.CanRunModelQueries("client-secret", ["gpt-5.4-mini"]);
        Assert.True(result);
    }

    [Fact]
    public async Task GetHealthAsync_ParsesExpectedPayload()
    {
        var json = """
        {
          "ok": true,
          "service": "openai-api-service",
          "requestId": "abc-123",
          "now": "2026-03-23T00:00:00.000Z"
        }
        """;

        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

        var result = await client.GetHealthAsync();

        Assert.True(result.Ok);
        Assert.Equal("openai-api-service", result.Service);
        Assert.Equal("abc-123", result.RequestId);
    }

    [Fact]
    public async Task PostLlmAsync_SendsAuthorizationHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        var responseJson = """
        {
          "id": "resp_1",
          "status": "completed"
        }
        """;

        var client = CreateClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
        });

        using var payload = await client.PostLlmAsync(
            new LlmRequest
            {
                Model = "gpt-5.4-mini",
                Input = "hello",
                Stream = false,
            },
            "client-secret");

        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("client-secret", capturedRequest.Headers.Authorization?.Parameter);
        Assert.Equal("/v1/llm", capturedRequest.RequestUri?.AbsolutePath);
        Assert.Equal("resp_1", payload.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task PostLlmAsync_ThrowsGatewayApiException_OnError()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":{\"code\":\"invalid_auth\"}}", Encoding.UTF8, "application/json"),
        });

        var error = await Assert.ThrowsAsync<GatewayApiException>(async () =>
        {
            using var _ = await client.PostLlmAsync(
                new LlmRequest
                {
                    Model = "gpt-5.4-mini",
                    Input = "hello",
                },
                "bad-key");
        });

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Contains("invalid_auth", error.ResponseBody);
    }

    [Fact]
    public async Task PostLlmStreamAsync_SendsStreamRequestAndReturnsSsePayload()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedRequestBody = null;

        var client = CreateClient(request =>
        {
            capturedRequest = request;
            capturedRequestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"hello\"}\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream"),
            };
        });

        using var response = await client.PostLlmStreamAsync(
            new LlmRequest
            {
                Model = "gpt-5.4-mini",
                Input = "hello",
                Stream = false,
            },
            "client-secret");

        var body = await response.Content.ReadAsStringAsync();

        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequestBody);
        Assert.Contains("\"stream\":true", capturedRequestBody);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("data:", body);
    }

    [Fact]
    public async Task PostLlmStreamAsync_ThrowsGatewayApiException_OnError()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":{\"code\":\"rate_limit\"}}", Encoding.UTF8, "application/json"),
        });

        var error = await Assert.ThrowsAsync<GatewayApiException>(async () =>
        {
            using var _ = await client.PostLlmStreamAsync(
                new LlmRequest
                {
                    Model = "gpt-5.4-mini",
                    Input = "hello",
                },
                "client-secret");
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, error.StatusCode);
        Assert.Contains("rate_limit", error.ResponseBody);
    }

    [Fact]
    public async Task PostWhisperAsync_SendsMultipartAndReturnsJson()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var responseJson = """
        {
          "text": "Hello world."
        }
        """;

        var client = CreateClient(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
        });

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake-audio-data"));
        using var result = await client.PostWhisperAsync(audioStream, "test.mp3", "whisper-1", "client-secret");

        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("client-secret", capturedRequest.Headers.Authorization?.Parameter);
        Assert.Equal("/v1/whisper", capturedRequest.RequestUri?.AbsolutePath);
        Assert.Equal("multipart/form-data", capturedRequest.Content?.Headers.ContentType?.MediaType);
        Assert.NotNull(capturedBody);
        Assert.Contains("whisper-1", capturedBody);
        Assert.Equal("Hello world.", result.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public async Task PostWhisperAsync_ThrowsGatewayApiException_OnError()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":{\"code\":\"invalid_audio\"}}", Encoding.UTF8, "application/json"),
        });

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake-audio"));
        var error = await Assert.ThrowsAsync<GatewayApiException>(async () =>
        {
            using var _ = await client.PostWhisperAsync(audioStream, "test.mp3", "whisper-1", "client-secret");
        });

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Contains("invalid_audio", error.ResponseBody);
    }

    [Fact]
    public async Task GetUsageAsync_ReturnsUsageItems()
    {
        HttpRequestMessage? capturedRequest = null;
        var responseJson = """
        {
          "clientId": "client-abc",
          "items": [
            {
              "id": "item-1",
              "created_at": "2026-03-23T00:00:00.000Z",
              "endpoint": "/v1/llm",
              "model": "gpt-5.4-mini",
              "http_status": 200,
              "duration_ms": 420,
              "input_tokens": 10,
              "output_tokens": 50,
              "total_tokens": 60,
              "total_cost_usd": 0.00012
            }
          ],
          "limit": 20,
          "offset": 0
        }
        """;

        var client = CreateClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
        });

        var result = await client.GetUsageAsync("client-secret", limit: 20, offset: 0);

        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("client-secret", capturedRequest.Headers.Authorization?.Parameter);
        Assert.Contains("/v1/usage", capturedRequest.RequestUri?.PathAndQuery);
        Assert.Equal("client-abc", result.ClientId);
        Assert.Single(result.Items);
        Assert.Equal("item-1", result.Items[0].Id);
        Assert.Equal("/v1/llm", result.Items[0].Endpoint);
        Assert.Equal(60, result.Items[0].TotalTokens);
    }

    [Fact]
    public async Task GetUsageAsync_ThrowsGatewayApiException_OnError()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":{\"code\":\"invalid_auth\"}}", Encoding.UTF8, "application/json"),
        });

        var error = await Assert.ThrowsAsync<GatewayApiException>(
            async () => await client.GetUsageAsync("bad-key"));

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Contains("invalid_auth", error.ResponseBody);
    }

    [Fact]
    public async Task GetAdminUsageAsync_ReturnsAdminUsageItems()
    {
        HttpRequestMessage? capturedRequest = null;
        var responseJson = """
        {
          "items": [
            {
              "id": "item-2",
              "created_at": "2026-03-23T00:00:00.000Z",
              "endpoint": "/v1/whisper",
              "model": "whisper-1",
              "http_status": 200,
              "duration_ms": 800,
              "input_tokens": null,
              "output_tokens": null,
              "total_tokens": null,
              "total_cost_usd": null,
              "client_id": "client-xyz"
            }
          ],
          "limit": 20,
          "offset": 0
        }
        """;

        var client = CreateClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
        });

        var result = await client.GetAdminUsageAsync("admin-secret", limit: 20, offset: 0);

        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("admin-secret", capturedRequest.Headers.Authorization?.Parameter);
        Assert.Contains("/v1/admin/usage", capturedRequest.RequestUri?.PathAndQuery);
        Assert.Single(result.Items);
        Assert.Equal("client-xyz", result.Items[0].ClientId);
        Assert.Equal("/v1/whisper", result.Items[0].Endpoint);
    }

    [Fact]
    public async Task GetAdminUsageAsync_ThrowsGatewayApiException_OnForbidden()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("{\"error\":{\"code\":\"forbidden\"}}", Encoding.UTF8, "application/json"),
        });

        var error = await Assert.ThrowsAsync<GatewayApiException>(
            async () => await client.GetAdminUsageAsync("not-admin-key"));

        Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
        Assert.Contains("forbidden", error.ResponseBody);
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsModelsList()
    {
        HttpRequestMessage? capturedRequest = null;
        var responseJson = """
        {
          "models": ["gpt-5.4", "gpt-5.4-mini"]
        }
        """;

        var client = CreateClient(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            };
        });

        var result = await client.GetModelsAsync("client-secret");

        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("client-secret", capturedRequest.Headers.Authorization?.Parameter);
        Assert.Equal("/v1/models", capturedRequest.RequestUri?.AbsolutePath);
        Assert.Equal(2, result.Models.Count);
        Assert.Contains("gpt-5.4", result.Models);
        Assert.Contains("gpt-5.4-mini", result.Models);
    }

    [Fact]
    public async Task GetModelsAsync_ThrowsGatewayApiException_OnUnauthorized()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":{\"code\":\"invalid_auth\"}}", Encoding.UTF8, "application/json"),
        });

        var error = await Assert.ThrowsAsync<GatewayApiException>(
            async () => await client.GetModelsAsync("bad-key"));

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Contains("invalid_auth", error.ResponseBody);
    }

    private static GatewayClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHandler(responder);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:3000"),
        };

        return new GatewayClient(httpClient);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
