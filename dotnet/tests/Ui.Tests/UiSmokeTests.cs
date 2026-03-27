using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using Xunit.Sdk;

namespace OpenAiServiceClients.Ui.Tests;

public sealed class UiSmokeTests : IDisposable
{
    private readonly WindowsDriver? _driver;
    private readonly string? _skipReason;

    public UiSmokeTests()
    {
        (_driver, _skipReason) = TryCreateDriver();
    }

    [Fact]
    public void CanLaunchAppAndOpenSettingsScreen()
    {
        var driver = RequireDriver();
        var openSettingsButton = driver.FindElement(MobileBy.AccessibilityId("OpenSettingsButton"));
        openSettingsButton.Click();

        driver.FindElement(MobileBy.AccessibilityId("SettingsApiKeyEntry"));
        driver.FindElement(MobileBy.AccessibilityId("SettingsSaveButton"));
    }

    [Fact]
    public void QueryButtonsAreDiscoverableForGatingAssertions()
    {
        var driver = RequireDriver();
        driver.FindElement(MobileBy.AccessibilityId("LlmButton"));
        driver.FindElement(MobileBy.AccessibilityId("LlmStreamButton"));
        driver.FindElement(MobileBy.AccessibilityId("WhisperButton"));
        driver.FindElement(MobileBy.AccessibilityId("ModelLoadStatusLabel"));
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }

    private WindowsDriver RequireDriver()
    {
        return _driver ?? throw new XunitException(_skipReason ?? "Appium driver was unavailable.");
    }

    private static (WindowsDriver? Driver, string? SkipReason) TryCreateDriver()
    {
        var appExe = Environment.GetEnvironmentVariable("MAUI_APP_EXE");
        if (string.IsNullOrWhiteSpace(appExe) || !File.Exists(appExe))
            return (null, "Set MAUI_APP_EXE to a built app path before running UI smoke tests.");

        var appiumServer = Environment.GetEnvironmentVariable("APPIUM_SERVER_URL");
        var appiumUri = string.IsNullOrWhiteSpace(appiumServer)
            ? new Uri("http://127.0.0.1:4723/")
            : new Uri(appiumServer);

        try
        {
            var options = new AppiumOptions();
            options.PlatformName = "Windows";
            options.AutomationName = "Windows";
            options.AddAdditionalAppiumOption("app", appExe);
            options.AddAdditionalAppiumOption("ms:waitForAppLaunch", "25");

            var driver = new WindowsDriver(appiumUri, options, TimeSpan.FromSeconds(30));
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
            return (driver, null);
        }
        catch (Exception error)
        {
            return (null, $"Unable to start Appium or launch the MAUI app: {error.Message}");
        }
    }
}
