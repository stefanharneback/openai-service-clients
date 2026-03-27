using OpenAiServiceClients.Core;

namespace OpenAiServiceClients.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddHttpClient<GatewayClient>(httpClient =>
        {
            httpClient.BaseAddress = new Uri(AppConstants.GatewayBaseUrl);
        });

        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
