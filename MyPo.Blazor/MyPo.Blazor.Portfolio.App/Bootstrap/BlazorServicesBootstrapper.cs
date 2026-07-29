using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyPo.Blazor.App;
using MyPo.Blazor.Portfolio.App.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Bootstrap;

namespace MyPo.Blazor.Portfolio.App.Bootstrap;

[Bootstrapper]
public class BlazorServicesBootstrapper
{
    public static void ConfigureServices(IServiceCollection services)
    {
		services.AddSingleton<IPortfolioApiClient, PortfolioApiClient>((sp) => {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var logger = sp.GetService<ILogger<PortfolioApiClient>>();
            return new PortfolioApiClient(httpClient, baseUrl: Globals.ApiBaseUrl??string.Empty, logger: logger);
        });
    }
}
