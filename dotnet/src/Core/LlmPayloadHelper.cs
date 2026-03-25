using System.Text.Json;

namespace OpenAiServiceClients.Core;

/// <summary>Token usage summary extracted from a gateway LLM response.</summary>
public sealed record LlmUsageSummary(
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens)
{
    public override string ToString()
    {
        var parts = new List<string>();
        if (InputTokens.HasValue)  parts.Add($"↑ {InputTokens} in");
        if (OutputTokens.HasValue) parts.Add($"↓ {OutputTokens} out");
        if (TotalTokens.HasValue)  parts.Add($"{TotalTokens} total");
        return parts.Count > 0 ? string.Join("  ", parts) : string.Empty;
    }
}

public static class LlmPayloadHelper
{
    /// <summary>
    /// Extracts token usage from a non-streaming LLM JSON response document.
    /// Returns null when no usage block is present.
    /// </summary>
    public static LlmUsageSummary? TryExtractUsage(JsonDocument document)
    {
        var root = document.RootElement;
        if (!root.TryGetProperty("usage", out var usage))
            return null;

        return ParseUsageElement(usage);
    }

    /// <summary>
    /// Extracts token usage from the data payload of a <c>response.completed</c> SSE event.
    /// Returns null for any other event or when usage is absent.
    /// </summary>
    public static LlmUsageSummary? TryExtractUsageFromCompletedEvent(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            if (!root.TryGetProperty("response", out var resp))
                return null;

            if (!resp.TryGetProperty("usage", out var usage))
                return null;

            return ParseUsageElement(usage);
        }
        catch
        {
            return null;
        }
    }

    private static LlmUsageSummary ParseUsageElement(JsonElement usage)
    {
        int? Get(string name) =>
            usage.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number
                ? el.GetInt32()
                : null;

        return new LlmUsageSummary(Get("input_tokens"), Get("output_tokens"), Get("total_tokens"));
    }
}
