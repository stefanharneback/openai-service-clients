using OpenAiServiceClients.Core;
using OpenAiServiceClients.Core.Models;
using System.Text.Json;

namespace OpenAiServiceClients.Maui;

public partial class MainPage : ContentPage
{
    private const string StorageKeyClientApiKey = "client_api_key";

    private readonly GatewayClient _gatewayClient;

    public MainPage()
    {
        InitializeComponent();
        var httpClient = new HttpClient { BaseAddress = new Uri(AppConstants.GatewayBaseUrl) };
        _gatewayClient = new GatewayClient(httpClient);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStoredApiKeyAsync();
        await LoadModelsAsync();
    }

    private async Task LoadStoredApiKeyAsync()
    {
        try
        {
            var stored = await SecureStorage.Default.GetAsync(StorageKeyClientApiKey);
            if (!string.IsNullOrEmpty(stored))
                ApiKeyEntry.Text = stored;
        }
        catch
        {
            // SecureStorage may be unavailable in some environments.
        }
    }

    private async Task LoadModelsAsync()
    {
        try
        {
            var apiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
            ModelsResponse? modelsResponse = string.IsNullOrWhiteSpace(apiKey)
                ? null
                : await _gatewayClient.GetModelsAsync(apiKey);
            PopulateModelPicker(modelsResponse?.Models ?? AppConstants.FallbackModels);
        }
        catch
        {
            PopulateModelPicker(AppConstants.FallbackModels);
        }
    }

    private void PopulateModelPicker(IReadOnlyList<string> models)
    {
        ModelPicker.Items.Clear();
        foreach (var m in models)
            ModelPicker.Items.Add(m);
        if (ModelPicker.Items.Count > 0)
            ModelPicker.SelectedIndex = 0;
    }

    private void ShowUsage(LlmUsageSummary? usage)
    {
        var text = usage?.ToString() ?? string.Empty;
        UsageStatsLabel.IsVisible = !string.IsNullOrEmpty(text);
        UsageStatsLabel.Text = text;
    }

    private static async Task TrySaveApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return;
        try { await SecureStorage.Default.SetAsync(StorageKeyClientApiKey, apiKey); }
        catch { /* best-effort */ }
    }

    private async void OnHealthClicked(object? sender, EventArgs eventArgs)
    {
        ResultEditor.Text = "Loading health...";
        try
        {
            var payload = await _gatewayClient.GetHealthAsync();
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
        ShowUsage(null);
        try
        {
            var apiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ResultEditor.Text = "Client API key is required.";
                return;
            }

            var model = (ModelPicker.SelectedItem as string) ?? string.Empty;
            var input = InputEditor.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(input))
            {
                ResultEditor.Text = "Model and input are required.";
                return;
            }

            using var payload = await _gatewayClient.PostLlmAsync(
                new LlmRequest
                {
                    Model = model,
                    Input = input,
                    Stream = false,
                },
                apiKey);

            ResultEditor.Text = payload.RootElement.GetRawText();
            ShowUsage(LlmPayloadHelper.TryExtractUsage(payload));
            await TrySaveApiKeyAsync(apiKey);
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

    private async void OnLlmStreamClicked(object? sender, EventArgs eventArgs)
    {
        ResultEditor.Text = "Streaming output:\n\n";
        ShowUsage(null);

        try
        {
            var apiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ResultEditor.Text = "Client API key is required.";
                return;
            }

            var model = (ModelPicker.SelectedItem as string) ?? string.Empty;
            var input = InputEditor.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(input))
            {
                ResultEditor.Text = "Model and input are required.";
                return;
            }

            using var response = await _gatewayClient.PostLlmStreamAsync(
                new LlmRequest
                {
                    Model = model,
                    Input = input,
                    Stream = true,
                },
                apiKey);

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            LlmUsageSummary? streamUsage = null;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                {
                    continue;
                }

                var data = line[5..].Trim();
                var maybeUsage = LlmPayloadHelper.TryExtractUsageFromCompletedEvent(data);
                if (maybeUsage is not null)
                    streamUsage = maybeUsage;
                var chunk = ExtractDisplayChunk(data);
                if (chunk.Length == 0)
                {
                    continue;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ResultEditor.Text += chunk;
                });
            }

            await MainThread.InvokeOnMainThreadAsync(() => ShowUsage(streamUsage));
            await TrySaveApiKeyAsync(apiKey);
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

    private static string ExtractDisplayChunk(string data)
    {
        if (data == "[DONE]")
        {
            return "\n\n[done]";
        }

        try
        {
            using var json = JsonDocument.Parse(data);
            var root = json.RootElement;

            if (root.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.String)
            {
                return delta.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("type", out var type)
                && type.ValueKind == JsonValueKind.String
                && type.GetString() == "response.completed")
            {
                return "\n\n[completed]";
            }

            return string.Empty;
        }
        catch
        {
            return $"{data}{Environment.NewLine}";
        }
    }


    private async void OnWhisperClicked(object? sender, EventArgs eventArgs)
    {
        ResultEditor.Text = "Picking audio file...";
        try
        {
            var apiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ResultEditor.Text = "Client API key is required.";
                return;
            }

            var model = WhisperModelEntry.Text?.Trim() ?? "whisper-1";

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select an audio file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI,    new[] { ".mp3", ".mp4", ".wav", ".m4a", ".webm", ".ogg", ".flac", ".aac" } },
                    { DevicePlatform.iOS,      new[] { "public.audio" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.audio" } },
                    { DevicePlatform.Android,  new[] { "audio/*" } },
                }),
            });

            if (result is null)
            {
                ResultEditor.Text = "No file selected.";
                return;
            }

            ResultEditor.Text = $"Transcribing {result.FileName}...";

            await using var stream = await result.OpenReadAsync();
            using var payload = await _gatewayClient.PostWhisperAsync(stream, result.FileName, model, apiKey);
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

    private async void OnUsageClicked(object? sender, EventArgs eventArgs)
    {
        ResultEditor.Text = "Loading usage...";
        try
        {
            var apiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ResultEditor.Text = "Client API key is required.";
                return;
            }

            if (!int.TryParse(UsageLimitEntry.Text, out var limit))  limit = 20;
            if (!int.TryParse(UsageOffsetEntry.Text, out var offset)) offset = 0;

            var payload = await _gatewayClient.GetUsageAsync(apiKey, limit, offset);
            ResultEditor.Text = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
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

    private async void OnAdminUsageClicked(object? sender, EventArgs eventArgs)
    {
        ResultEditor.Text = "Loading admin usage...";
        try
        {
            var adminKey = AdminKeyEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(adminKey))
            {
                ResultEditor.Text = "Admin key is required.";
                return;
            }

            if (!int.TryParse(AdminLimitEntry.Text, out var limit))  limit = 20;
            if (!int.TryParse(AdminOffsetEntry.Text, out var offset)) offset = 0;

            var payload = await _gatewayClient.GetAdminUsageAsync(adminKey, limit, offset);
            ResultEditor.Text = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
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
}
