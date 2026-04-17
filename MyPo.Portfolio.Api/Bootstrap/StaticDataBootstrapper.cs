using MyPo.Portfolio.Shared.Helpers;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Bootstrapper that registers services used by Blazor application.
/// </summary>
/// <remarks>
///		This bootstrapper is shared between Blazor Server and Blazor WASM envs.
/// </remarks>
[Bootstrapper]
public class StaticDataBootstrapper
{
	public static async Task InitializeServicesAsync(IServiceProvider serviceProvider, IConfiguration configuration)
	{
		var resourcesBasePath = $"./_content/MyPo.Blazor.Portfolio.App/resources/indices/";
		var resourceBaseUrl = new Uri(new Uri(configuration.GetValue("API:BaseUrl", string.Empty)), resourcesBasePath);
		await StaticDataCacher.CacheIndexConstituentsAsync(serviceProvider, resourceBaseUrl.AbsoluteUri);
	}
}
