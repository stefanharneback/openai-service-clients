using OpenAiServiceClients.Core;
using OpenAiServiceClients.Core.Models;

namespace OpenAiServiceClients.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnHealthClicked(object? sender, EventArgs eventArgs)
    {
        ResultEditor.Text = "Loading health...";
        try
        {
            var client = BuildGatewayClient();
            var payload = await client.GetHealthAsync();
            ResultEditor.Text = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
            });
        }
        catch (Exception error)
        {
            ResultEditor.Text = error.Message;
        }
    }

    private async void OnLlmClicked(object? sender, EventArgs eventArgs)
    {
        ResultEditor.Text = "Loading llm response...";
        try
        {
            var apiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ResultEditor.Text = "Client API key is required.";
                return;
            }

            var model = ModelEntry.Text?.Trim() ?? string.Empty;
            var input = InputEditor.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(input))
            {
                ResultEditor.Text = "Model and input are required.";
                return;
            }

            var client = BuildGatewayClient();
            using var payload = await client.PostLlmAsync(
                new LlmRequest
                {
                    Model = model,
                    Input = input,
                    Stream = false,
                },
                apiKey);

            ResultEditor.Text = payload.RootElement.GetRawText();
        }
        catch (GatewayApiException error)
        {
            ResultEditor.Text = $"{error.Message}{Environment.NewLine}{error.ResponseBody}";
        }
        catch (Exception error)
        {
            ResultEditor.Text = error.Message;
        }
    }

    private GatewayClient BuildGatewayClient()
    {
        var baseUrl = BaseUrlEntry.Text?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Gateway base URL must be an absolute URI.");
        }

        var httpClient = new HttpClient
        {
            BaseAddress = uri,
        };

        return new GatewayClient(httpClient);
    }
}
