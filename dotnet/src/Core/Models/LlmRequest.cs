using System.Text.Json.Serialization;

namespace OpenAiServiceClients.Core.Models;

public sealed record LlmRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("input")]
    public required string Input { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;

    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; set; }
}
