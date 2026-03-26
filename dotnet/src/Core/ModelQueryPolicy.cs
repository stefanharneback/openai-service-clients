namespace OpenAiServiceClients.Core;

public static class ModelQueryPolicy
{
    public static bool CanRunModelQueries(string apiKey, IReadOnlyList<string>? models)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        return models is { Count: > 0 };
    }
}
