using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
	/// <summary>
	/// Gets current user's portfolio ROI records.
	/// </summary>
	[HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_RECS)]
	public async ValueTask<ActionResult<ApiResp<IEnumerable<RoiRecResp>>>> GetMyPortfolioRoiRecs([FromRoute] string id)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		// validate portfolio, must be current user's portfolio
		var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
		if (existingPortfolio == null)
		{
			return ResponseNoData(404, "Portfolio not found.");
		}

		var roiRecList = await PortfolioRepository.GetRoiRecsByPortfolioIdAsync(id);
		var result = new List<RoiRecResp>();
		foreach (var rr in roiRecList)
		{
			var market = Globals.MarketsMap.TryGetValue(rr.RefMarketId?.ToUpper()??string.Empty, out var mkt) ? mkt : null;
			result.Add(RoiRecResp.BuildFrom(rr, market));
		}
		return ResponseOk(result);
	}

	[HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_PNL)]
	public async Task<ActionResult<ApiResp<PnlSummaryResp>>> GetMyPortfolioPnl([FromRoute] string id)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		// validate portfolio, must be current user's portfolio
		var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
		if (existingPortfolio == null)
		{
			return ResponseNoData(404, "Portfolio not found.");
		}

		var pnlSummary = await PortfolioRepository.GetRoiSummaryForPortfolioAsync(id);
		return ResponseOk(PnlSummaryResp.BuildFrom(pnlSummary));
	}
}
