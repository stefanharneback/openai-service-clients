using System.Text.Json.Serialization;

namespace OpenAiServiceClients.Core.Models;

public sealed record UsageItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("endpoint")] string Endpoint,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("http_status")] int HttpStatus,
    [property: JsonPropertyName("duration_ms")] int DurationMs,
    [property: JsonPropertyName("input_tokens")] int? InputTokens,
    [property: JsonPropertyName("output_tokens")] int? OutputTokens,
    [property: JsonPropertyName("total_tokens")] int? TotalTokens,
    [property: JsonPropertyName("total_cost_usd")] decimal? TotalCostUsd
);

public sealed record AdminUsageItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("endpoint")] string Endpoint,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("http_status")] int HttpStatus,
    [property: JsonPropertyName("duration_ms")] int DurationMs,
    [property: JsonPropertyName("input_tokens")] int? InputTokens,
    [property: JsonPropertyName("output_tokens")] int? OutputTokens,
    [property: JsonPropertyName("total_tokens")] int? TotalTokens,
    [property: JsonPropertyName("total_cost_usd")] decimal? TotalCostUsd,
    [property: JsonPropertyName("client_id")] string? ClientId
);
