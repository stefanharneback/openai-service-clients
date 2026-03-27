namespace OpenAiServiceClients.Maui;

public partial class App : Application
{
    public static event EventHandler? EnteredBackground;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        EnteredBackground?.Invoke(this, EventArgs.Empty);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = Handler?.MauiContext?.Services.GetRequiredService<MainPage>()
            ?? throw new InvalidOperationException("Unable to resolve MainPage from DI.");
        return new Window(new NavigationPage(mainPage));
    }
}
