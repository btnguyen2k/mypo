using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api.Services;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class FinHubController : MyPoBaseController
{
    private readonly IFinHubClient FinHubClient;

    public FinHubController(
        IIdentityRepository identityRepository,
        IOptions<IdentityOptions> identityOptions,
        IAuthenticator? authenticator,
        IAuthenticatorAsync? authenticatorAsync,
        IPortfolioRepository portfolioRepository,
        IFinHubClient finHubClient,
        ILogger<FinHubController> logger
    ) : base(identityRepository, identityOptions, authenticator, authenticatorAsync, portfolioRepository, logger)
    {
        ArgumentNullException.ThrowIfNull(finHubClient, nameof(finHubClient));
        FinHubClient = finHubClient;
    }
}
