using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.Portfolio.App.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Bootstrap;

namespace MyPo.Blazor.Portfolio.App.Bootstrap;

[Bootstrapper]
public class BlazorServicesBootstrapper
{
	public static void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<IPortfolioApiClient, PortfolioApiClient>();
	}
}
