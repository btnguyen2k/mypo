using Microsoft.Extensions.Logging;
using MyPo.Portfolio.Shared.Helpers;
using MyPo.Shared.Bootstrap;

namespace MyPo.Blazor.Portfolio.App.Bootstrap;

[Bootstrapper]
public class StaticDataBootstrapper
{
	public static async Task InitializeServicesAsync(IServiceProvider serviceProvider, ILogger<StaticDataBootstrapper>? logger)
	{
		var resourcesBasePath = $"./_content/MyPo.Blazor.Portfolio.App/resources/indices/";
		var siteBaseUrl = Blazor.App.Globals.ApiBaseUrl;
		if (string.IsNullOrEmpty(siteBaseUrl))
		{
			throw new InvalidOperationException("Cannot determine site base URL.");
		}
		var resourceBaseUrl = new Uri(new Uri(siteBaseUrl), resourcesBasePath);
		await StaticDataCacher.CacheIndexConstituentsAsync(serviceProvider, resourceBaseUrl.AbsoluteUri);
	}
}
