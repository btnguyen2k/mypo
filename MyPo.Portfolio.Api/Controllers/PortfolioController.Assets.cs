using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
	/// <summary>
	/// Gets current user's portfolio records.
	/// </summary>
	/// <returns></returns>
	/// <remarks>The user is identified by the auth token.</remarks>
	[HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO)]
	public async Task<ActionResult<ApiResp<List<PortfolioRecResp>>>> GetMyPortfolio(IPortfolioRepository portfolioRepo)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var result = new List<PortfolioRecResp>();
		var myPortfolioList = await portfolioRepo.GetPortfolioByUserIdAsync(currentUser.Id);
		foreach (var portfolio in myPortfolioList)
		{
			result.Add(PortfolioRecResp.BuildFrom(portfolio));
		}
		return ResponseOk(result);
	}
}
