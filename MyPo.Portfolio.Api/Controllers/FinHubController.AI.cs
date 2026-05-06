using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
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

		var holdings = new Dictionary<string, decimal>();
		foreach (var ht in portfolioPlan.Metadata?.HoldingTickers?? [])
		{
			holdings[ht.Ticker] = ht.TargetAllocation;
		}

		var finhubResult = await FinHubClient.AnalyzePortfolioAsync(holdings, market?.Country??"US", portfolioPlan.Metadata?.Description);
		if (!finhubResult.IsSuccess)
		{
			return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to analyze portfolio plan '{portfolioPlan.Name}'", finhubResult.Extras);
		}
		var result = new PortfolioAnalysis
		{
			LLMError = string.IsNullOrEmpty(finhubResult.Data?.Analysis) || (finhubResult.Data?.LLMError??false),
			LLMErrorMsg = finhubResult.Data?.LLMErrorMsg ?? "Unknown error from LLM",
			Analysis = finhubResult.Data?.Analysis ?? "",
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
