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
    private bool _canRunModelQueries;

    public MainPage()
    {
        InitializeComponent();
        var httpClient = new HttpClient { BaseAddress = new Uri(AppConstants.GatewayBaseUrl) };
        _gatewayClient = new GatewayClient(httpClient);
        SetModelQueryAvailability(false, "Open Settings and set your client API key to load models.");
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
            SetModelQueryAvailability(false, "Client API key is required. Open Settings and save a key.");
            return;
        }

        try
        {
            var response = await _gatewayClient.GetModelsAsync(_clientApiKey);
            if (!ModelQueryPolicy.CanRunModelQueries(_clientApiKey, response.Models))
            {
                SetModelQueryAvailability(false, "Model catalog is empty. Update server allowlist and refresh models.");
                return;
            }

            PopulateModelPicker(response.Models);
            SetModelQueryAvailability(true, $"Loaded {response.Models.Count} model(s) from server.");
        }
        catch (GatewayApiException error)
        {
            SetModelQueryAvailability(false, $"Unable to load models: {error.Message}");
        }
        catch
        {
            SetModelQueryAvailability(false, "Unable to load models from server.");
        }
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

    private void SetModelQueryAvailability(bool enabled, string status)
    {
        _canRunModelQueries = enabled;
        LlmButton.IsEnabled = enabled;
        LlmStreamButton.IsEnabled = enabled;
        WhisperButton.IsEnabled = enabled;
        ModelPicker.IsEnabled = enabled;
        ModelLoadStatusLabel.Text = status;
    }

    private bool EnsureModelQueriesReady()
    {
        if (_canRunModelQueries)
            return true;

        ResultEditor.Text = "Model queries are disabled. Open Settings, save a valid API key, and refresh models from server.";
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
        if (!EnsureModelQueriesReady())
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
        if (!EnsureModelQueriesReady())
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
        if (!EnsureModelQueriesReady())
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

            if (root.TryGetProperty("type", out var type)
                && type.ValueKind == JsonValueKind.String
                && type.GetString() == "response.completed")
                return "\n\n[completed]";

            return string.Empty;
        }
        catch
        {
            return $"{data}{Environment.NewLine}";
        }
    }

    private void ShowUsage(LlmUsageSummary? usage)
    {
        var text = usage?.ToString() ?? string.Empty;
        UsageStatsLabel.IsVisible = !string.IsNullOrEmpty(text);
        UsageStatsLabel.Text = text;
    }
}
