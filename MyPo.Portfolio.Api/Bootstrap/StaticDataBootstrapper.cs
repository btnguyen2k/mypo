using MyPo.Portfolio.Shared.Helpers;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Loads index constituent data from FinHub into the server-side cache.
/// </summary>
[Bootstrapper]
public class StaticDataBootstrapper
{
    public static async Task InitializeServicesAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var finhubBaseUrl = configuration.GetValue("FinHub:Url", string.Empty);
        var resourceBaseUrl = new Uri(new Uri(finhubBaseUrl), "/market/index/");
        await StaticDataCacher.CacheIndexConstituentsFinHubAsync(serviceProvider, resourceBaseUrl.AbsoluteUri);
    }
}
