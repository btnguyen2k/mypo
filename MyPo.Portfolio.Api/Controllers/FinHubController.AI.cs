using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Portfolio.Shared.Utils;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class FinHubController
{
    [HttpGet(IPortfolioApiClient.API_FINHUB_AI_ANALYZE_PORTFOLIO)]
    public async ValueTask<ActionResult<ApiResp<PortfolioAnalysis>>> AnalyzePortfolioPlan([FromRoute] string id)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var portfolioPlan = await GetPortfolioPlanIfOwnedByUser(currentUser, id);
        if (portfolioPlan == null)
        {
            return ResponseNoData(404, "Portfolio plan not found.");
        }

        var portfolio = !string.IsNullOrEmpty(portfolioPlan.PortfolioId)
            ? await PortfolioRepository.GetPortfolioByIdAsync(portfolioPlan.PortfolioId)
            : null;
        var market = Globals.MarketsMap.TryGetValue(portfolio?.Metadata?.DefaultMarketId?.ToUpper() ?? string.Empty, out var m) ? m : null;

        /*
        if current holdings is empty, or >=half of tickers are at 0 allocation ==> call API to build the portfolio
        otherwise call API to analyze for portfolio
        */

        // Step 1: check portfolio plan's holdings
        var countPositiveAllocation = (portfolioPlan.Metadata?.HoldingTickers ?? []).Where(ht => ht.Shares > 0).Count();
        var countEntries = (portfolioPlan.Metadata?.HoldingTickers ?? []).Count;

        // Step 2: make API call
        var finhubResult = countEntries == 0 || (double)countPositiveAllocation / countEntries <= 0.5
            ? await BuildPortfolio(portfolioPlan, market)
            : await AnalyzePortfolio(portfolioPlan, market);

        // Step 2: build and return result
        if (!finhubResult.IsSuccess)
        {
            return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to analyze portfolio plan '{portfolioPlan.Name}'", finhubResult.Extras);
        }
        var result = finhubResult.Data ?? new PortfolioAnalysis
        {
            LLMError = true,
            LLMErrorMsg = "No data returned from FinHub API",
            Analysis = string.Empty,
        };
        if (!result.LLMError)
        {
            portfolioPlan.Metadata ??= new();
            portfolioPlan.Metadata.AnalysisRefreshTimestsmp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            portfolioPlan.Metadata.Analysis = result.Analysis;
            await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
        }
        return ResponseOk(result);
    }

    private async ValueTask<ApiResp<PortfolioAnalysis>> BuildPortfolio(PortfolioPlanEntity plan, MarketDef? market)
    {
        var req = new BuildPortfolioReq
        {
            Country = market?.Country ?? "US",
            InvestorTheme = plan.Metadata?.Description,
            CurrentAllocation = [.. (plan.Metadata?.HoldingTickers?? []).Select(ht => new HoldingTickerReq
            {
                Ticker = ht.Ticker,
                TargetAllocation = ht.TargetAllocation,
                NumShares = ht.Shares,
                AvgPrice = ht.AveragePrice,
                MarketPrice = ht.MarketPrice,
                Tags = ht.Tags,
            })]
        };
        return await FinHubClient.BuildPortfolioAsync(req);
    }

    private async ValueTask<ApiResp<PortfolioAnalysis>> AnalyzePortfolio(PortfolioPlanEntity plan, MarketDef? market)
    {
        var req = new BuildPortfolioReq
        {
            Country = market?.Country ?? "US",
            InvestorTheme = plan.Metadata?.Description,
            CurrentAllocation = [.. (plan.Metadata?.HoldingTickers?? []).Select(ht => new HoldingTickerReq
            {
                Ticker = ht.Ticker,
                TargetAllocation = ht.TargetAllocation,
                NumShares = ht.Shares,
                AvgPrice = ht.AveragePrice,
                MarketPrice = ht.MarketPrice,
                Tags = ht.Tags,
            })]
        };
        return await FinHubClient.BuildPortfolioAsync(req);
    }

    /*----------------------------------------------------------------------*/

    [HttpPost(IPortfolioApiClient.API_FINHUB_AI_ANALYZE_TICKER)]
    public async ValueTask<ActionResult<ApiResp<TickerAnalysis>>> AnalyzeTickerAsync([FromBody] TickerAnalysisReq req)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        var symbolInfoResp = await FinHubClient.GetStockSymbolInfoAsync(req.Symbol);
        if (!symbolInfoResp.IsSuccess || symbolInfoResp.Data is null)
        {
            return ResponseNoData(404, $"Invalid ticker symbol '{req.Symbol}'");
        }
        var symbolInfo = symbolInfoResp.Data;

        var portfolio = string.IsNullOrEmpty(req.PortfolioId)
            ? null
            : await GetPortfolioIfOwnedByUser(currentUser, req.PortfolioId);
        var assets = portfolio is null
            ? []
            : await GetOwningAssets(portfolio.Id) ?? [];

        var parts = symbolInfo.NormalizedSymbol.Split(":") ?? [];
        var (exchange, symbol) = (parts.Length > 1 ? parts[0] : string.Empty, parts.Length > 1 ? parts[1] : parts[0]);
        var market = Globals.Markets.FirstOrDefault(m => string.Equals(m.Code, exchange, StringComparison.OrdinalIgnoreCase));
        var asset = assets.FirstOrDefault(a => string.Equals(a.ItemCode, symbol, StringComparison.CurrentCultureIgnoreCase) && string.Equals(a.MarketId, market?.Id, StringComparison.OrdinalIgnoreCase));

        var intent = req.Intent;
        if (asset is not null)
        {
            if (!string.IsNullOrEmpty(intent))
            {
                intent += "\n";
            }
            intent += $"Owning {FormatUtils.FormatValueMaxDecimals(asset.Quantity, 4)} shares, " +
                $"base price {FormatUtils.FormatValueWithScale(asset.AveragePrice, market?.PriceScale ?? 2, market?.ValueFormat)}, " +
                $"market price {FormatUtils.FormatRawValueWithScale(symbolInfo.StockQuote?.MarketPrice ?? 0, market?.PriceScale ?? 2, market?.ValueFormat)}";
        }

        var fhReq = new AnalyzeTickerReq()
        {
            Symbol = symbolInfo.NormalizedSymbol,
            Intent = intent,
        };
        var fhResult = await FinHubClient.AnalyzeTickerAsync(fhReq);

        if (!fhResult.IsSuccess)
        {
            return ResponseNoData(fhResult.Status, fhResult.Message ?? $"Failed to analyze ticker '{req.Symbol}'", fhResult.Extras);
        }
        var result = fhResult.Data ?? new TickerAnalysis
        {
            LLMError = true,
            LLMErrorMsg = "No data returned from FinHub API",
            Analysis = string.Empty,
        };
        //if (!result.LLMError)
        //{
        //	portfolioPlan.Metadata ??= new();
        //	portfolioPlan.Metadata.AnalysisRefreshTimestsmp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //	portfolioPlan.Metadata.Analysis = result.Analysis;
        //	await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
        //}
        return ResponseOk(result);
    }
}
