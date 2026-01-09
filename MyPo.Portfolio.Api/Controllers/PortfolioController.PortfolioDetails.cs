using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
	/// <summary>
	/// Gets current user's portfolio transactions.
	/// </summary>
	[HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TRANSACTIONS)]
	public async Task<ActionResult<ApiResp<IEnumerable<TransactionRecResp>>>> GetMyPortfolioTransactions([FromRoute] string id)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var myPortfolioList = await PortfolioRepository.GetPortfolioByUserIdAsync(currentUser.Id);
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id == id);
		if (existingPortfolio == null)
		{
			return ResponseNoData(404, "Portfolio not found.");
		}

		var txList = await PortfolioRepository.GetTransactionsByPortfolioIdAsync(id);
		var result = new List<TransactionRecResp>();
		foreach (var tx in txList)
		{
			result.Add(TransactionRecResp.BuildFrom(tx));
		}
		return ResponseOk(result);
	}
}
