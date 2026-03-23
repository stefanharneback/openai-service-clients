using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OpenAiServiceClients.Core.Models;

namespace OpenAiServiceClients.Core;

public sealed class GatewayClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public GatewayClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("/health", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>(SerializerOptions, cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Health payload was empty.");
        }

        return payload;
    }

    public async Task<JsonDocument> PostLlmAsync(
        LlmRequest request,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/v1/llm")
        {
            Content = JsonContent.Create(request, options: SerializerOptions),
        };

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return json;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new GatewayApiException(
            response.StatusCode,
            $"Gateway request failed with status {(int)response.StatusCode}.",
            body);
    }
}
