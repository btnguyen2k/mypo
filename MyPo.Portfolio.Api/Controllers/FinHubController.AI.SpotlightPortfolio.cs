using FinHub.Client.Models.Portfolios;
using FinHub.Client.Schemas.PortfolioSpotlight;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Api.Utils;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class FinHubController
{
    private static PortfolioSpotlightAnalysis? HasRecentSpotlightAnalysis(PortfolioPlanEntity portfolioPlan)
    {
        var oldChecksum = portfolioPlan.Metadata?.ChecksumLastSpotlightAnalysis ?? null;
        var curChecksum = portfolioPlan.Metadata?.CalcChecksum() ?? null;
        var lastSpotlightRefreshTimestamp = portfolioPlan.Metadata?.RefreshTimestampSpotlightAnalysis ?? 0;
        var lastSpotlightAnalysis = portfolioPlan.Metadata?.SpotlightAnalysis ?? null;
        return lastSpotlightAnalysis is null
            || oldChecksum != curChecksum
            || lastSpotlightRefreshTimestamp <= DateTimeOffset.UtcNow.AddHours(-24).ToUnixTimeSeconds()
            ? null
            : lastSpotlightAnalysis;
    }

    private static AsyncTaskInfo? HasRecentSpotlightAnalysisRequest(PortfolioPlanEntity portfolioPlan)
    {
        var oldChecksum = portfolioPlan.Metadata?.ChecksumRequestSpotlightAnalysis ?? null;
        var curChecksum = portfolioPlan.Metadata?.CalcChecksum() ?? null;
        var lastSpotlightRequestTimestamp = portfolioPlan.Metadata?.TimestampRequestSpotlightAnalysis ?? 0;
        var lastSpotlightRequest = portfolioPlan.Metadata?.RequestSpotlightAnalysis ?? null;
        return lastSpotlightRequest is null
            || oldChecksum != curChecksum
            || lastSpotlightRequestTimestamp <= DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()
            ? null
            : lastSpotlightRequest;
    }

    private static void StoreSpotlightAnalysisResult(PortfolioPlanEntity portfolioPlan, PortfolioSpotlightAnalysis result)
    {
        portfolioPlan.Metadata ??= new();
        portfolioPlan.Metadata.ChecksumLastSpotlightAnalysis = portfolioPlan.Metadata.CalcChecksum();
        portfolioPlan.Metadata.RefreshTimestampSpotlightAnalysis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        portfolioPlan.Metadata.SpotlightAnalysis = result;
    }

    /// <summary>
    /// Handler for spotlighting the specified portfolio plan using the FinHub API.
    /// </summary>
    /// <param name="id">The ID of the portfolio plan to spotlight.</param>
    /// <returns>The spotlight result of the portfolio plan.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO)]
    public async ValueTask<ActionResult<PortfolioSpotlightResponse>> SpotlightPortfolioPlan(
        [FromRoute(Name = "id")] string planId)
    {
        var (errorResult, _, portfolioPlan, market) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 0: check if the portfolio plan already has a recent spotlight analysis
        var recentSpotlightAnalysis = HasRecentSpotlightAnalysis(portfolioPlan);
        if (recentSpotlightAnalysis is not null)
        {
            Logger?.LogInformation(
                "Portfolio plan '{planId}: {planName}' already has a recent spotlight analysis.",
                portfolioPlan.Id,
                portfolioPlan.Name);
            return ResponseOk(recentSpotlightAnalysis);
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
            StoreSpotlightAnalysisResult(portfolioPlan, result);
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
    public async ValueTask<ActionResult<PortfolioSpotlightAsyncResponse>> StartSpotlightPortfolioPlanAsync(
        [FromRoute(Name = "id")] string planId)
    {
        var (errorResult, _, portfolioPlan, market) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 0a: check if the portfolio plan already has a recent spotlight analysis
        var recentSpotlightAnalysis = HasRecentSpotlightAnalysis(portfolioPlan);
        if (recentSpotlightAnalysis is not null)
        {
            Logger?.LogInformation(
                "Portfolio plan '{planId}: {planName}' already has a recent spotlight analysis.",
                portfolioPlan.Id,
                portfolioPlan.Name);
            return ResponseAsyncOk(recentSpotlightAnalysis, TaskCompleted);
        }

        // Step 0b: check if there is a recent spotlight analysis request
        var recentSpotlightAnalysisRequest = HasRecentSpotlightAnalysisRequest(portfolioPlan);
        if (recentSpotlightAnalysisRequest is not null)
        {
            Logger?.LogInformation(
                "Portfolio plan '{planId}: {planName}' already has a recent spotlight analysis request - TaskId: {taskId}",
                portfolioPlan.Id,
                portfolioPlan.Name,
                recentSpotlightAnalysisRequest.TaskId);
            var draftAnalysis = portfolioPlan.Metadata?.SpotlightAnalysis;
            return ResponseAsyncOk(draftAnalysis, recentSpotlightAnalysisRequest);
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

        // Step 2: store the task info for tracking the spotlight analysis request
        portfolioPlan.Metadata ??= new();
        portfolioPlan.Metadata.ChecksumRequestSpotlightAnalysis = portfolioPlan.Metadata.CalcChecksum();
        portfolioPlan.Metadata.TimestampRequestSpotlightAnalysis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        portfolioPlan.Metadata.RequestSpotlightAnalysis = finhubResult.Extra;
        if (finhubResult.Extra.State == TaskState.Completed && finhubResult.Data is not null)
        {
            // The spotlight analysis has completed successfully. Store the analysis result too
            StoreSpotlightAnalysisResult(portfolioPlan, finhubResult.Data);
        }
        Logger?.LogInformation("Persisting spotlight analysis request for portfolio '{id}: {name}'...", portfolioPlan.Id, portfolioPlan.Name);
        var dbresult = await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
        if (dbresult is null)
        {
            Logger?.LogError("Failed to persist spotlight analysis request for portfolio '{id}: {name}'.", portfolioPlan.Id, portfolioPlan.Name);
        }

        // Step 3: return result
        return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
    }

    /// <summary>
    /// Handler for polling the spotlight status of a portfolio plan.
    /// </summary>
    /// <param name="planId">The ID of the portfolio plan to poll.</param>
    /// <param name="taskId">The task ID of the portfolio spotlight to poll.</param>
    /// <returns>The current status and result of the portfolio spotlight task.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_POLL_SPOTLIGHT_PORTFOLIO)]
    public async ValueTask<ActionResult<PortfolioSpotlightAsyncResponse>> PollSpotlightPortfolioPlanAsync(
        [FromRoute(Name = "id")] string planId,
        [FromQuery(Name = "task_id")] string taskId)
    {
        var (errorResult, _, portfolioPlan, _) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 0a: check if there is a recent spotlight analysis request
        var recentSpotlightAnalysisRequest = HasRecentSpotlightAnalysisRequest(portfolioPlan);
        if (recentSpotlightAnalysisRequest is null || recentSpotlightAnalysisRequest.TaskId != taskId)
        {
            Logger?.LogInformation(
                "Portfolio plan '{planId}: {planName}' does not have a recent spotlight analysis request matching task '{taskId}'",
                portfolioPlan.Id,
                portfolioPlan.Name,
                taskId);
            return ResponseNoData(
                400,
                $"Portfolio plan '{portfolioPlan.Id}: {portfolioPlan.Name}' does not have a recent spotlight analysis request matching task '{taskId}'");
        }

        // Step 0b: check the recent spotlight analysis request's status
        switch (recentSpotlightAnalysisRequest.State)
        {
            case TaskState.Failed:
                Logger?.LogWarning("Spotlight analysis task '{taskId}' has failed.", taskId);
                return ResponseNoData(
                    400,
                    $"Spotlight analysis task '{taskId}' has failed.");
            case TaskState.Completed:
                Logger?.LogInformation("Spotlight analysis task '{taskId}' has been completed.", taskId);
                return ResponseAsyncOk(portfolioPlan.Metadata!.SpotlightAnalysis, recentSpotlightAnalysisRequest);
            case TaskState.Running:
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

                // Step 2: store the task info for tracking the spotlight analysis request
                portfolioPlan.Metadata ??= new();
                portfolioPlan.Metadata.ChecksumRequestSpotlightAnalysis = portfolioPlan.Metadata.CalcChecksum();
                portfolioPlan.Metadata.TimestampRequestSpotlightAnalysis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                portfolioPlan.Metadata.RequestSpotlightAnalysis = finhubResult.Extra;
                if (finhubResult.Extra.State == TaskState.Completed && finhubResult.Data is not null)
                {
                    // The spotlight analysis has completed successfully. Store the analysis result too
                    StoreSpotlightAnalysisResult(portfolioPlan, finhubResult.Data);
                }
                Logger?.LogInformation("Persisting spotlight analysis request for task '{taskId}' and portfolio '{id}: {name}'...", taskId, portfolioPlan.Id, portfolioPlan.Name);
                var dbresult = await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
                if (dbresult is null)
                {
                    Logger?.LogError("Failed to persist spotlight analysis request for task '{taskId}' and portfolio '{id}: {name}'.", taskId, portfolioPlan.Id, portfolioPlan.Name);
                }

                // Step 3: return result
                return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
            default:
                Logger?.LogInformation("Spotlight analysis task '{taskId}' has an unknown status '{status}'.", taskId, recentSpotlightAnalysisRequest.State);
                return ResponseNoData(
                    400,
                    $"Spotlight analysis task '{taskId}' has an unknown status '{recentSpotlightAnalysisRequest.State}'.");
        }
    }
}
