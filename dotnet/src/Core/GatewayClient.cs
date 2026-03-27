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

    public async Task<HttpResponseMessage> PostLlmStreamAsync(
        LlmRequest request,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var streamRequest = request with { Stream = true };

        using var message = new HttpRequestMessage(HttpMethod.Post, "/v1/llm")
        {
            Content = JsonContent.Create(streamRequest, options: SerializerOptions),
        };

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var statusCode = response.StatusCode;
        response.Dispose();

        throw new GatewayApiException(
            statusCode,
            $"Gateway request failed with status {(int)statusCode}.",
            body);
    }

    public async Task<JsonDocument> PostWhisperAsync(
        Stream audioStream,
        string fileName,
        string model,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(model), "model");

        var streamContent = new StreamContent(audioStream);
        content.Add(streamContent, "file", fileName);

        using var message = new HttpRequestMessage(HttpMethod.Post, "/v1/whisper")
        {
            Content = content,
        };

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    public async Task<UsageResponse> GetUsageAsync(
        string apiKey,
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 100);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        using var message = new HttpRequestMessage(
            HttpMethod.Get,
            $"/v1/usage?limit={limit}&offset={offset}");

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<UsageResponse>(SerializerOptions, cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Usage payload was empty.");
        }

        return payload;
    }

    public async Task<AdminUsageResponse> GetAdminUsageAsync(
        string adminKey,
        int limit = 20,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 100);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        using var message = new HttpRequestMessage(
            HttpMethod.Get,
            $"/v1/admin/usage?limit={limit}&offset={offset}");

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminKey);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<AdminUsageResponse>(SerializerOptions, cancellationToken);
        if (payload is null)
        {
            throw new InvalidOperationException("Admin usage payload was empty.");
        }

        return payload;
    }

    public async Task<ModelsResponse> GetModelsAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, "/v1/models");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<ModelsResponse>(SerializerOptions, cancellationToken);
        return payload ?? throw new InvalidOperationException("Models payload was empty.");
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
