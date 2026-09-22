using FinHub.Client.Schemas.PortfolioSpotlight;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Api.Utils;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class FinHubController
{
    /// <summary>
    /// Handler for spotlighting the specified portfolio plan using the FinHub API.
    /// </summary>
    /// <param name="id">The ID of the portfolio plan to spotlight.</param>
    /// <returns>The spotlight result of the portfolio plan.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO)]
    public async ValueTask<ActionResult<PortfolioSpotlightResponse>> SpotlightPortfolioPlan([FromRoute(Name = "id")] string planId)
    {
        var (errorResult, _, portfolioPlan, market) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 1: make API call
        Logger?.LogInformation(
            "Calling FinHub SpotlightPortfolio API for portfolio plan '{planId}: {planName}' (market: {market}) with {numHoldings} holdings...",
            portfolioPlan.Id, portfolioPlan.Name,
            market?.Code ?? "N/A",
            portfolioPlan.Metadata?.HoldingTickers?.Count ?? 0);
        var req = new PortfolioSpotlightRequest
        {
            Country = market?.Country ?? "US",
            InvestorTheme = portfolioPlan.Metadata?.Description ?? string.Empty,
            CurrentAllocation = FinHubHelper.BuildAllocationReqs(portfolioPlan),
        };
        var finhubResult = await FinHubClient.SpotlightPortfolioAsync(req);
        if (!finhubResult.IsSuccess || finhubResult.Data is null)
        {
            return ResponseNoData(
                finhubResult.Status,
                finhubResult.Message ?? $"Failed to spotlight portfolio plan '{portfolioPlan.Id}: {portfolioPlan.Name}'",
                finhubResult.Extra);
        }
        var result = finhubResult.Data;

        // Step 2: save spotlight result to portfolio plan's metadata
        {
            portfolioPlan.Metadata ??= new();
            portfolioPlan.Metadata.LastChecksumAnalysis = portfolioPlan.Metadata.CalcChecksumAnalysis();
            portfolioPlan.Metadata.SpotlightRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            portfolioPlan.Metadata.SpotlightAnalysis = result;
            Logger?.LogInformation("Persisting spotlight for portfolio '{id}: {name}'...", portfolioPlan.Id, portfolioPlan.Name);
            var dbresult = await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
            if (dbresult is null)
            {
                Logger?.LogError("Failed to persist spotlight for portfolio '{id}: {name}'.", portfolioPlan.Id, portfolioPlan.Name);
            }
        }

        // Step 3: return result
        return ResponseOk(result);
    }

    /// <summary>
    /// Handler to start spotlighting a portfolio plan asynchronously using the FinHub API.
    /// </summary>
    /// <param name="id">The ID of the portfolio plan to spotlight.</param>
    /// <returns>An asynchronous response indicating the status of the analysis task.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_START_SPOTLIGHT_PORTFOLIO)]
    public async ValueTask<ActionResult<PortfolioSpotlightAsyncResponse>> StartSpotlightPortfolioPlanAsync([FromRoute(Name = "id")] string planId)
    {
        var (errorResult, _, portfolioPlan, market) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 1: make API call
        Logger?.LogInformation(
            "Calling FinHub StartSpotlightPortfolio API for portfolio plan '{planId}: {planName}' (market: {market}) with {numHoldings} holdings...",
            portfolioPlan.Id, portfolioPlan.Name,
            market?.Code ?? "N/A",
            portfolioPlan.Metadata?.HoldingTickers?.Count ?? 0);
        var req = new PortfolioSpotlightRequest
        {
            Country = market?.Country ?? "US",
            InvestorTheme = portfolioPlan.Metadata?.Description ?? string.Empty,
            CurrentAllocation = FinHubHelper.BuildAllocationReqs(portfolioPlan),
        };
        var finhubResult = await FinHubClient.StartSpotlightPortfolioAsync(req);
        if (!finhubResult.IsSuccess || finhubResult.Extra is null || string.IsNullOrEmpty(finhubResult.Extra.TaskId))
        {
            return ResponseNoData(
                finhubResult.Status,
                finhubResult.Message ?? $"Failed to start spotlighting portfolio plan '{portfolioPlan.Id}: {portfolioPlan.Name}'",
                finhubResult.Extra);
        }

        // Step 2: return result
        return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
    }

    /// <summary>
    /// Handler for polling the spotlight status of a portfolio plan.
    /// </summary>
    /// <param name="taskId">The task ID of the portfolio spotlight to poll.</param>
    /// <returns>The current status and result of the portfolio spotlight task.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_POLL_SPOTLIGHT_PORTFOLIO)]
    public async ValueTask<ActionResult<PortfolioSpotlightAsyncResponse>> PollSpotlightPortfolioPlanAsync([FromQuery(Name = "task_id")] string taskId)
    {
        // Step 1: make API call
        Logger?.LogInformation("Calling FinHub PollSpotlightPortfolio API for task '{taskId}'...", taskId);
        var finhubResult = await FinHubClient.PollSpotlightPortfolioAsync(taskId);
        if (!finhubResult.IsSuccess || finhubResult.Extra is null || string.IsNullOrEmpty(finhubResult.Extra.TaskId))
        {
            return ResponseNoData(
                finhubResult.Status,
                finhubResult.Message ?? $"Failed to poll spotlighting portfolio plan for task '{taskId}'",
                finhubResult.Extra);
        }

        // Step 2: return result
        return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
    }
}
