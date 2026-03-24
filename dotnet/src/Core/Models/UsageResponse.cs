using System.Text.Json.Serialization;

namespace OpenAiServiceClients.Core.Models;

public sealed record UsageResponse(
    [property: JsonPropertyName("clientId")] string ClientId,
    [property: JsonPropertyName("items")] IReadOnlyList<UsageItem> Items,
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("offset")] int Offset
);

public sealed record AdminUsageResponse(
    [property: JsonPropertyName("items")] IReadOnlyList<AdminUsageItem> Items,
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("offset")] int Offset
);
