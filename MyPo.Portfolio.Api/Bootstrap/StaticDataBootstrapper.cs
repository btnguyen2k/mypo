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
        await Task.Delay(5000); // Delay a few seconds to allow the server to start up and be ready to serve requests
        var resourcesBasePath = $"./_content/MyPo.Blazor.Portfolio.App/resources/indices/";
        var resourceBaseUrl = new Uri(new Uri(configuration.GetValue("API:BaseUrl", string.Empty)), resourcesBasePath);
        await StaticDataCacher.CacheIndexConstituentsAsync(serviceProvider, resourceBaseUrl.AbsoluteUri);
    }
}
