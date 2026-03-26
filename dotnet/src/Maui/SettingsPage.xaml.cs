namespace OpenAiServiceClients.Maui;

public partial class SettingsPage : ContentPage
{
    private const string StorageKeyClientApiKey = "client_api_key";
    private const string StorageKeyAdminApiKey = "admin_api_key";
    private const string PreferenceKeyWhisperModel = "whisper_model";
    private const string PreferenceKeyUsageLimit = "usage_limit";
    private const string PreferenceKeyUsageOffset = "usage_offset";
    private const string PreferenceKeyAdminLimit = "admin_limit";
    private const string PreferenceKeyAdminOffset = "admin_offset";
    private const string PreferenceKeyRememberAdminKey = "remember_admin_key";
    private const string PreferenceKeyAdminAutoLockMinutes = "admin_auto_lock_minutes";
    private static readonly string[] WhisperModels = ["whisper-1", "gpt-4o-transcribe", "gpt-4o-mini-transcribe"];
    private static readonly int[] AdminAutoLockOptionsMinutes = [1, 2, 5, 10, 15, 30, 60, 120];

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        PopulateWhisperPicker(Preferences.Default.Get(PreferenceKeyWhisperModel, "whisper-1"));
        PopulateAdminAutoLockPicker(Preferences.Default.Get(PreferenceKeyAdminAutoLockMinutes, 15));

        UsageLimitEntry.Text = Preferences.Default.Get(PreferenceKeyUsageLimit, "20");
        UsageOffsetEntry.Text = Preferences.Default.Get(PreferenceKeyUsageOffset, "0");
        AdminLimitEntry.Text = Preferences.Default.Get(PreferenceKeyAdminLimit, "20");
        AdminOffsetEntry.Text = Preferences.Default.Get(PreferenceKeyAdminOffset, "0");
        RememberAdminKeyCheckBox.IsChecked = Preferences.Default.Get(PreferenceKeyRememberAdminKey, false);

        _ = LoadStoredKeysAsync();
    }

    private async Task LoadStoredKeysAsync()
    {
        try
        {
            var clientKey = await SecureStorage.Default.GetAsync(StorageKeyClientApiKey);
            if (!string.IsNullOrWhiteSpace(clientKey))
                ApiKeyEntry.Text = clientKey;
        }
        catch
        {
            // Best effort only.
        }

        try
        {
            var adminKey = await SecureStorage.Default.GetAsync(StorageKeyAdminApiKey);
            if (!string.IsNullOrWhiteSpace(adminKey))
                AdminKeyEntry.Text = adminKey;
        }
        catch
        {
            // Best effort only.
        }
    }

    private void PopulateWhisperPicker(string selected)
    {
        WhisperModelPicker.Items.Clear();
        foreach (var model in WhisperModels)
            WhisperModelPicker.Items.Add(model);

        var index = WhisperModelPicker.Items.IndexOf(selected);
        WhisperModelPicker.SelectedIndex = index >= 0 ? index : 0;
    }

    private void PopulateAdminAutoLockPicker(int selected)
    {
        AdminAutoLockPicker.Items.Clear();
        foreach (var option in AdminAutoLockOptionsMinutes)
            AdminAutoLockPicker.Items.Add(option.ToString());

        var index = AdminAutoLockPicker.Items.IndexOf(selected.ToString());
        AdminAutoLockPicker.SelectedIndex = index >= 0 ? index : 4;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var apiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
        var adminKey = AdminKeyEntry.Text?.Trim() ?? string.Empty;

        Preferences.Default.Set(PreferenceKeyWhisperModel, (WhisperModelPicker.SelectedItem as string) ?? "whisper-1");
        Preferences.Default.Set(PreferenceKeyUsageLimit, UsageLimitEntry.Text?.Trim() ?? "20");
        Preferences.Default.Set(PreferenceKeyUsageOffset, UsageOffsetEntry.Text?.Trim() ?? "0");
        Preferences.Default.Set(PreferenceKeyAdminLimit, AdminLimitEntry.Text?.Trim() ?? "20");
        Preferences.Default.Set(PreferenceKeyAdminOffset, AdminOffsetEntry.Text?.Trim() ?? "0");
        Preferences.Default.Set(PreferenceKeyRememberAdminKey, RememberAdminKeyCheckBox.IsChecked);

        if (int.TryParse(AdminAutoLockPicker.SelectedItem as string, out var minutes))
            Preferences.Default.Set(PreferenceKeyAdminAutoLockMinutes, Math.Clamp(minutes, 1, 120));

        try
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
                await SecureStorage.Default.SetAsync(StorageKeyClientApiKey, apiKey);
            else
                SecureStorage.Default.Remove(StorageKeyClientApiKey);
        }
        catch
        {
            // Best effort only.
        }

        try
        {
            if (RememberAdminKeyCheckBox.IsChecked && !string.IsNullOrWhiteSpace(adminKey))
                await SecureStorage.Default.SetAsync(StorageKeyAdminApiKey, adminKey);
            else
                SecureStorage.Default.Remove(StorageKeyAdminApiKey);
        }
        catch
        {
            // Best effort only.
        }

        await Navigation.PopAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnClearStoredKeysClicked(object? sender, EventArgs e)
    {
        try
        {
            SecureStorage.Default.Remove(StorageKeyClientApiKey);
            SecureStorage.Default.Remove(StorageKeyAdminApiKey);
        }
        catch
        {
            // Best effort only.
        }

        ApiKeyEntry.Text = string.Empty;
        AdminKeyEntry.Text = string.Empty;
        RememberAdminKeyCheckBox.IsChecked = false;
        Preferences.Default.Set(PreferenceKeyRememberAdminKey, false);
        StatusLabel.Text = "Stored keys were cleared.";

        await Task.CompletedTask;
    }
}
