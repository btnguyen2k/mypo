using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;
using MyPo.Shared.Identity;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
    /// <summary>
    /// Gets current user's portfolio plan records.
    /// </summary>
    /// <returns></returns>
    /// <remarks>The user is identified by the auth token.</remarks>
    [HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS)]
    public async ValueTask<ActionResult<ApiResp<List<PortfolioPlanResp>>>> GetMyPortfolioPlans()
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var result = new List<PortfolioPlanResp>();
        var myPortfolioPlanList = await PortfolioRepository.GetPortfolioPlansAccessibleByUserAsync(currentUser);
        foreach (var plan in myPortfolioPlanList)
        {
            var planResp = PortfolioPlanResp.BuildFrom(plan);
            var portfolio = plan.PortfolioId != null ? await PortfolioRepository.GetPortfolioByIdAsync(plan.PortfolioId) : null;
            planResp.Portfolio = portfolio != null ? PortfolioResp.BuildFrom(portfolio) : null;
            result.Add(planResp);
        }
        return ResponseOk(result);
    }

    /// <summary>
    /// Gets a portfolio plan record by ID. The portfolio plan must be owned by the current user.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <remarks>The user is identified by the auth token.</remarks>
    [HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID)]
    public async ValueTask<ActionResult<ApiResp<PortfolioPlanResp>>> GetMyPortfolioPlanById([FromRoute] string id)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var portfolioPlan = await GetPortfolioPlanIfAccessible(currentUser, id);
        if (portfolioPlan == null)
        {
            return ResponseNoData(404, "Portfolio plan not found.");
        }
        var planResp = PortfolioPlanResp.BuildFrom(portfolioPlan);
        var portfolio = portfolioPlan.PortfolioId != null ? await PortfolioRepository.GetPortfolioByIdAsync(portfolioPlan.PortfolioId) : null;
        planResp.Portfolio = portfolio != null ? PortfolioResp.BuildFrom(portfolio) : null;
        return ResponseOk(planResp);
    }

    private async ValueTask<ObjectResult?> ValidateCreateOrUpdatePortfolioPlanReq(CreateOrUpdatePortfolioPlanReq req, MyPoUser user)
    {
        // validate linked portfolio
        if (!string.IsNullOrWhiteSpace(req.PortfolioId))
        {
            var portfolio = await PortfolioRepository.GetPortfolioByIdAsync(req.PortfolioId);
            if (portfolio == null || !portfolio.OwnerUserId.Equals(user.Id, StringComparison.OrdinalIgnoreCase))
            {
                return ResponseNoData(400, "Invalid linked portfolio ID.");
            }
        }

        // validate name
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return ResponseNoData(400, "Name is required.");
        }

        // validate type
        if (!PortfolioPlanEntity.ValidPlanTypes.Contains(req.Type))
        {
            return ResponseNoData(400, $"Invalid plan type '{req.Type}'.");
        }

        // validate tickers
        if (req.Metadata != null && req.Metadata.HoldingTickers != null)
        {
            foreach (var ticker in req.Metadata.HoldingTickers)
            {
                if (string.IsNullOrWhiteSpace(ticker.Ticker))
                {
                    return ResponseNoData(400, "Ticker is required for all holding tickers.");
                }
                if (ticker.TargetAllocation < 0)
                {
                    return ResponseNoData(400, "Target allocation must be >= 0 for all holding tickers.");
                }
            }
            // var totalAllocation = req.Metadata.HoldingTickers.Sum(ht => ht.TargetAllocation);
            // if (totalAllocation != 100)
            // {
            // 	return ResponseNoData(400, "Total allocation must be 100% for all holding tickers.");
            // }
        }

        return null;
    }

    private async ValueTask<(ObjectResult?, IList<HoldingTicker>?)> BuildHoldingTickers(CreateOrUpdatePortfolioPlanReq req)
    {
        var inputTickers = req.Metadata?.HoldingTickers ?? [];
        var result = await PortfolioPlanHoldingsService.RefreshHoldingsAsync(inputTickers, req.PortfolioId);
        if (result.FailedTickers.Count > 0)
        {
            return (ResponseNoData(400, $"Cannot fetch info for ticker '{result.FailedTickers[0]}'."), null);
        }
        return (null, result.Holdings);
    }

    /// <summary>
    /// Creates a new portfolio plan record.
    /// </summary>
    /// <param name="req">The create portfolio plan request.</param>
    /// <returns>The created portfolio plan record.</returns>
    /// <remarks>The user is identified by the auth token.</remarks>
    [HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_PORTFOLIO_PLANS)]
    [Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
    public async ValueTask<ActionResult<ApiResp<PortfolioPlanResp>>> CreatePortfolioPlan([FromBody] CreateOrUpdatePortfolioPlanReq req)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var validationResult = await ValidateCreateOrUpdatePortfolioPlanReq(req, currentUser);
        if (validationResult != null)
        {
            return validationResult;
        }

        IList<HoldingTicker>? holdings;
        (validationResult, holdings) = await BuildHoldingTickers(req);
        if (validationResult != null)
        {
            return validationResult;
        }

        // Create new portfolio plan record
        var portfolioPlanRec = new PortfolioPlanEntity
        {
            // Id = Guid.NewGuid().ToString(),
            Type = req.Type,
            OwnerUserId = currentUser.Id,
            PortfolioId = string.IsNullOrWhiteSpace(req.PortfolioId) ? null : req.PortfolioId.Trim(),
            Name = req.Name.Trim(),
            Metadata = new()
            {
                HoldingsRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                HoldingTickers = holdings ?? [],
                Description = req.Metadata?.Description?.Trim() ?? string.Empty,
            },
        };
        var plan = await PortfolioRepository.CreatePortfolioPlanAsync(portfolioPlanRec);
        if (plan == null)
        {
            return ResponseNoData(500, "Failed to create new portfolio plan.");
        }
        var planResp = PortfolioPlanResp.BuildFrom(plan);
        var portfolio = plan.PortfolioId != null ? await PortfolioRepository.GetPortfolioByIdAsync(plan.PortfolioId) : null;
        planResp.Portfolio = portfolio != null ? PortfolioResp.BuildFrom(portfolio) : null;
        return ResponseOk(planResp);
    }

    /// <summary>
    /// Updates an existing portfolio plan record. The portfolio plan must be owned by the current user.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="req"></param>
    /// <returns></returns>
    /// <remarks>The user is identified by the auth token.</remarks>
    [HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID)]
    [Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
    public async ValueTask<ActionResult<ApiResp<PortfolioPlanResp>>> UpdateMyPortfolioPlan([FromRoute] string id, [FromBody] CreateOrUpdatePortfolioPlanReq req)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var validationResult = await ValidateCreateOrUpdatePortfolioPlanReq(req, currentUser);
        if (validationResult != null)
        {
            return validationResult;
        }

        IList<HoldingTicker>? holdings;
        (validationResult, holdings) = await BuildHoldingTickers(req);
        if (validationResult != null)
        {
            return validationResult;
        }

        var existingPortfolioPlan = await GetPortfolioPlanIfOwnedByUser(currentUser, id);
        if (existingPortfolioPlan == null)
        {
            return ResponseNoData(404, "Portfolio plan not found.");
        }

        existingPortfolioPlan.Type = req.Type;
        existingPortfolioPlan.PortfolioId = string.IsNullOrWhiteSpace(req.PortfolioId) ? null : req.PortfolioId.Trim();
        existingPortfolioPlan.Name = req.Name.Trim();
        existingPortfolioPlan.Metadata ??= new PortfolioPlanMetadata();
        existingPortfolioPlan.Metadata.HoldingsRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        existingPortfolioPlan.Metadata.HoldingTickers = holdings ?? [];
        existingPortfolioPlan.Metadata.Description = req.Metadata?.Description?.Trim() ?? string.Empty;

        existingPortfolioPlan = await PortfolioRepository.UpdatePortfolioPlanAsync(existingPortfolioPlan);
        if (existingPortfolioPlan == null)
        {
            return ResponseNoData(500, $"Failed to update portfolio plan '{id}'.");
        }
        var planResp = PortfolioPlanResp.BuildFrom(existingPortfolioPlan);
        var portfolio = existingPortfolioPlan.PortfolioId != null ? await PortfolioRepository.GetPortfolioByIdAsync(existingPortfolioPlan.PortfolioId) : null;
        planResp.Portfolio = portfolio != null ? PortfolioResp.BuildFrom(portfolio) : null;
        return ResponseOk(planResp);
    }

    /// <summary>
    /// Deletes an existing portfolio plan record. The portfolio plan must be owned by the current user.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <remarks>The user is identified by the auth token.</remarks>
    [HttpDelete(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS_ID)]
    [Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
    public async ValueTask<ActionResult<ApiResp<PortfolioPlanResp>>> DeleteMyPortfolioPlan([FromRoute] string id)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var existingPortfolioPlan = await GetPortfolioPlanIfOwnedByUser(currentUser, id);
        if (existingPortfolioPlan == null)
        {
            return ResponseNoData(404, "Portfolio plan not found.");
        }

        var resultDelete = await PortfolioRepository.DeletePortfolioPlanAsync(existingPortfolioPlan);
        if (!resultDelete)
        {
            return ResponseNoData(500, $"Failed to delete portfolio plan '{id}'.");
        }
        return ResponseOk(PortfolioPlanResp.BuildFrom(existingPortfolioPlan));
    }
}
