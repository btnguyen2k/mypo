using MyPo.Blazor.Demo.App.Services;
using MyPo.Demo.Shared.Api;
using MyPo.Shared.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

namespace MyPo.Blazor.Demo.App.Bootstrap;

[Bootstrapper]
public class BlazorServicesBootstrapper
{
	public static void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<IDemoApiClient, DemoApiClient>();
	}
}
