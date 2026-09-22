using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using FinHub.Client.Schemas.TickerAnalysis;

namespace MyPo.Portfolio.Api.Controllers;

public partial class FinHubController
{
    /// <summary>
    /// Handler for analyzing a ticker symbol using FinHub AI.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="portfolioId"></param>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_ANALYZE_TICKER)]
    public async ValueTask<ActionResult<AnalyzeTickerResponse>> AnalyzeTickerAsync([FromRoute] string symbol, [FromQuery(Name = "pid")] string? portfolioId)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var symbolInfoResp = await FinHubClient.GetStockSymbolInfoAsync(symbol);
        if (!symbolInfoResp.IsSuccess || symbolInfoResp.Data is null)
        {
            return ResponseNoData(404, $"Invalid ticker symbol '{symbol}'");
        }
        var symbolInfo = symbolInfoResp.Data;

        // Step 0: check if currently owning any shares
        var portfolio = string.IsNullOrEmpty(portfolioId)
            ? null
            : await GetPortfolioIfAccessible(currentUser, portfolioId);
        var assets = portfolio is null
            ? []
            : await GetOwningAssets(portfolio.Id) ?? [];
        var parts = symbolInfo.NormalizedSymbol.Split(":") ?? [];
        var (exchange, code) = (parts.Length > 1 ? parts[0] : string.Empty, parts.Length > 1 ? parts[1] : parts[0]);
        var market = Globals.Markets.FirstOrDefault(m => string.Equals(m.Code, exchange, StringComparison.OrdinalIgnoreCase));
        var asset = assets.FirstOrDefault(a => string.Equals(a.ItemCode, code, StringComparison.CurrentCultureIgnoreCase) && string.Equals(a.MarketId, market?.Id, StringComparison.OrdinalIgnoreCase));

        // Step 1: make API call
        Logger?.LogInformation("Calling FinHub AnalyzeTicker API for symbol '{symbol}'...", symbolInfo.NormalizedSymbol);
        var req = new AnalyzeTickerRequest
        {
            Symbol = symbolInfo.NormalizedSymbol,
            CurrentHolding = asset is null
                ? null
                : new TickerHoldingInput{ NumShares = asset.Quantity, AvgPrice = asset.AveragePrice }
        };
        var finhubResult = await FinHubClient.AnalyzeTickerAsync(req);
        if (!finhubResult.IsSuccess || finhubResult.Data is null)
        {
            return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to analyze ticker '{symbol}'", finhubResult.Extra);
        }
        var result = finhubResult.Data;

        // Step 2: return result
        return ResponseOk(result);
    }

    /// <summary>
    /// Handler for starting the analysis of a ticker symbol using FinHub AI.
    /// </summary>
    /// <param name="symbol">The ticker symbol to analyze.</param>
    /// <param name="portfolioId">The ID of the portfolio to check current holdings against.</param>
    /// <returns>The result of the analysis request.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_START_ANALYZE_TICKER)]
    public async ValueTask<ActionResult<AnalyzeTickerAsyncResponse>> StartAnalyzeTickerAsync([FromRoute] string symbol, [FromQuery(Name = "pid")] string? portfolioId)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var symbolInfoResp = await FinHubClient.GetStockSymbolInfoAsync(symbol);
        if (!symbolInfoResp.IsSuccess || symbolInfoResp.Data is null)
        {
            return ResponseNoData(404, $"Invalid ticker symbol '{symbol}'");
        }
        var symbolInfo = symbolInfoResp.Data;

        // Step 0: check if currently owning any shares
        var portfolio = string.IsNullOrEmpty(portfolioId)
            ? null
            : await GetPortfolioIfAccessible(currentUser, portfolioId);
        var assets = portfolio is null
            ? []
            : await GetOwningAssets(portfolio.Id) ?? [];
        var parts = symbolInfo.NormalizedSymbol.Split(":") ?? [];
        var (exchange, code) = (parts.Length > 1 ? parts[0] : string.Empty, parts.Length > 1 ? parts[1] : parts[0]);
        var market = Globals.Markets.FirstOrDefault(m => string.Equals(m.Code, exchange, StringComparison.OrdinalIgnoreCase));
        var asset = assets.FirstOrDefault(a => string.Equals(a.ItemCode, code, StringComparison.CurrentCultureIgnoreCase) && string.Equals(a.MarketId, market?.Id, StringComparison.OrdinalIgnoreCase));

        // Step 1: make API call
        Logger?.LogInformation("Calling FinHub StartAnalyzeTicker API for symbol '{symbol}'...", symbolInfo.NormalizedSymbol);
        var req = new AnalyzeTickerRequest
        {
            Symbol = symbolInfo.NormalizedSymbol,
            CurrentHolding = asset is null
                ? null
                : new TickerHoldingInput{ NumShares = asset.Quantity, AvgPrice = asset.AveragePrice }
        };
        var finhubResult = await FinHubClient.StartAnalyzeTickerAsync(req);
        if (!finhubResult.IsSuccess || finhubResult.Extra is null || string.IsNullOrEmpty(finhubResult.Extra.TaskId))
        {
            return ResponseNoData(
                finhubResult.Status,
                finhubResult.Message ?? $"Failed to start analyzing ticker '{symbol}'",
                finhubResult.Extra);
        }

        // Step 2: return result
        return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
    }

    /// <summary>
    /// Handler for polling the analysis status of a ticker.
    /// </summary>
    /// <param name="taskId">The ID of the analysis task to poll.</param>
    /// <returns>The result of the analysis task.</returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_POLL_ANALYZE_TICKER)]
    public async ValueTask<ActionResult<AnalyzeTickerAsyncResponse>> PollAnalyzeTickerAsync([FromQuery(Name = "task_id")] string taskId)
    {
        // Step 1: make API call
        Logger?.LogInformation("Calling FinHub PollAnalyzeTicker API for task '{taskId}'...", taskId);
        var finhubResult = await FinHubClient.PollAnalyzeTickerAsync(taskId);
        if (!finhubResult.IsSuccess || finhubResult.Extra is null || string.IsNullOrEmpty(finhubResult.Extra.TaskId))
        {
            return ResponseNoData(
                finhubResult.Status,
                finhubResult.Message ?? $"Failed to poll ticker analysis for task '{taskId}'",
                finhubResult.Extra);
        }

        // Step 2: return result
        return ResponseAsyncOk(finhubResult.Data, finhubResult.Extra);
    }
}
