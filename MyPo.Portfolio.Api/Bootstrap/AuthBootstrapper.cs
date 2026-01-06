using MyPo.Portfolio.Shared.Identity;
using MyPo.Shared.Bootstrap;

namespace MyPo.Portfolio.Api.Bootstrap;

[Bootstrapper]
public class AuthBootstrapper
{
	public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		// Configurate authorization policies
		appBuilder.Services.AddAuthorization(c =>
		{
			c.AddPolicy(PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER, PortfolioPolicies.POLICY_ADMIN_ROLE_OR_PORTFOLIO_MANAGER);
		});
	}
}
