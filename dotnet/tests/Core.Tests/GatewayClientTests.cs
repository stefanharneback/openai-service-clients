using System.Net;
using System.Text;
using System.Text.Json;
using OpenAiServiceClients.Core;
using OpenAiServiceClients.Core.Models;

namespace OpenAiServiceClients.Core.Tests;

public sealed class GatewayClientTests
{
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
