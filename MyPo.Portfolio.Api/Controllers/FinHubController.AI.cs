using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
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

		var req = new AnalyzePortfolioReq
		{
			Country = market?.Country ?? "US",
			InvestorTheme = portfolioPlan.Metadata?.Description,
			CurrentAllocation = [.. (portfolioPlan.Metadata?.HoldingTickers?? []).Select(ht => new HoldingTickerReq
			{
				Ticker = ht.Ticker,
				TargetAllocation = ht.TargetAllocation,
				NumShares = ht.Shares,
				AvgPrice = ht.AveragePrice,
				MarketPrice = ht.MarketPrice,
			})]
		};
		var template = PortfolioPlanEntity.PLAN_TYPE_ALLOCATION.Equals(portfolioPlan.Type, StringComparison.OrdinalIgnoreCase)
			? IFinHubClient.PORTFOLIO_ANALYSIS_TEMPLATE_ALLOCATION
			: PortfolioPlanEntity.PLAN_TYPE_PL.Equals(portfolioPlan.Type, StringComparison.OrdinalIgnoreCase)
				? IFinHubClient.PORTFOLIO_ANALYSIS_TEMPLATE_SWING
				: IFinHubClient.PORTFOLIO_ANALYSIS_TEMPLATE_HYBRID;

		var finhubResult = await FinHubClient.AnalyzePortfolioAsync(req, template);
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
			portfolioPlan.Metadata ??= new ();
			portfolioPlan.Metadata.AnalysisRefreshTimestsmp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			portfolioPlan.Metadata.Analysis = result.Analysis;
			await PortfolioRepository.UpdatePortfolioPlanAsync(portfolioPlan);
		}
		return ResponseOk(result);
	}
}
