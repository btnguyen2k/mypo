using FinHub.Client.Schemas.PortfolioAnalysis;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Api.Utils;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Api.Controllers;

public partial class FinHubController
{
    /// <summary>
    /// Handler for analyzing the specified portfolio plan using the FinHub API.
    /// </summary>
    /// <param name="id">The ID of the portfolio plan to analyze.</param>
    /// <returns>The analysis result of the portfolio plan.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_ANALYZE_PORTFOLIO)]
    public async ValueTask<ActionResult<AnalyzePortfolioResponse>> AnalyzePortfolioPlan([FromRoute(Name = "id")] string planId)
    {
        var (errorResult, _, portfolioPlan, market) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 1: make API call
        Logger?.LogInformation(
            "Calling FinHub AnalyzePortfolio API for portfolio plan '{planId}: {planName}' (market: {market}) with {numHoldings} holdings...",
            portfolioPlan.Id, portfolioPlan.Name,
            market?.Code ?? "N/A",
            portfolioPlan.Metadata?.HoldingTickers?.Count ?? 0);
        var req = new AnalyzePortfolioRequest
        {
            Country = market?.Country ?? "US",
            InvestorTheme = portfolioPlan.Metadata?.Description ?? string.Empty,
            CurrentAllocation = FinHubHelper.BuildAllocationReqs(portfolioPlan),
            RebalancePlan = portfolioPlan.Type == PortfolioPlanEntity.PLAN_TYPE_ALLOCATION,
        };
        var finhubResult = await FinHubClient.AnalyzePortfolioAsync(req);
        if (!finhubResult.IsSuccess || finhubResult.Data is null)
        {
            return ResponseNoData(
                finhubResult.Status,
                finhubResult.Message ?? $"Failed to analyze portfolio plan '{portfolioPlan.Id}: {portfolioPlan.Name}'",
                finhubResult.Extra);
        }
        var result = finhubResult.Data;

        // Step 2: save analysis result to portfolio plan's metadata
        {
            portfolioPlan.Metadata ??= new();
            portfolioPlan.Metadata.LastChecksumAnalysis = portfolioPlan.Metadata.CalcChecksumAnalysis();
            portfolioPlan.Metadata.AnalysisRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            portfolioPlan.Metadata.PortfolioAnalysis = result;
            Logger?.LogInformation("Persisting analysis for portfolio '{id}: {name}'...", portfolioPlan.Id, portfolioPlan.Name);
            var dbresult = await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
            if (dbresult is null)
            {
                Logger?.LogError("Failed to persist analysis for portfolio '{id}: {name}'.", portfolioPlan.Id, portfolioPlan.Name);
            }
        }

        // Step 3: return result
        return ResponseOk(result);
    }

    /// <summary>
    /// Handler to start analyzing a portfolio plan asynchronously using the FinHub API.
    /// </summary>
    /// <param name="id">The ID of the portfolio plan to analyze.</param>
    /// <returns>An asynchronous response indicating the status of the analysis task.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_START_ANALYZE_PORTFOLIO)]
    public async ValueTask<ActionResult<AnalyzePortfolioAsyncResponse>> StartAnalyzePortfolioPlanAsync([FromRoute(Name = "id")] string planId)
    {
        var (errorResult, _, portfolioPlan, market) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 1: make API call
        Logger?.LogInformation(
            "Calling FinHub StartAnalyzePortfolio API for portfolio plan '{planId}: {planName}' (market: {market}) with {numHoldings} holdings...",
            portfolioPlan.Id, portfolioPlan.Name,
            market?.Code ?? "N/A",
            portfolioPlan.Metadata?.HoldingTickers?.Count ?? 0);
        var req = new AnalyzePortfolioRequest
        {
            Country = market?.Country ?? "US",
            InvestorTheme = portfolioPlan.Metadata?.Description ?? string.Empty,
            CurrentAllocation = FinHubHelper.BuildAllocationReqs(portfolioPlan),
            RebalancePlan = portfolioPlan.Type == PortfolioPlanEntity.PLAN_TYPE_ALLOCATION,
        };
        var finhubResult = await FinHubClient.StartAnalyzePortfolioAsync(req);
        if (!finhubResult.IsSuccess || finhubResult.Extra is null || string.IsNullOrEmpty(finhubResult.Extra.TaskId))
        {
            return ResponseNoData(
                finhubResult.Status,
                finhubResult.Message ?? $"Failed to start analyzing portfolio plan '{portfolioPlan.Id}: {portfolioPlan.Name}'",
                finhubResult.Extra);
        }

        // Step 2: return result
        return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
    }

    /// <summary>
    /// Handler for polling the analysis status of a portfolio plan.
    /// </summary>
    /// <param name="taskId">The task ID of the portfolio analysis to poll.</param>
    /// <returns>The current status and result of the portfolio analysis task.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_POLL_ANALYZE_PORTFOLIO)]
    public async ValueTask<ActionResult<AnalyzePortfolioAsyncResponse>> PollAnalyzePortfolioPlanAsync([FromQuery(Name = "task_id")] string taskId)
    {
        // Step 1: make API call
        Logger?.LogInformation("Calling FinHub PollAnalyzePortfolio API for task '{taskId}'...", taskId);
        var finhubResult = await FinHubClient.PollAnalyzePortfolioAsync(taskId);
        if (!finhubResult.IsSuccess || finhubResult.Extra is null || string.IsNullOrEmpty(finhubResult.Extra.TaskId))
        {
            return ResponseNoData(
                finhubResult.Status,
                finhubResult.Message ?? $"Failed to poll analyzing portfolio plan for task '{taskId}'",
                finhubResult.Extra);
        }

        // Step 2: return result
        return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
    }
}
