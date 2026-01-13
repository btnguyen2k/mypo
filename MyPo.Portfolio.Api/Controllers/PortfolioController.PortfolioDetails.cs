using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Portfolio.Shared.Models;
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
			var market = Globals.Markets.FirstOrDefault(m => m.Id.Equals(tx.MarketId, StringComparison.OrdinalIgnoreCase));
			result.Add(TransactionRecResp.BuildFrom(tx, market));
		}
		return ResponseOk(result);
	}

	[HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TRANSACTIONS)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async Task<ActionResult<TransactionRecResp>> AddTransactionToPortfolioAsync([FromBody] CreateOrUpdateTransactionRecReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		// validate transaction type, must be either BUY or SELL
		req.Type = req.Type.ToUpper().Trim();
		if (req.Type != "BUY" && req.Type != "SELL")
		{
			return ResponseNoData(400, $"Transaction type must be either 'BUY' or 'SELL', currently '{req.Type}'.");
		}

		// validate item code, must not be empty
		req.ItemCode = req.ItemCode.ToUpper().Trim();
		if (string.IsNullOrEmpty(req.ItemCode))
		{
			return ResponseNoData(400, "Item code must not be empty.");
		}

		// validate price, must be positive
		if (req.Price <= 0.00m)
		{
			return ResponseNoData(400, "Price must be a positive value.");
		}

		// validate quantity, must be positive
		if (req.Quantity <= 0)
		{
			return ResponseNoData(400, "Quantity must be a positive value.");
		}

		// validate market, must be a valid one or empty
		req.MarketId = req.MarketId?.Trim() ?? null;
		if (!string.IsNullOrEmpty(req.MarketId))
		{
			var market = Globals.Markets.FirstOrDefault(m => m.Id.Equals(req.MarketId, StringComparison.OrdinalIgnoreCase));
			if (market == null)
			{
				return ResponseNoData(400, $"Market '{req.MarketId}' is not recognized.");
			}
			req.Time = TimeZoneInfo.ConvertTime(req.Time, market.TZ).Subtract(market.TZ.BaseUtcOffset);
		}
		req.Time = req.Time.UtcDateTime; // convert to UTC for storing into database

		// validate portfolio, must be current user's portfolio
		var myPortfolioList = await PortfolioRepository.GetPortfolioByUserIdAsync(currentUser.Id);
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id == req.PortfolioId);
		if (existingPortfolio == null)
		{
			return ResponseNoData(404, "Portfolio not found.");
		}

		var tx = new TransactionRec
		{
			PortfolioId = req.PortfolioId,
			Type = req.Type,
			Time = req.Time == default ? DateTimeOffset.Now.UtcDateTime : req.Time,
			Quantity = req.Quantity,
			Price = req.Price,
			Notes = req.Notes?.Trim() ?? null,
			FeeTx = req.FeeTx ?? 0.0m,
			FeeTax = req.FeeTax ?? 0.0m,
			FeeOther = req.FeeOther ?? 0.0m,
			ItemType = req.ItemType,
			ItemCode = req.ItemCode,
			MarketId = req.MarketId,
			IsSettled = false,
		};
		var result = await PortfolioRepository.CreateTxAsync(tx);
		if (result == null)
		{
			return ResponseNoData(500, "Failed to create transaction record.");
		}
		return ResponseOk(TransactionRecResp.BuildFrom(tx));
	}
}
