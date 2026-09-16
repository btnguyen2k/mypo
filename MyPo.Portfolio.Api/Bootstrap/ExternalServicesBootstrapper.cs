using Finhub.Client;
using MyPo.Portfolio.Api.Services;
using MyPo.Shared.Bootstrap;
using System.Reflection;

namespace MyPo.Portfolio.Api.Bootstrap;

[Bootstrapper]
public class ExternalServicesBootstrapper
{
    private const string EXTERNAL_SERVICES_SETTINGS_FILE = "Resources.ext_services_settings.json";
    private static readonly ILogger<ExternalServicesBootstrapper> logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ExternalServicesBootstrapper>();

    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.{EXTERNAL_SERVICES_SETTINGS_FILE}";
        logger.LogInformation("Loading '{}'...", resourceName);
        var availableResources = assembly.GetManifestResourceNames();
        if (Array.IndexOf(availableResources, resourceName) == -1)
        {
            throw new FileNotFoundException($"'{resourceName}' not found in assembly resources.");
        }
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            var externalServicesSettings = new ConfigurationBuilder()
                .AddJsonStream(stream!)
                .AddEnvironmentVariables()
                .Build();

            appBuilder.Configuration.AddConfiguration(externalServicesSettings);
            ConfigureExternalServices(appBuilder, externalServicesSettings);
        }
    }

    private static void ConfigureExternalServices(WebApplicationBuilder appBuilder, IConfiguration externalServicesSettings)
    {
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
