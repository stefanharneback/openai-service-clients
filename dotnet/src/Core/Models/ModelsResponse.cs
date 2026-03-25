using System.Text.Json.Serialization;

namespace OpenAiServiceClients.Core.Models;

public sealed record ModelsResponse(
    [property: JsonPropertyName("models")] IReadOnlyList<string> Models
);
