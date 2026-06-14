using System.Security.Claims;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Shared.Identity;

public sealed class PortfolioClaims
{
    /// <summary>
    /// Claim to mark a user/role as a portfolio manager.
    /// </summary>
    public static readonly Claim CLAIM_ROLE_PORTFOLIO_MANAGER = new($"{BuiltinClaims.CLAIM_PREFIX}{BuiltinClaims.ROLE_PREFIX}", "portfolio-manager");

    public static readonly IEnumerable<Claim> ALL_CLAIMS = [
        CLAIM_ROLE_PORTFOLIO_MANAGER,
    ];
}
