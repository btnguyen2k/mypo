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
public partial class PortfolioController : ApiBaseController
{
	private readonly IAuthenticator? Authenticator;
	private readonly IAuthenticatorAsync? AuthenticatorAsync;
	private readonly IIdentityRepository IdentityRepository;
	private readonly IdentityOptions IdentityOptions;
	private readonly IPortfolioRepository PortfolioRepository;

	public PortfolioController(
		IIdentityRepository identityRepository,
		IOptions<IdentityOptions> identityOptions,
		IAuthenticator? authenticator,
		IAuthenticatorAsync? authenticatorAsync,
		IPortfolioRepository portfolioRepository)
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

	private async ValueTask<(ActionResult?, MyPoUser)> VerifyAuthTokenAndCurrentUser()
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

	private async ValueTask<PortfolioEntity?> GetPortfolioIfOwnedByUser(MyPoUser user, string portfolioId)
	{
		var portfolioRec = await PortfolioRepository.GetPortfolioByIdAsync(portfolioId);
		return portfolioRec != null && portfolioRec.OwnerUserId.Equals(user.Id, StringComparison.OrdinalIgnoreCase)
			? portfolioRec
			: null;
	}

	private async ValueTask<PortfolioEntity?> GetPortfolioIfAccessible(MyPoUser user, string portfolioId)
	{
		var portfolioRec = await PortfolioRepository.GetPortfolioByIdAsync(portfolioId);
		return portfolioRec != null
			&& (portfolioRec.OwnerUserId.Equals(user.Id, StringComparison.OrdinalIgnoreCase)
				|| (portfolioRec.Metadata?.Viewers?.Contains(user.Email, StringComparer.OrdinalIgnoreCase)??false))
			? portfolioRec
			: null;
	}

	private async ValueTask<PortfolioPlanEntity?> GetPortfolioPlanIfOwnedByUser(MyPoUser user, string portfolioId)
	{
		var portfolioPlanRec = await PortfolioRepository.GetPortfolioPlanByIdAsync(portfolioId);
		return portfolioPlanRec != null && portfolioPlanRec.OwnerUserId.Equals(user.Id, StringComparison.OrdinalIgnoreCase)
			? portfolioPlanRec
			: null;
	}
}
