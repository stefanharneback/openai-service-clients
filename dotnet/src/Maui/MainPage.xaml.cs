using OpenAiServiceClients.Core;
using OpenAiServiceClients.Core.Models;
using System.Text.Json;

namespace OpenAiServiceClients.Maui;

public partial class MainPage : ContentPage
{
    private const string StorageKeyClientApiKey = "client_api_key";
    private const string PreferenceKeyModel = "selected_model";
    private const string PreferenceKeyWhisperModel = "whisper_model";

    private readonly GatewayClient _gatewayClient;
    private string _clientApiKey = string.Empty;
    private string _whisperModel = "whisper-1";
    private bool _canRunTextQueries;
    private bool _canRunWhisper;

    public MainPage()
    {
        InitializeComponent();
        var httpClient = new HttpClient { BaseAddress = new Uri(AppConstants.GatewayBaseUrl) };
        _gatewayClient = new GatewayClient(httpClient);
        SetInteractionAvailability(false, false, false, "Open Settings and set your client API key to load models.");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadSettingsAndModelsAsync();
    }

    private async Task ReloadSettingsAndModelsAsync()
    {
        await LoadStoredApiKeyAsync();
        _whisperModel = Preferences.Default.Get(PreferenceKeyWhisperModel, "whisper-1");
        await LoadModelsAsync();
    }

    private async Task LoadStoredApiKeyAsync()
    {
        try
        {
            _clientApiKey = await SecureStorage.Default.GetAsync(StorageKeyClientApiKey) ?? string.Empty;
        }
        catch
        {
            _clientApiKey = string.Empty;
        }
    }

    private async Task LoadModelsAsync()
    {
        ModelPicker.Items.Clear();
        ModelPicker.SelectedIndex = -1;

        if (string.IsNullOrWhiteSpace(_clientApiKey))
        {
            SetInteractionAvailability(false, false, false, "Client API key is required. Open Settings and save a key.");
            return;
        }

        try
        {
            var response = await _gatewayClient.GetModelsAsync(_clientApiKey);
            var textModels = FilterTextModels(response.Models);
            if (!ModelQueryPolicy.CanRunModelQueries(_clientApiKey, textModels))
            {
                var status = response.Unrestricted
                    ? "No text models were advertised. Refresh the catalog or update the gateway defaults. Whisper remains available."
                    : "No text models are available. Update the server allowlist and refresh models. Whisper remains available.";
                SetInteractionAvailability(false, true, false, status);
                return;
            }

            PopulateModelPicker(textModels);
            var loadedStatus = response.Unrestricted
                ? $"Loaded {textModels.Count} starter model(s); gateway allowlist is unrestricted."
                : $"Loaded {textModels.Count} text model(s) from server.";
            SetInteractionAvailability(true, true, true, loadedStatus);
        }
        catch (GatewayApiException error)
        {
            SetInteractionAvailability(false, true, false, $"Unable to load models: {error.Message}. Whisper remains available.");
        }
        catch
        {
            SetInteractionAvailability(false, true, false, "Unable to load models from server. Whisper remains available.");
        }
    }

    private static IReadOnlyList<string> FilterTextModels(IReadOnlyList<string> models)
    {
        return models
            .Where(static model => !string.IsNullOrWhiteSpace(model))
            .Where(static model =>
                !string.Equals(model, "whisper-1", StringComparison.OrdinalIgnoreCase)
                && model.IndexOf("transcribe", StringComparison.OrdinalIgnoreCase) < 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static model => model, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void PopulateModelPicker(IReadOnlyList<string> models)
    {
        var preferredModel = Preferences.Default.Get(PreferenceKeyModel, string.Empty);

        ModelPicker.Items.Clear();
        foreach (var model in models)
            ModelPicker.Items.Add(model);

        var preferredIndex = string.IsNullOrWhiteSpace(preferredModel)
            ? -1
            : ModelPicker.Items.IndexOf(preferredModel);

        ModelPicker.SelectedIndex = preferredIndex >= 0 ? preferredIndex : 0;
    }

    private void SetInteractionAvailability(bool canRunTextQueries, bool canRunWhisper, bool canSelectModel, string status)
    {
        _canRunTextQueries = canRunTextQueries;
        _canRunWhisper = canRunWhisper;
        LlmButton.IsEnabled = canRunTextQueries;
        LlmStreamButton.IsEnabled = canRunTextQueries;
        WhisperButton.IsEnabled = canRunWhisper;
        ModelPicker.IsEnabled = canSelectModel;
        ModelLoadStatusLabel.Text = status;
    }

    private bool EnsureTextQueriesReady()
    {
        if (_canRunTextQueries)
            return true;

        ResultEditor.Text = "Model queries are disabled. Open Settings, save a valid API key, and refresh models from server.";
        return false;
    }

    private bool EnsureWhisperReady()
    {
        if (_canRunWhisper)
            return true;

        ResultEditor.Text = "Whisper is disabled. Open Settings, save a valid API key, and refresh models.";
        return false;
    }

    private async void OnOpenSettingsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage());
    }

    private async void OnRefreshModelsClicked(object? sender, EventArgs e)
    {
        await ReloadSettingsAndModelsAsync();
    }

    private void OnModelSelectionChanged(object? sender, EventArgs e)
    {
        var selected = ModelPicker.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(selected))
            Preferences.Default.Set(PreferenceKeyModel, selected);
    }

    private async void OnHealthClicked(object? sender, EventArgs e)
    {
        ResultEditor.Text = "Loading health...";
        try
        {
            var payload = await _gatewayClient.GetHealthAsync();
            ResultEditor.Text = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception error)
        {
            ResultEditor.Text = error.Message;
        }
    }

    private async void OnLlmClicked(object? sender, EventArgs e)
    {
        if (!EnsureTextQueriesReady())
            return;

        ResultEditor.Text = "Loading llm response...";
        ShowUsage(null);

        try
        {
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
                _clientApiKey);

            ResultEditor.Text = payload.RootElement.GetRawText();
            ShowUsage(LlmPayloadHelper.TryExtractUsage(payload));
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

    private async void OnLlmStreamClicked(object? sender, EventArgs e)
    {
        if (!EnsureTextQueriesReady())
            return;

        ResultEditor.Text = "Streaming output:\n\n";
        ShowUsage(null);

        try
        {
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
                _clientApiKey);

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            LlmUsageSummary? streamUsage = null;
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                    break;

                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;

                var data = line[5..].Trim();
                var maybeUsage = LlmPayloadHelper.TryExtractUsageFromCompletedEvent(data);
                if (maybeUsage is not null)
                    streamUsage = maybeUsage;

                var chunk = ExtractDisplayChunk(data);
                if (chunk.Length == 0)
                    continue;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ResultEditor.Text += chunk;
                });
            }

            await MainThread.InvokeOnMainThreadAsync(() => ShowUsage(streamUsage));
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

    private async void OnWhisperClicked(object? sender, EventArgs e)
    {
        if (!EnsureWhisperReady())
            return;

        ResultEditor.Text = "Picking audio file...";

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select an audio file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".mp3", ".mp4", ".wav", ".m4a", ".webm", ".ogg", ".flac", ".aac" } },
                    { DevicePlatform.iOS, new[] { "public.audio" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.audio" } },
                    { DevicePlatform.Android, new[] { "audio/*" } },
                }),
            });

            if (result is null)
            {
                ResultEditor.Text = "No file selected.";
                return;
            }

            ResultEditor.Text = $"Transcribing {result.FileName}...";

            await using var stream = await result.OpenReadAsync();
            using var payload = await _gatewayClient.PostWhisperAsync(stream, result.FileName, _whisperModel, _clientApiKey);
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

    private static string ExtractDisplayChunk(string data)
    {
        if (data == "[DONE]")
            return "\n\n[done]";

        try
        {
            using var json = JsonDocument.Parse(data);
            var root = json.RootElement;

            if (root.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.String)
                return delta.GetString() ?? string.Empty;

            if (root.TryGetProperty("type", out var eventType)
                && eventType.ValueKind == JsonValueKind.String
                && eventType.GetString() == "response.output_item.done"
                && root.TryGetProperty("item", out var item))
            {
                return ExtractOutputItemText(item);
            }

            if (root.TryGetProperty("type", out var completionType)
                && completionType.ValueKind == JsonValueKind.String
                && completionType.GetString() == "response.completed")
                return "\n\n[completed]";

            if (root.TryGetProperty("type", out var failedType)
                && failedType.ValueKind == JsonValueKind.String
                && failedType.GetString() == "response.failed")
            {
                var errorCode = root.TryGetProperty("error", out var error)
                    && error.TryGetProperty("code", out var code)
                    && code.ValueKind == JsonValueKind.String
                        ? code.GetString()
                        : "unknown_error";
                var errorMessage = root.TryGetProperty("error", out error)
                    && error.TryGetProperty("message", out var message)
                    && message.ValueKind == JsonValueKind.String
                        ? message.GetString()
                        : "Streaming response failed.";
                return $"\n\n[failed: {errorCode}] {errorMessage}";
            }

            if (root.TryGetProperty("type", out var incompleteType)
                && incompleteType.ValueKind == JsonValueKind.String
                && incompleteType.GetString() == "response.incomplete")
            {
                var reason = root.TryGetProperty("response", out var response)
                    && response.TryGetProperty("status_details", out var statusDetails)
                    && statusDetails.TryGetProperty("reason", out var reasonValue)
                    && reasonValue.ValueKind == JsonValueKind.String
                        ? reasonValue.GetString()
                        : "unknown";
                return $"\n\n[incomplete: {reason}]";
            }

            return string.Empty;
        }
        catch
        {
            return $"{data}{Environment.NewLine}";
        }
    }

    private static string ExtractOutputItemText(JsonElement item)
    {
        if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var chunks = new List<string>();
        foreach (var contentItem in content.EnumerateArray())
        {
            if (!contentItem.TryGetProperty("type", out var type)
                || type.ValueKind != JsonValueKind.String
                || type.GetString() != "output_text")
            {
                continue;
            }

            if (contentItem.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
            {
                var value = text.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    chunks.Add(value.Trim());
            }
        }

        return string.Join(Environment.NewLine, chunks);
    }

    private void ShowUsage(LlmUsageSummary? usage)
    {
        var text = usage?.ToString() ?? string.Empty;
        UsageStatsLabel.IsVisible = !string.IsNullOrEmpty(text);
        UsageStatsLabel.Text = text;
    }
}
