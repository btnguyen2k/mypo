using MyPo.Libs.Clavis;
using MyPo.Portfolio.Api.Services;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

[Bootstrapper]
public class ServicesBootstrapper
{
    private static readonly ILogger<ServicesBootstrapper> logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ServicesBootstrapper>();

    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
    {
        ConfigureClavis(appBuilder.Services);
        appBuilder.Services.AddScoped<IPortfolioPlanHoldingsService, PortfolioPlanHoldingsService>();
    }

    private static void ConfigureClavis(IServiceCollection services)
    {
        logger.LogInformation("Configuring Clavis service...");

        var key = Environment.GetEnvironmentVariable("CLAVIS_KEY") ?? null;
        if (string.IsNullOrEmpty(key))
        {
            logger.LogWarning("Clavis key not found in environment variables. A random key will be created!");
            key = Guid.NewGuid().ToString();
        }
        if (key.Length < 32)
        {
            throw new InvalidOperationException("Clavis key must be at least 32 characters long. Please provide a valid key in the CLAVIS_KEY environment variable.");
        }
        services.AddSingleton<Clavis, Clavis>(sp => new Clavis(key));
    }
}
