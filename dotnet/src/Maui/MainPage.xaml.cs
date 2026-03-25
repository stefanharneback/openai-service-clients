using OpenAiServiceClients.Core;
using OpenAiServiceClients.Core.Models;
using Microsoft.Maui.Dispatching;
using System.Text.Json;

namespace OpenAiServiceClients.Maui;

public partial class MainPage : ContentPage
{
    private const string StorageKeyClientApiKey = "client_api_key";
    private const string StorageKeyAdminApiKey = "admin_api_key";
    private const string PreferenceKeyModel = "selected_model";
    private const string PreferenceKeyWhisperModel = "whisper_model";
    private const string PreferenceKeyUsageLimit = "usage_limit";
    private const string PreferenceKeyUsageOffset = "usage_offset";
    private const string PreferenceKeyAdminLimit = "admin_limit";
    private const string PreferenceKeyAdminOffset = "admin_offset";
    private const string PreferenceKeyRememberAdminKey = "remember_admin_key";

    private readonly GatewayClient _gatewayClient;
    private readonly IDispatcherTimer _adminAutoLockTimer;
    private bool _settingsVisible;
    private bool _isAdminMode;

    public MainPage()
    {
        InitializeComponent();
        var httpClient = new HttpClient { BaseAddress = new Uri(AppConstants.GatewayBaseUrl) };
        _gatewayClient = new GatewayClient(httpClient);

        _adminAutoLockTimer = Dispatcher.CreateTimer();
        _adminAutoLockTimer.Interval = TimeSpan.FromMinutes(15);
        _adminAutoLockTimer.Tick += OnAdminAutoLockTick;
    }

    private void OnAdminAutoLockTick(object? sender, EventArgs eventArgs)
    {
        SetAdminMode(false, "Admin mode auto-locked after inactivity.");
    }

    private void ResetAdminAutoLockTimer()
    {
        _adminAutoLockTimer.Stop();
        if (_isAdminMode)
            _adminAutoLockTimer.Start();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadPreferences();
        await LoadStoredApiKeyAsync();
        await LoadStoredAdminKeyAsync();
        await LoadModelsAsync();
        SetAdminMode(false, "Admin mode is locked.");
    }

    private void LoadPreferences()
    {
        WhisperModelEntry.Text = Preferences.Default.Get(PreferenceKeyWhisperModel, "whisper-1");
        UsageLimitEntry.Text = Preferences.Default.Get(PreferenceKeyUsageLimit, "20");
        UsageOffsetEntry.Text = Preferences.Default.Get(PreferenceKeyUsageOffset, "0");
        AdminLimitEntry.Text = Preferences.Default.Get(PreferenceKeyAdminLimit, "20");
        AdminOffsetEntry.Text = Preferences.Default.Get(PreferenceKeyAdminOffset, "0");
        RememberAdminKeyCheckBox.IsChecked = Preferences.Default.Get(PreferenceKeyRememberAdminKey, false);
    }

    private void SavePreferences()
    {
        Preferences.Default.Set(PreferenceKeyWhisperModel, WhisperModelEntry.Text?.Trim() ?? "whisper-1");
        Preferences.Default.Set(PreferenceKeyUsageLimit, UsageLimitEntry.Text?.Trim() ?? "20");
        Preferences.Default.Set(PreferenceKeyUsageOffset, UsageOffsetEntry.Text?.Trim() ?? "0");
        Preferences.Default.Set(PreferenceKeyAdminLimit, AdminLimitEntry.Text?.Trim() ?? "20");
        Preferences.Default.Set(PreferenceKeyAdminOffset, AdminOffsetEntry.Text?.Trim() ?? "0");
        Preferences.Default.Set(PreferenceKeyRememberAdminKey, RememberAdminKeyCheckBox.IsChecked);
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

    private async Task LoadStoredAdminKeyAsync()
    {
        try
        {
            var stored = await SecureStorage.Default.GetAsync(StorageKeyAdminApiKey);
            if (!string.IsNullOrEmpty(stored))
                AdminKeyEntry.Text = stored;
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
        var preferredModel = Preferences.Default.Get(PreferenceKeyModel, string.Empty);
        ModelPicker.Items.Clear();
        foreach (var m in models)
            ModelPicker.Items.Add(m);
        if (ModelPicker.Items.Count == 0)
            return;

        var preferredIndex = string.IsNullOrWhiteSpace(preferredModel)
            ? -1
            : ModelPicker.Items.IndexOf(preferredModel);
        ModelPicker.SelectedIndex = preferredIndex >= 0 ? preferredIndex : 0;
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

    private static async Task TrySaveAdminKeyAsync(string adminKey)
    {
        if (string.IsNullOrWhiteSpace(adminKey)) return;
        try { await SecureStorage.Default.SetAsync(StorageKeyAdminApiKey, adminKey); }
        catch { /* best-effort */ }
    }

    private static void TryRemoveAdminKey()
    {
        try { SecureStorage.Default.Remove(StorageKeyAdminApiKey); }
        catch { /* best-effort */ }
    }

    private void SetAdminMode(bool enabled, string status)
    {
        _isAdminMode = enabled;
        AdminPanel.IsVisible = enabled;
        AdminModeStatusLabel.Text = status;
        AdminModeStatusLabel.TextColor = enabled ? Colors.Green : Colors.Gray;

        if (enabled)
            ResetAdminAutoLockTimer();
        else
            _adminAutoLockTimer.Stop();
    }

    private void OnToggleSettingsClicked(object? sender, EventArgs eventArgs)
    {
        _settingsVisible = !_settingsVisible;
        SettingsPanel.IsVisible = _settingsVisible;
        ToggleSettingsButton.Text = _settingsVisible ? "Hide Settings" : "Show Settings";
    }

    private async void OnRefreshModelsClicked(object? sender, EventArgs eventArgs)
    {
        await LoadModelsAsync();
        ResultEditor.Text = "Models refreshed from server (or fallback list if unavailable).";
    }

    private void OnModelSelectionChanged(object? sender, EventArgs eventArgs)
    {
        var selected = ModelPicker.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(selected))
            Preferences.Default.Set(PreferenceKeyModel, selected);
    }

    private async void OnUnlockAdminClicked(object? sender, EventArgs eventArgs)
    {
        var adminKey = AdminKeyEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(adminKey))
        {
            SetAdminMode(false, "Admin key is required to unlock admin mode.");
            return;
        }

        try
        {
            await _gatewayClient.GetAdminUsageAsync(adminKey, 1, 0);
            SetAdminMode(true, "Admin mode is unlocked for this session.");
            SavePreferences();

            if (RememberAdminKeyCheckBox.IsChecked)
                await TrySaveAdminKeyAsync(adminKey);
            else
                TryRemoveAdminKey();
        }
        catch
        {
            SetAdminMode(false, "Admin unlock failed. Check admin key.");
        }
    }

    private void OnLockAdminClicked(object? sender, EventArgs eventArgs)
    {
        SetAdminMode(false, "Admin mode is locked.");
    }

    private async void OnClearStoredKeysClicked(object? sender, EventArgs eventArgs)
    {
        var confirmed = await DisplayAlert(
            "Clear stored keys",
            "This will remove stored client/admin keys from this device. Continue?",
            "Clear",
            "Cancel");

        if (!confirmed)
            return;

        try
        {
            SecureStorage.Default.Remove(StorageKeyClientApiKey);
            SecureStorage.Default.Remove(StorageKeyAdminApiKey);
        }
        catch
        {
            // Best effort cleanup.
        }

        ApiKeyEntry.Text = string.Empty;
        AdminKeyEntry.Text = string.Empty;
        RememberAdminKeyCheckBox.IsChecked = false;
        Preferences.Default.Set(PreferenceKeyRememberAdminKey, false);
        SetAdminMode(false, "Admin mode is locked.");
        ResultEditor.Text = "Stored keys were cleared from this device.";
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
            SavePreferences();
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
            SavePreferences();
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
            await TrySaveApiKeyAsync(apiKey);
            SavePreferences();
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
            await TrySaveApiKeyAsync(apiKey);
            SavePreferences();
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
            if (!_isAdminMode)
            {
                ResultEditor.Text = "Admin mode is locked. Unlock admin mode in Settings first.";
                return;
            }

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
            SavePreferences();
            ResetAdminAutoLockTimer();
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
