namespace OpenAiServiceClients.Core.Models;

public sealed record HealthResponse(
    bool Ok,
    string Service,
    string RequestId,
    string Now
);
