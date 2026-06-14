using MyPo.Portfolio.Shared.Identity;
using MyPo.Shared.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

namespace MyPo.Blazor.Demo.App.Bootstrap;

[Bootstrapper]
public class AuthBootstrapper
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // set up authorization
        services.AddAuthorizationCore(c =>
        {
            // Configurate authorization policies
            c.AddPolicy(PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER, PortfolioPolicies.POLICY_ADMIN_ROLE_OR_PORTFOLIO_MANAGER);
        });
    }
}
