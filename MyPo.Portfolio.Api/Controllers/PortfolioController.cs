using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.PubSub;
using MyPo.Shared.Api.Services;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class PortfolioController : MyPoBaseController
{
    private readonly IPortfolioPlanHoldingsService PortfolioPlanHoldingsService;
    private readonly IPortfolioDeletionService PortfolioDeletionService;

    public PortfolioController(
        IIdentityRepository identityRepository,
        IOptions<IdentityOptions> identityOptions,
        IAuthenticator? authenticator,
        IAuthenticatorAsync? authenticatorAsync,
        IPortfolioRepository portfolioRepository,
        IPortfolioPlanHoldingsService portfolioPlanHoldingsService,
        IPortfolioDeletionService portfolioDeletionService,
        ILogger<PortfolioController>? logger = null
    ) : base(identityRepository, identityOptions, authenticator, authenticatorAsync, portfolioRepository, logger)
    {
        ArgumentNullException.ThrowIfNull(portfolioPlanHoldingsService, nameof(portfolioPlanHoldingsService));
        ArgumentNullException.ThrowIfNull(portfolioDeletionService, nameof(portfolioDeletionService));
        PortfolioPlanHoldingsService = portfolioPlanHoldingsService;
        PortfolioDeletionService = portfolioDeletionService;
    }
}
