using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App;
using MyPo.Blazor.App.Helpers;
using MyPo.Blazor.App.Layout;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Bootstrap;

namespace MyPo.Blazor.Portfolio.App.Bootstrap;

[Bootstrapper]
public class DebugBootstrapper
{
    public static void ConfigureServices(IServiceCollection _)
    {
        // Wire up the sidebar's Debug action to the portfolio server-side debug API.
        // The base UI layer (MyPo.Blazor.App) does not reference the portfolio module, so it exposes
        // a static handler that downstream modules register.
        Sidebar.DebugHandler = async (serviceProvider) =>
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var localStorage = scope.ServiceProvider.GetRequiredService<LocalStorageHelper>();
                var authToken = await localStorage.GetItemAsync<string>(Globals.LOCAL_STORAGE_KEY_AUTH_TOKEN) ?? string.Empty;
                var apiClient = scope.ServiceProvider.GetRequiredService<IPortfolioApiClient>();
                var resp = await apiClient.DebugAsync(authToken, Globals.ApiBaseUrl);
                if (resp.Status == 200)
                {
                    return resp.Data ?? [];
                }
                return [$"Error [{resp.Status}]: {resp.Message}"];
            }
        };
    }
}

