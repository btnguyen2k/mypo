using Finhub.Client;
using MyPo.Portfolio.Api.Services;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

[Bootstrapper]
public class ExternalServicesBootstrapper
{
    private static readonly ILogger<ExternalServicesBootstrapper> logger
        = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ExternalServicesBootstrapper>();

    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
    {
        logger.LogInformation("Configuring FinHub API services...");

        var externalServicesSettings = appBuilder.Configuration;
        var finhubBaseUrl = externalServicesSettings.GetValue<string>("FinHub:Url");
        if (!Uri.TryCreate(finhubBaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"Invalid FinHub base URL: '{finhubBaseUrl}'");
        }
        appBuilder.Services.AddSingleton<IFinHubClient, FinHubClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var logger = sp.GetService<ILogger<FinHubClient>>();
            return new FinHubClient(httpClient, baseUrl: finhubBaseUrl??string.Empty, logger: logger);
        });
    }
}
