using System.Reflection;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Bootstrapper that loads additional application settings from the embedded .json file.
/// </summary>
/// <remarks>
/// This bootstrap needs to be executed as early as possible to ensure that the application settings are loaded before
/// other bootstrappers that depend on them.
/// </remarks>
[Bootstrapper(Priority = 0)]
public class AppSettingsBootstrapper
{
    private const string APP_SETTINGS_FILE = "Resources.appsettings.json";
    private static readonly ILogger<AppSettingsBootstrapper> logger
        = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<AppSettingsBootstrapper>();

    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.{APP_SETTINGS_FILE}";
        logger.LogInformation("Loading '{}'...", resourceName);
        var availableResources = assembly.GetManifestResourceNames();
        if (Array.IndexOf(availableResources, resourceName) == -1)
        {
            throw new FileNotFoundException($"'{resourceName}' not found in assembly resources.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        var myAppSettings = new ConfigurationBuilder()
            .AddJsonStream(stream!)
            .AddEnvironmentVariables()
            .Build();
        appBuilder.Configuration.AddConfiguration(myAppSettings);
    }
}
