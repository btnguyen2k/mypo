using Finhub.Client;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Helpers;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Bootstrapper that configures and registers external services, such as the FinHub API client.
/// </summary>
[Bootstrapper]
public class ExternalServicesBootstrapper
{
    private static readonly ILogger<ExternalServicesBootstrapper> logger
        = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ExternalServicesBootstrapper>();

    private static Uri FinHubBaseUrl = default!;

    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
    {
        ConfigureFinHub(appBuilder.Configuration, appBuilder.Services);
        appBuilder.Services.AddHostedService<BackgroundTaskCacheIndexConstituentsFinHub>();
    }

    private static void ConfigureFinHub(IConfiguration configuration, IServiceCollection services)
    {
        logger.LogInformation("Configuring FinHub API services...");

        var finhubBaseUrl = configuration.GetValue("FinHub:Url", string.Empty);
        if (string.IsNullOrEmpty(finhubBaseUrl) || !Uri.TryCreate(finhubBaseUrl, UriKind.Absolute, out var finhubUri))
        {
            throw new InvalidOperationException($"Invalid FinHub base URL: '{finhubBaseUrl}'");
        }
        FinHubBaseUrl = finhubUri;

        services.AddSingleton<IFinHubClient, FinHubClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var logger = sp.GetService<ILogger<FinHubClient>>();
            return new FinHubClient(httpClient, baseUrl: finhubBaseUrl, logger: logger);
        });
    }

    sealed class BackgroundTaskCacheIndexConstituentsFinHub : BackgroundService
    {
        private readonly IServiceProvider ServiceProvider;

        public BackgroundTaskCacheIndexConstituentsFinHub(IServiceProvider serviceProvider) : base()
        {
            ServiceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var resourceBaseUrl = new Uri(FinHubBaseUrl, "/market/index/");
            await StaticDataCacher.CacheIndexConstituentsFinHubAsync(
                ServiceProvider,
                resourceBaseUrl.AbsoluteUri,
                cancellationToken);
        }
    }
}
