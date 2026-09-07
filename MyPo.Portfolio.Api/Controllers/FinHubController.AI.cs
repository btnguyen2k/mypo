using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using FinHub.Client.Schemas.TickerAnalysis;
using FinHub.Client.Schemas.PortfolioSpotlight;
using MyPo.Portfolio.Api.Utils;
using FinHub.Client.Schemas.PortfolioAnalysis;
using FinHub.Client.Models.Stocks;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class FinHubController
{
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_ANALYZE_PORTFOLIO)]
    public async ValueTask<ActionResult<AnalyzePortfolioResponse>> AnalyzePortfolioPlan([FromRoute] string id)
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

        var portfolio = !string.IsNullOrEmpty(portfolioPlan.PortfolioId)
            ? await PortfolioRepository.GetPortfolioByIdAsync(portfolioPlan.PortfolioId)
            : null;
        var market = Globals.MarketsMap.TryGetValue(portfolio?.Metadata?.DefaultMarketId?.ToUpper() ?? string.Empty, out var m) ? m : null;

        // Step 1: make API call
        var finhubResult = await AnalyzePortfolio(portfolioPlan, market);
        if (!finhubResult.IsSuccess || finhubResult.Data is null)
        {
            return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to analyze portfolio plan '{portfolioPlan.Name}'", finhubResult.Extra);
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
            if (dbresult == null)
            {
                Logger?.LogError("Failed to persist analysis for portfolio '{id}: {name}'.", portfolioPlan.Id, portfolioPlan.Name);
            }
        }

        // Step 3: return result
        return ResponseOk(result);
    }

    private async ValueTask<AnalyzePortfolioResponse> AnalyzePortfolio(PortfolioPlanEntity plan, MarketDef? market)
    {
        Logger?.LogInformation("Calling FinHub AnalyzePortfolio API for portfolio plan '{planId}: {planName}' (market: {market}) with {numHoldings} holdings...", plan.Id, plan.Name, market?.Code ?? "N/A", plan.Metadata?.HoldingTickers?.Count ?? 0);
        var req = new AnalyzePortfolioRequest
        {
            Country = market?.Country ?? "US",
            InvestorTheme = plan.Metadata?.Description ?? string.Empty,
            CurrentAllocation = FinHubHelper.BuildAllocationReqs(plan),
            RebalancePlan = plan.Type == PortfolioPlanEntity.PLAN_TYPE_ALLOCATION,
        };
        var analysisResult = await FinHubClient.AnalyzePortfolioAsync(req);
        return analysisResult;
    }

    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_SPOTLIGHT_PORTFOLIO)]
    public async ValueTask<ActionResult<PortfolioSpotlightResponse>> SpotlightPortfolioPlan([FromRoute] string id)
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

        var portfolio = !string.IsNullOrEmpty(portfolioPlan.PortfolioId)
            ? await PortfolioRepository.GetPortfolioByIdAsync(portfolioPlan.PortfolioId)
            : null;
        var market = Globals.MarketsMap.TryGetValue(portfolio?.Metadata?.DefaultMarketId?.ToUpper() ?? string.Empty, out var m) ? m : null;

        // Step 1: make API call
        var finhubResult = await SpotlightPortfolio(portfolioPlan, market);
        if (!finhubResult.IsSuccess || finhubResult.Data is null)
        {
            return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to spotlight analyze portfolio plan '{portfolioPlan.Name}'", finhubResult.Extra);
        }
        var result = finhubResult.Data;

        // Step 2: save analysis result to portfolio plan's metadata
        {
            // save spotlight analysis to the portfolio plan's metadata
            portfolioPlan.Metadata ??= new();
            portfolioPlan.Metadata.LastChecksumAnalysis = portfolioPlan.Metadata.CalcChecksumAnalysis();
            portfolioPlan.Metadata.SpotlightRefreshTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            portfolioPlan.Metadata.SpotlightAnalysis = result;
            Logger?.LogInformation("Persisting spotlight analysis for portfolio '{id}: {name}'...", portfolioPlan.Id, portfolioPlan.Name);
            var dbresult = await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
            if (dbresult == null)
            {
                Logger?.LogError("Failed to persist spotlight analysis for portfolio '{id}: {name}'.", portfolioPlan.Id, portfolioPlan.Name);
            }
        }

        // Step 3: return result
        return ResponseOk(result);
    }

    private async ValueTask<PortfolioSpotlightResponse> SpotlightPortfolio(PortfolioPlanEntity plan, MarketDef? market)
    {
        Logger?.LogInformation("Calling FinHub SpotlightPortfolio API for portfolio plan '{planId}: {planName}' (market: {market}) with {numHoldings} holdings...", plan.Id, plan.Name, market?.Code ?? "N/A", plan.Metadata?.HoldingTickers?.Count ?? 0);
        var req = new PortfolioSpotlightRequest
        {
            Country = market?.Country ?? "US",
            InvestorTheme = plan.Metadata?.Description ?? string.Empty,
            CurrentAllocation = FinHubHelper.BuildAllocationReqs(plan)
        };
        return await FinHubClient.SpotlightPortfolioAsync(req);
    }

    /*----------------------------------------------------------------------*/

    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_ANALYZE_TICKER)]
    public async ValueTask<ActionResult<AnalyzeTickerResponse>> AnalyzeTickerAsync([FromRoute] string symbol, [FromQuery] string? pid)
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
        var portfolio = string.IsNullOrEmpty(pid)
            ? null
            : await GetPortfolioIfAccessible(currentUser, pid);
        var assets = portfolio is null
            ? []
            : await GetOwningAssets(portfolio.Id) ?? [];
        var parts = symbolInfo.NormalizedSymbol.Split(":") ?? [];
        var (exchange, code) = (parts.Length > 1 ? parts[0] : string.Empty, parts.Length > 1 ? parts[1] : parts[0]);
        var market = Globals.Markets.FirstOrDefault(m => string.Equals(m.Code, exchange, StringComparison.OrdinalIgnoreCase));
        var asset = assets.FirstOrDefault(a => string.Equals(a.ItemCode, code, StringComparison.CurrentCultureIgnoreCase) && string.Equals(a.MarketId, market?.Id, StringComparison.OrdinalIgnoreCase));

        // Step 1: make API call
        var finhubResult = await AnalyzeTicker(symbolInfo, asset);
        if (!finhubResult.IsSuccess || finhubResult.Data is null)
        {
            return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to analyze ticker '{symbol}'", finhubResult.Extra);
        }
        var result = finhubResult.Data;

        // Step 2: return result
        return ResponseOk(result);
    }

    private async ValueTask<AnalyzeTickerResponse> AnalyzeTicker(SymbolInfo symbol, AssetEntity? asset = null)
    {
        Logger?.LogInformation("Calling FinHub AnalyzeTicker API for symbol '{symbol}'...", symbol.NormalizedSymbol);
        var req = new AnalyzeTickerRequest
        {
            Symbol = symbol.NormalizedSymbol,
            CurrentHolding = asset is null
                ? null
                : new TickerHoldingInput{ NumShares = asset.Quantity, AvgPrice = asset.AveragePrice }
        };
        return await FinHubClient.AnalyzeTickerAsync(req);
    }
}
