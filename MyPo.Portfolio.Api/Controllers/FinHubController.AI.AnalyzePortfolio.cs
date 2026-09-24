using FinHub.Client.Models.Portfolios;
using FinHub.Client.Schemas.PortfolioAnalysis;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Api.Utils;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class FinHubController
{
    private static IPortfolioAnalysisResult? HasRecentDeepAnalysis(PortfolioPlanEntity portfolioPlan)
    {
        var oldChecksum = portfolioPlan.Metadata?.ChecksumLastDeepAnalysis ?? null;
        var curChecksum = portfolioPlan.Metadata?.CalcChecksum() ?? null;
        var lastAnalysisRefreshTimestamp = portfolioPlan.Metadata?.RefreshTimestampDeepAnalysis ?? 0;
        var lastAnalysis = portfolioPlan.Metadata?.DeepAnalysis ?? null;
        return lastAnalysis is null
            || oldChecksum != curChecksum
            || lastAnalysisRefreshTimestamp <= DateTimeOffset.UtcNow.AddHours(-24).ToUnixTimeSeconds()
            ? null
            : lastAnalysis;
    }

    private static AsyncTaskInfo? HasRecentDeepAnalysisRequest(PortfolioPlanEntity portfolioPlan)
    {
        var oldChecksum = portfolioPlan.Metadata?.ChecksumRequestDeepAnalysis ?? null;
        var curChecksum = portfolioPlan.Metadata?.CalcChecksum() ?? null;
        var lastAnalysisRequestTimestamp = portfolioPlan.Metadata?.TimestampRequestDeepAnalysis ?? 0;
        var lastAnalysisRequest = portfolioPlan.Metadata?.RequestDeepAnalysis ?? null;
        return lastAnalysisRequest is null
            || oldChecksum != curChecksum
            || lastAnalysisRequestTimestamp <= DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()
            ? null
            : lastAnalysisRequest;
    }

    private static void StoreDeepAnalysisResult(PortfolioPlanEntity portfolioPlan, IPortfolioAnalysisResult result)
    {
        portfolioPlan.Metadata ??= new();
        portfolioPlan.Metadata.ChecksumLastDeepAnalysis = portfolioPlan.Metadata.CalcChecksum();
        portfolioPlan.Metadata.RefreshTimestampDeepAnalysis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        portfolioPlan.Metadata.DeepAnalysis = result;
    }

    /// <summary>
    /// Handler for analyzing the specified portfolio plan using the FinHub API.
    /// </summary>
    /// <param name="id">The ID of the portfolio plan to analyze.</param>
    /// <returns>The analysis result of the portfolio plan.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_ANALYZE_PORTFOLIO)]
    public async ValueTask<ActionResult<AnalyzePortfolioResponse>> AnalyzePortfolioPlan(
        [FromRoute(Name = "id")] string planId)
    {
        var (errorResult, _, portfolioPlan, market) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 0: check if the portfolio plan already has a recent deep analysis
        var recentDeepAnalysis = HasRecentDeepAnalysis(portfolioPlan);
        if (recentDeepAnalysis is not null)
        {
            Logger?.LogInformation(
                "Portfolio plan '{planId}: {planName}' already has a recent analysis.",
                portfolioPlan.Id,
                portfolioPlan.Name);
            return ResponseOk(recentDeepAnalysis);
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
            StoreDeepAnalysisResult(portfolioPlan, result);
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
    public async ValueTask<ActionResult<AnalyzePortfolioAsyncResponse>> StartAnalyzePortfolioPlanAsync(
        [FromRoute(Name = "id")] string planId)
    {
        var (errorResult, _, portfolioPlan, market) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 0a: check if the portfolio plan already has a recent deep analysis
        var recentDeepAnalysis = HasRecentDeepAnalysis(portfolioPlan);
        if (recentDeepAnalysis is not null)
        {
            Logger?.LogInformation(
                "Portfolio plan '{planId}: {planName}' already has a recent deep analysis.",
                portfolioPlan.Id,
                portfolioPlan.Name);
            return ResponseAsyncOk(recentDeepAnalysis, TaskCompleted);
        }

        // Step 0b: check if there is a recent deep analysis request
        var recentDeepAnalysisRequest = HasRecentDeepAnalysisRequest(portfolioPlan);
        if (recentDeepAnalysisRequest is not null)
        {
            Logger?.LogInformation(
                "Portfolio plan '{planId}: {planName}' already has a recent deep analysis request - TaskId: {taskId}",
                portfolioPlan.Id,
                portfolioPlan.Name,
                recentDeepAnalysisRequest.TaskId);
            var draftAnalysis = portfolioPlan.Metadata?.DeepAnalysis;
            return ResponseAsyncOk(draftAnalysis, recentDeepAnalysisRequest);
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

        // Step 2: store the task info for tracking the deep analysis request
        portfolioPlan.Metadata ??= new();
        portfolioPlan.Metadata.ChecksumRequestDeepAnalysis = portfolioPlan.Metadata.CalcChecksum();
        portfolioPlan.Metadata.TimestampRequestDeepAnalysis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        portfolioPlan.Metadata.RequestDeepAnalysis = finhubResult.Extra;
        if (finhubResult.Extra.State == TaskState.Completed && finhubResult.Data is not null)
        {
            // The deep analysis has completed successfully. Store the analysis result too
            StoreDeepAnalysisResult(portfolioPlan, finhubResult.Data);
        }
        Logger?.LogInformation("Persisting deep analysis request for portfolio '{id}: {name}'...", portfolioPlan.Id, portfolioPlan.Name);
        var dbresult = await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
        if (dbresult is null)
        {
            Logger?.LogError("Failed to persist deep analysis request for portfolio '{id}: {name}'.", portfolioPlan.Id, portfolioPlan.Name);
        }

        // Step 3: return result
        return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
    }

    /// <summary>
    /// Handler for polling the analysis status of a portfolio plan.
    /// </summary>
    /// <param name="planId">The ID of the portfolio plan to poll.</param>
    /// <param name="taskId">The task ID of the portfolio analysis to poll.</param>
    /// <returns>The current status and result of the portfolio analysis task.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_POLL_ANALYZE_PORTFOLIO)]
    public async ValueTask<ActionResult<AnalyzePortfolioAsyncResponse>> PollAnalyzePortfolioPlanAsync(
        [FromRoute(Name = "id")] string planId,
        [FromQuery(Name = "task_id")] string taskId)
    {
        var (errorResult, _, portfolioPlan, _) = await ValidatePortfolioPlan(planId);
        if (errorResult != null)
        {
            return errorResult;
        }

        // Step 0a: check if there is a recent deep analysis request
        var recentDeepAnalysisRequest = HasRecentDeepAnalysisRequest(portfolioPlan);
        if (recentDeepAnalysisRequest is null || recentDeepAnalysisRequest.TaskId != taskId)
        {
            Logger?.LogInformation(
                "Portfolio plan '{planId}: {planName}' does not have a recent deep analysis request matching task '{taskId}'",
                portfolioPlan.Id,
                portfolioPlan.Name,
                taskId);
            return ResponseNoData(
                400,
                $"Portfolio plan '{portfolioPlan.Id}: {portfolioPlan.Name}' does not have a recent deep analysis request matching task '{taskId}'");
        }

        // Step 0b: check the recent deep analysis request's status
        switch (recentDeepAnalysisRequest.State)
        {
            case TaskState.Failed:
                Logger?.LogWarning("Deep analysis task '{taskId}' has failed.", taskId);
                return ResponseNoData(
                    400,
                    $"Deep analysis task '{taskId}' has failed.");
            case TaskState.Completed:
                Logger?.LogInformation("Deep analysis task '{taskId}' has been completed.", taskId);
                return ResponseAsyncOk(portfolioPlan.Metadata!.DeepAnalysis, recentDeepAnalysisRequest);
            case TaskState.Running:
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

                // Step 2: store the task info for tracking the deep analysis request
                portfolioPlan.Metadata ??= new();
                portfolioPlan.Metadata.ChecksumRequestDeepAnalysis = portfolioPlan.Metadata.CalcChecksum();
                portfolioPlan.Metadata.TimestampRequestDeepAnalysis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                portfolioPlan.Metadata.RequestDeepAnalysis = finhubResult.Extra;
                if (finhubResult.Extra.State == TaskState.Completed && finhubResult.Data is not null)
                {
                    // The deep analysis has completed successfully. Store the analysis result too
                    StoreDeepAnalysisResult(portfolioPlan, finhubResult.Data);
                }
                Logger?.LogInformation("Persisting deep analysis request for task '{taskId}' and portfolio '{id}: {name}'...", taskId, portfolioPlan.Id, portfolioPlan.Name);
                var dbresult = await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
                if (dbresult is null)
                {
                    Logger?.LogError("Failed to persist deep analysis request for task '{taskId}' and portfolio '{id}: {name}'.", taskId, portfolioPlan.Id, portfolioPlan.Name);
                }

                // Step 3: return result
                return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
            default:
                Logger?.LogInformation("Deep analysis task '{taskId}' has an unknown status '{status}'.", taskId, recentDeepAnalysisRequest.State);
                return ResponseNoData(
                    400,
                    $"Deep analysis task '{taskId}' has an unknown status '{recentDeepAnalysisRequest.State}'.");
        }
    }
}
