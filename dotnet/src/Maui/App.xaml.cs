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
        return new Window(new MainPage());
    }
}
