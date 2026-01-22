using MyPo.Portfolio.Shared.Identity;
using MyPo.Shared.Bootstrap;
using MyPo.Shared.Global;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyPo.Blazor.Portfolio.App.Bootstrap;

[Bootstrapper(Priority = 100)]
public class InitIdentityBootstrapper
{
	public static void ConfigureServices(IServiceCollection _, ILogger<InitIdentityBootstrapper> logger)
	{
		logger.LogInformation("Registering module portfolio {count} claims...", PortfolioClaims.ALL_CLAIMS.Count());
		GlobalRegistry.ALL_CLAIMS.UnionWith(PortfolioClaims.ALL_CLAIMS);
		logger.LogInformation("Total registered claims: {count}", GlobalRegistry.ALL_CLAIMS.Count);
	}
}
