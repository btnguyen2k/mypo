using Finhub.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Services;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class FinHubController : MyPoBaseController
{
    private readonly IFinHubClient FinHubClient;

    private readonly AsyncTaskInfo TaskCompleted = new() {TaskId = "completed-task", State = TaskState.Completed};

    public FinHubController(
        IIdentityRepository identityRepository,
        IOptions<IdentityOptions> identityOptions,
        IAuthenticator? authenticator,
        IAuthenticatorAsync? authenticatorAsync,
        IPortfolioRepository portfolioRepository,
        IFinHubClient finHubClient,
        ILogger<FinHubController>? logger = null
    ) : base(identityRepository, identityOptions, authenticator, authenticatorAsync, portfolioRepository, logger)
    {
        ArgumentNullException.ThrowIfNull(finHubClient, nameof(finHubClient));
        FinHubClient = finHubClient;
    }

    private async ValueTask<(ActionResult?, MyPoUser, PortfolioPlanEntity, MarketDef?)> ValidatePortfolioPlan(
        string planId,
        bool isOwner = false)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return (authErrorResult, currentUser, null!, null);
        }

        var portfolioPlan = isOwner
            ? await GetPortfolioPlanIfOwnedByUser(currentUser, planId)
            : await GetPortfolioPlanIfAccessible(currentUser, planId);
        if (portfolioPlan == null)
        {
            return (ResponseNoData(404, "Portfolio plan not found."), currentUser, null!, null);
        }

        var portfolio = !string.IsNullOrEmpty(portfolioPlan.PortfolioId)
            ? await PortfolioRepository.GetPortfolioByIdAsync(portfolioPlan.PortfolioId)
            : null;
        var market = Globals.MarketsMap.TryGetValue(portfolio?.Metadata?.DefaultMarketId?.ToUpper() ?? string.Empty, out var m) ? m : null;

        return (null, currentUser, portfolioPlan, market);
    }
}
