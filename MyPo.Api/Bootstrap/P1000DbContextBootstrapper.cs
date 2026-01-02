using MyPo.Shared.Api.Helpers;
using MyPo.Shared.Bootstrap;
using MyPo.Shared.EF.Identity;
using MyPo.Shared.Identity;

namespace MyPo.Api.Bootstrap;

/// <summary>
/// Built-in bootstrapper that initializes DbContext/DbContextPool services.
/// </summary>
[Bootstrapper]
public class DbContextBootstrapper
{
	public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<DbContextBootstrapper>();

		const string confKey = "Databases:Identity";
		logger.LogInformation("Configuring DbContext service {confKey}...", confKey);
		DbBootstrapHelper.ConfigureDbContext<IIdentityRepository, IdentityDbContextRepository>(appBuilder, confKey, logger);
		appBuilder.Services.AddHostedService<IdentityInitializer>();
	}
}
