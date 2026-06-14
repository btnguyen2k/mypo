using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api.Controller;
using MyPo.Shared.Api.Services;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public abstract class MyPoBaseController : ApiBaseController
{
    protected readonly IAuthenticator? Authenticator;
    protected readonly IAuthenticatorAsync? AuthenticatorAsync;
    protected readonly IIdentityRepository IdentityRepository;
    protected readonly IdentityOptions IdentityOptions;
    protected readonly IPortfolioRepository PortfolioRepository;

    public MyPoBaseController(
        IIdentityRepository identityRepository,
        IOptions<IdentityOptions> identityOptions,
        IAuthenticator? authenticator,
        IAuthenticatorAsync? authenticatorAsync,
        IPortfolioRepository portfolioRepository
    )
    {
        ArgumentNullException.ThrowIfNull(identityRepository, nameof(identityRepository));
        ArgumentNullException.ThrowIfNull(identityOptions, nameof(identityOptions));
        if (authenticator == null && authenticatorAsync == null)
        {
            throw new ArgumentNullException("No authenticator defined.");
        }
        ArgumentNullException.ThrowIfNull(portfolioRepository, nameof(portfolioRepository));

        IdentityRepository = identityRepository;
        IdentityOptions = identityOptions.Value;
        Authenticator = authenticator;
        AuthenticatorAsync = authenticatorAsync;
        PortfolioRepository = portfolioRepository;
    }

    protected async ValueTask<(ActionResult?, MyPoUser)> VerifyAuthTokenAndCurrentUser()
    {
        var jwtToken = GetAuthToken();
        var tokenValidationResult = await ValidateAuthTokenAsync(Authenticator, AuthenticatorAsync, jwtToken);
        if (tokenValidationResult.Status != 200)
        {
            // the auth token should still be valid
            return (ResponseNoData(403, tokenValidationResult.Error), null!);
        }

        var currentUser = await GetCurrentUserAsync(IdentityOptions, IdentityRepository);
        if (currentUser == null)
        {
            // should not happen
            return (_respAuthenticationRequired, null!);
        }

        return (null, currentUser);
    }

    protected async ValueTask<PortfolioEntity?> GetPortfolioIfOwnedByUser(MyPoUser user, string portfolioId)
    {
        var portfolio = await PortfolioRepository.GetPortfolioByIdAsync(portfolioId);
        return portfolio != null && portfolio.OwnerUserId.Equals(user.Id, StringComparison.OrdinalIgnoreCase)
            ? portfolio
            : null;
    }

    protected async ValueTask<PortfolioPlanEntity?> GetPortfolioPlanIfOwnedByUser(MyPoUser user, string planId)
    {
        var plan = await PortfolioRepository.GetPortfolioPlanByIdAsync(planId);
        return plan != null && plan.OwnerUserId.Equals(user.Id, StringComparison.OrdinalIgnoreCase)
            ? plan
            : null;
    }

    protected async ValueTask<IEnumerable<AssetEntity>> GetOwningAssets(string portfolioId)
    {
        return await PortfolioRepository.GetAssetsByPortfolioIdAsync(portfolioId);
    }
}
