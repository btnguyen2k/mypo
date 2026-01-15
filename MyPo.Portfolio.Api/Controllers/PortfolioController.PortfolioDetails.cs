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
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
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

	private static (CreateOrUpdateTransactionRecReq?, ObjectResult?) ValidateTx(CreateOrUpdateTransactionRecReq reqTx, TransactionRec? existingTx)
	{
		// validate transaction type, must be either BUY or SELL
		reqTx.Type = reqTx.Type.ToUpper().Trim();
		if (!(existingTx?.IsSettled??false) && (reqTx.Type != "BUY" && reqTx.Type != "SELL"))
		{
			return (null, ResponseNoData(400, $"Transaction type must be either 'BUY' or 'SELL', currently '{reqTx.Type}'."));
		}

		// validate item code, must not be empty
		reqTx.ItemCode = reqTx.ItemCode.ToUpper().Trim();
		if (!(existingTx?.IsSettled??false) && string.IsNullOrEmpty(reqTx.ItemCode))
		{
			return (null, ResponseNoData(400, "Item code must not be empty."));
		}

		// validate price, must be positive
		if (!(existingTx?.IsSettled??false) && reqTx.Price <= 0.00m)
		{
			return (null, ResponseNoData(400, "Price must be a positive value."));
		}

		// validate quantity, must be positive
		if (!(existingTx?.IsSettled??false) && reqTx.Quantity <= 0)
		{
			return (null, ResponseNoData(400, "Quantity must be a positive value."));
		}

		// validate market, must be a valid one or empty
		reqTx.MarketId = reqTx.MarketId?.Trim() ?? null;
		if (!(existingTx?.IsSettled??false) && !string.IsNullOrEmpty(reqTx.MarketId))
		{
			var market = Globals.Markets.FirstOrDefault(m => m.Id.Equals(reqTx.MarketId, StringComparison.OrdinalIgnoreCase));
			if (market == null)
			{
				return (null, ResponseNoData(400, $"Market '{reqTx.MarketId}' is not recognized."));
			}
			// shift the transaction's timezone to market's timezone
			reqTx.Time = new DateTimeOffset(reqTx.Time.DateTime, market.TZ.BaseUtcOffset);
		}
		reqTx.Time = reqTx.Time.ToUniversalTime(); // convert to UTC for storing into database

		return (reqTx, null);
	}

	[HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TRANSACTIONS)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async Task<ActionResult<ApiResp<TransactionRecResp>>> AddTransactionToPortfolio([FromRoute] string id, [FromBody] CreateOrUpdateTransactionRecReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var (reqTx, validationResult) = ValidateTx(req, null);
		if (validationResult != null)
		{
			return validationResult;
		}

		// validate portfolio, must be current user's portfolio
		var myPortfolioList = await PortfolioRepository.GetPortfolioByUserIdAsync(currentUser.Id);
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
		if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase)??false))
		{
			return ResponseNoData(400, "Portfolio not found or mismatched.");
		}

		var tx = new TransactionRec
		{
			PortfolioId = reqTx!.Value.PortfolioId,
			Type = reqTx!.Value.Type,
			Time = reqTx!.Value.Time == default ? DateTimeOffset.UtcNow : reqTx!.Value.Time,
			Quantity = reqTx!.Value.Quantity,
			Price = reqTx!.Value.Price,
			Notes = reqTx!.Value.Notes?.Trim() ?? null,
			FeeTx = reqTx!.Value.FeeTx ?? 0.0m,
			FeeTax = reqTx!.Value.FeeTax ?? 0.0m,
			FeeOther = reqTx!.Value.FeeOther ?? 0.0m,
			ItemType = reqTx!.Value.ItemType,
			ItemCode = reqTx!.Value.ItemCode,
			MarketId = reqTx!.Value.MarketId,
			IsSettled = false,
		};
		var result = await PortfolioRepository.CreateTxAsync(tx);
		if (result == null)
		{
			return ResponseNoData(500, "Failed to create transaction record.");
		}
		return ResponseOk(TransactionRecResp.BuildFrom(tx));
	}

	[HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TX_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async Task<ActionResult<ApiResp<TransactionRecResp>>> UpdateTransactionFromPortfolio([FromRoute] string id, [FromRoute] string txid, [FromBody] CreateOrUpdateTransactionRecReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var existingTx = await PortfolioRepository.GetTxAsync(txid);
		if (existingTx == null)
		{
			return ResponseNoData(404, "Transaction record not found.");
		}
		var (reqTx, validationResult) = ValidateTx(req, existingTx);
		if (validationResult != null)
		{
			return validationResult;
		}

		// validate portfolio, must be current user's portfolio
		var myPortfolioList = await PortfolioRepository.GetPortfolioByUserIdAsync(currentUser.Id);
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
		if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase)??false))
		{
			return ResponseNoData(400, "Portfolio not found or mismatched.");
		}

		if (existingTx.IsSettled)
		{
			// for settled transactions, only notes can be updated
			existingTx.Notes = reqTx!.Value.Notes?.Trim() ?? null;
		}
		else
		{
			existingTx.Type = reqTx!.Value.Type;
			existingTx.Time = reqTx!.Value.Time == default ? DateTimeOffset.UtcNow : reqTx!.Value.Time;
			existingTx.Quantity = reqTx!.Value.Quantity;
			existingTx.Price = reqTx!.Value.Price;
			existingTx.Notes = reqTx!.Value.Notes?.Trim() ?? null;
			existingTx.FeeTx = reqTx!.Value.FeeTx ?? 0.0m;
			existingTx.FeeTax = reqTx!.Value.FeeTax ?? 0.0m;
			existingTx.FeeOther = reqTx!.Value.FeeOther ?? 0.0m;
			existingTx.ItemType = reqTx!.Value.ItemType;
			existingTx.ItemCode = reqTx!.Value.ItemCode;
			existingTx.MarketId = reqTx!.Value.MarketId;
		}
		existingTx = await PortfolioRepository.UpdateTxAsync(existingTx);
		if (existingTx == null)
		{
			return ResponseNoData(500, "Failed to update settled transaction record.");
		}
		return ResponseOk(TransactionRecResp.BuildFrom(existingTx));
	}

	[HttpDelete(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_TX_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async Task<ActionResult<ApiResp<TransactionRecResp>>> DeleteTransactionFromPortfolio([FromRoute] string id, [FromRoute] string txid)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var existingTx = await PortfolioRepository.GetTxAsync(txid);
		if (existingTx == null)
		{
			return ResponseNoData(404, "Transaction record not found.");
		}

		// validate portfolio, must be current user's portfolio
		var myPortfolioList = await PortfolioRepository.GetPortfolioByUserIdAsync(currentUser.Id);
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
		if (!(existingPortfolio?.Id.Equals(existingTx.PortfolioId, StringComparison.OrdinalIgnoreCase)??false))
		{
			return ResponseNoData(400, "Portfolio not found or mismatched.");
		}

		if (existingTx.IsSettled)
		{
			return ResponseNoData(400, "Cannot delete a settled transaction.");
		}
		else
		{
			var result = await PortfolioRepository.DeleteTxAsync(existingTx);
			if (!result)
			{
				return ResponseNoData(500, "Failed to delete transaction record.");
			}
			return ResponseOk(TransactionRecResp.BuildFrom(existingTx));
		}
	}

	[HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLE_TX_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async Task<ActionResult<ApiResp<TransactionRecResp>>> SettleTransactionInPortfolio([FromRoute] string id, [FromRoute] string txid, [FromBody] CreateOrUpdateTransactionRecReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var existingTx = await PortfolioRepository.GetTxAsync(txid);
		if (existingTx == null)
		{
			return ResponseNoData(404, "Transaction record not found.");
		}
		if (existingTx.IsSettled)
		{
			return ResponseNoData(400, "Transaction is already settled.");
		}
		var (reqTx, validationResult) = ValidateTx(req, existingTx);
		if (validationResult != null)
		{
			return validationResult;
		}

		// validate portfolio, must be current user's portfolio
		var myPortfolioList = await PortfolioRepository.GetPortfolioByUserIdAsync(currentUser.Id);
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
		if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase)??false))
		{
			return ResponseNoData(400, "Portfolio not found or mismatched.");
		}

		existingTx.Type = reqTx!.Value.Type;
		existingTx.Time = reqTx!.Value.Time == default ? DateTimeOffset.UtcNow : reqTx!.Value.Time;
		existingTx.Quantity = reqTx!.Value.Quantity;
		existingTx.Price = reqTx!.Value.Price;
		existingTx.Notes = reqTx!.Value.Notes?.Trim() ?? null;
		existingTx.FeeTx = reqTx!.Value.FeeTx ?? 0.0m;
		existingTx.FeeTax = reqTx!.Value.FeeTax ?? 0.0m;
		existingTx.FeeOther = reqTx!.Value.FeeOther ?? 0.0m;
		existingTx.ItemType = reqTx!.Value.ItemType;
		existingTx.ItemCode = reqTx!.Value.ItemCode;
		existingTx.MarketId = reqTx!.Value.MarketId;

		var market = Globals.Markets.FirstOrDefault(m => m.Id.Equals(existingTx.MarketId, StringComparison.OrdinalIgnoreCase));
		existingTx = await PortfolioRepository.SettleTxAsync(existingTx, market);
		if (existingTx == null)
		{
			return ResponseNoData(500, "Failed to settle transaction record.");
		}
		return ResponseOk(TransactionRecResp.BuildFrom(existingTx));
	}

	/// <summary>
	/// Gets current user's portfolio assets.
	/// </summary>
	[HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ASSETS)]
	public async Task<ActionResult<ApiResp<IEnumerable<TransactionRecResp>>>> GetMyPortfolioAssets([FromRoute] string id)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var myPortfolioList = await PortfolioRepository.GetPortfolioByUserIdAsync(currentUser.Id);
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
		if (existingPortfolio == null)
		{
			return ResponseNoData(404, "Portfolio not found.");
		}

		var assetList = await PortfolioRepository.GetAssetsByPortfolioIdAsync(id);
		var result = new List<AssetResp>();
		foreach (var asset in assetList)
		{
			var market = Globals.Markets.FirstOrDefault(m => m.Id.Equals(asset.MarketId, StringComparison.OrdinalIgnoreCase));
			result.Add(AssetResp.BuildFrom(asset, market));
		}
		return ResponseOk(result);
	}
}
