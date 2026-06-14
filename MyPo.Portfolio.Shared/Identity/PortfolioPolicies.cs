using MyPo.Shared.Identity;
using Microsoft.AspNetCore.Authorization;

namespace MyPo.Portfolio.Shared.Identity;

public sealed class PortfolioPolicies
{
    public const string POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER = "AdminRoleOrPortfolioManager";
    public static readonly AuthorizationPolicy POLICY_ADMIN_ROLE_OR_PORTFOLIO_MANAGER = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(context =>
        {
            var hasAdminRole = context.User.HasClaim(BuiltinClaims.CLAIM_ROLE_GLOBAL_ADMIN.Type, BuiltinClaims.CLAIM_ROLE_GLOBAL_ADMIN.Value);
            var hasPortfolioManagerRole = context.User.HasClaim(PortfolioClaims.CLAIM_ROLE_PORTFOLIO_MANAGER.Type, PortfolioClaims.CLAIM_ROLE_PORTFOLIO_MANAGER.Value);
            return hasAdminRole || hasPortfolioManagerRole;
        })
        .Build();
}
