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
        logger.LogInformation("Loading external services settings...");
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.{EXTERNAL_SERVICES_SETTINGS_FILE}";
        var availableResources = assembly.GetManifestResourceNames();
        if (Array.IndexOf(availableResources, resourceName) == -1)
        {
            throw new FileNotFoundException($"External services settings resource '{resourceName}' not found in assembly resources.");
        }
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            var externalServicesSettings = new ConfigurationBuilder()
                .AddJsonStream(stream!)
                .AddEnvironmentVariables()
                .Build();
            ConfigureExternalServices(appBuilder, externalServicesSettings);
        }
    }

    private static void ConfigureExternalServices(WebApplicationBuilder appBuilder, IConfiguration externalServicesSettings)
    {
        var finhubBaseUrl = externalServicesSettings.GetValue<string>("FinHub:Url");
        appBuilder.Services.AddSingleton<IFinHubClient, FinHubClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FinHubClient>>();
            var httpClient = sp.GetRequiredService<HttpClient>();
            return new FinHubClient(logger, httpClient, finhubBaseUrl ?? string.Empty);
        });
    }
}
