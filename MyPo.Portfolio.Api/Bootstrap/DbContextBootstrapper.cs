using MyPo.Portfolio.Shared.EF;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api.Helpers;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

/// <summary>
/// Bootstrapper that initializes DbContext/DbContextPool services.
/// </summary>
[Bootstrapper]
public class DbContextBootstrapper
{
    public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
    {
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<DbContextBootstrapper>();

        const string confKey = "Databases:Portfolio";
        logger.LogInformation("Configuring DbContext service {confKey}...", confKey);
        DbBootstrapHelper.ConfigureDbContext<IPortfolioRepository, PortfolioDbContextRepository>(appBuilder, confKey, logger);
        appBuilder.Services.AddHostedService<PortfolioInitializer>();
    }
}
