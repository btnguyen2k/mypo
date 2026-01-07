using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

	public PortfolioController(
		IIdentityRepository identityRepository,
		IOptions<IdentityOptions> identityOptions,
		IAuthenticator? authenticator,
		IAuthenticatorAsync? authenticatorAsync)
	{
		ArgumentNullException.ThrowIfNull(identityRepository, nameof(identityRepository));
		ArgumentNullException.ThrowIfNull(identityOptions, nameof(identityOptions));
		if (authenticator == null && authenticatorAsync == null)
		{
			throw new ArgumentNullException("No authenticator defined.");
		}

		IdentityRepository = identityRepository;
		IdentityOptions = identityOptions.Value;
		Authenticator = authenticator;
		AuthenticatorAsync = authenticatorAsync;
	}

	private async Task<(ActionResult?, MyPoUser)> VerifyAuthTokenAndCurrentUser()
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
}
