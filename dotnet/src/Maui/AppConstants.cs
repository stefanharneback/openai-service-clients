namespace OpenAiServiceClients.Maui;

internal static class AppConstants
{
    public const string GatewayBaseUrl = "https://openai-api-service.vercel.app";

    public static readonly IReadOnlyList<string> FallbackModels = ["gpt-5.4", "gpt-5.4-mini"];
}
