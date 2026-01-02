using MyPo.Blazor.App.Helpers;
using MyPo.Blazor.App.Services;
using MyPo.Libs.Opurator;
using MyPo.Shared.Api;
using MyPo.Shared.Bootstrap;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace MyPo.Blazor.App.Bootstrap;

/// <summary>
/// Bootstrapper that registers services used by Blazor application.
/// </summary>
/// <remarks>
///		Bootstrappers in Blazor.App project are shared between for Blazor Server and Blazor WASM.
/// </remarks>
[Bootstrapper]
public class BlazorServicesBootstrapper
{
	public static void ConfigureServices(IServiceCollection services)
	{
		services.AddHttpClient();
		services.AddSingleton<IApiClient, ApiClient>();
		services.AddBlazoredLocalStorage();
		services.AddScoped<LocalStorageHelper>();
		services.AddTaskOperator();
		services.AddSingleton<StateContainer>();

		// https://stackoverflow.com/questions/52889827/remove-http-client-logging-handler-in-asp-net-core
		services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
	}
}
