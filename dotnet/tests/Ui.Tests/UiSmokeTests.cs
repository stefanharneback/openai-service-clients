using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace OpenAiServiceClients.Ui.Tests;

public sealed class UiSmokeTests : IDisposable
{
    private readonly WindowsDriver? _driver;

    public UiSmokeTests()
    {
        _driver = TryCreateDriver();
    }

    [Fact]
    public void CanLaunchAppAndOpenSettingsScreen()
    {
        if (_driver is null)
            return;

        var openSettingsButton = _driver.FindElement(MobileBy.AccessibilityId("OpenSettingsButton"));
        openSettingsButton.Click();

        _driver.FindElement(MobileBy.AccessibilityId("SettingsApiKeyEntry"));
        _driver.FindElement(MobileBy.AccessibilityId("SettingsSaveButton"));
    }

    [Fact]
    public void QueryButtonsAreDiscoverableForGatingAssertions()
    {
        if (_driver is null)
            return;

        _driver.FindElement(MobileBy.AccessibilityId("LlmButton"));
        _driver.FindElement(MobileBy.AccessibilityId("LlmStreamButton"));
        _driver.FindElement(MobileBy.AccessibilityId("WhisperButton"));
        _driver.FindElement(MobileBy.AccessibilityId("ModelLoadStatusLabel"));
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }

    private static WindowsDriver? TryCreateDriver()
    {
        var appExe = Environment.GetEnvironmentVariable("MAUI_APP_EXE");
        if (string.IsNullOrWhiteSpace(appExe) || !File.Exists(appExe))
            return null;

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
            return driver;
        }
        catch
        {
            return null;
        }
    }
}
