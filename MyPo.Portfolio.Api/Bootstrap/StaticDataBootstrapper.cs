using MyPo.Portfolio.Shared.Helpers;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Loads index constituent data from FinHub into the server-side cache.
/// </summary>
[Bootstrapper]
public class StaticDataBootstrapper
{
    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
    {
        // events
        appBuilder.Services.AddHostedService<BackgroundTaskCacheIndexConstituentsFinHub>();
    }
}

sealed class BackgroundTaskCacheIndexConstituentsFinHub : BackgroundService
{
    private readonly IServiceProvider ServiceProvider;
    private readonly IConfiguration Configuration;

    public BackgroundTaskCacheIndexConstituentsFinHub(
        IServiceProvider serviceProvider,
        IConfiguration configuration) : base()
    {
        ServiceProvider = serviceProvider;
        Configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var finhubBaseUrl = Configuration.GetValue("FinHub:Url", string.Empty);
        if (!Uri.TryCreate(finhubBaseUrl, UriKind.Absolute, out var finhubUri))
        {
            throw new InvalidOperationException($"Invalid FinHub base URL: '{finhubBaseUrl}'");
        }
        var resourceBaseUrl = new Uri(finhubUri, "/market/index/");
        await StaticDataCacher.CacheIndexConstituentsFinHubAsync(ServiceProvider, resourceBaseUrl.AbsoluteUri, cancellationToken);
    }
}
