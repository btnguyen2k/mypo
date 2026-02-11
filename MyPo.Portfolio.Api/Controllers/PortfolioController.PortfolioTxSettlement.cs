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
	/// Gets current user's portfolio Settlement records.
	/// </summary>
	/// <param name="id">ID of the portfolio to fetch ROI records from.</param>
	/// <returns></returns>
	[HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENTS)]
	public async ValueTask<ActionResult<ApiResp<IEnumerable<TxSettlementResp>>>> GetMyPortfolioTxSettlements([FromRoute] string id)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		// validate portfolio, must be current user's portfolio
		var existingPortfolio = await GetPortfolioIfAccessible(currentUser, id);
		if (existingPortfolio == null)
		{
			return ResponseNoData(404, "Portfolio not found or not accessible.");
		}

		var roiRecList = await PortfolioRepository.GetTxSettlementsByPortfolioIdAsync(id);
		var result = new List<TxSettlementResp>();
		foreach (var rr in roiRecList)
		{
			var market = Globals.MarketsMap.TryGetValue(rr.RefMarketId?.ToUpper() ?? string.Empty, out var mkt) ? mkt : null;
			result.Add(TxSettlementResp.BuildFrom(rr, market));
		}
		return ResponseOk(result);
	}

	private static (CreateOrUpdateTxSettlementReq?, ObjectResult?) ValidateTxSettlement(CreateOrUpdateTxSettlementReq reqTx, TxSettlementEntity? existingTx)
	{
		var isImmutable = existingTx != null && TxSettlementEntity.ImmutableStatuses.Contains(existingTx.Status.ToUpper());

		// validate transaction type
		reqTx.TxType = reqTx.TxType.ToUpper().Trim();
		if (!isImmutable && !TxSettlementEntity.TxTypes.Contains(reqTx.TxType))
		{
			return (null, ResponseNoData(400, $"Transaction type must be one of: {string.Join(", ", TxSettlementEntity.TxTypes)}, currently '{reqTx.TxType}'."));
		}

		// validate value, must be positive
		if (!isImmutable && reqTx.TxValue <= 0.00m)
		{
			return (null, ResponseNoData(400, "Transaction value must be a positive value."));
		}

		// validate market, must be a valid one or empty
		reqTx.RefMarketId = reqTx.RefMarketId?.Trim() ?? null;
		if (!isImmutable && !string.IsNullOrEmpty(reqTx.RefMarketId))
		{
			var market = Globals.MarketsMap.TryGetValue(reqTx.RefMarketId.ToUpper(), out var mkt) ? mkt : null;
			if (market == null)
			{
				return (null, ResponseNoData(400, $"Market '{reqTx.RefMarketId}' is not recognized."));
			}
			// shift the transaction's timezone to market's timezone
			reqTx.TxTime = new DateTimeOffset(reqTx.TxTime.DateTime, market.TZ.GetUtcOffset(reqTx.TxTime));
		}
		reqTx.TxTime = reqTx.TxTime.ToUniversalTime(); // convert to UTC for storing into database

		return (reqTx, null);
	}

	/// <summary>
	/// Creates a new Settlement record and Adds it to current user's portfolio.
	/// </summary>
	/// <param name="id">ID of the portfolio to add ROI record to.</param>
	/// <param name="req">Details of the ROI record to add.</param>
	/// <returns></returns>
	[HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENTS)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<TxSettlementResp>>> AddTxSettlementToMyPortfolio([FromRoute] string id, [FromBody] CreateOrUpdateTxSettlementReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var (reqRoiRec, validationResult) = ValidateTxSettlement(req, null);
		if (validationResult != null)
		{
			return validationResult;
		}

		// validate portfolio, must be current user's portfolio
		var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
		if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase)??false))
		{
			return ResponseNoData(400, "Portfolio not found or mismatched.");
		}

		var rec = new TxSettlementEntity
		{
			Status = TxSettlementEntity.STATUS_NEW,
			PortfolioId = reqRoiRec!.Value.PortfolioId,
			TxType = reqRoiRec!.Value.TxType,
			TxTime = reqRoiRec!.Value.TxTime,
			TxValue = reqRoiRec!.Value.TxValue,
			RefTxId = reqRoiRec!.Value.RefTxId ?? null,
			RefItemType = reqRoiRec!.Value.RefItemType?.Trim().ToUpper() ?? null,
			RefItemCode = reqRoiRec!.Value.RefItemCode?.Trim().ToUpper() ?? null,
			RefMarketId = reqRoiRec!.Value.RefMarketId?.Trim() ?? null,
			TxDesc = reqRoiRec!.Value.TxDesc?.Trim() ?? null,
		};
		var result = await PortfolioRepository.CreateTxSettlementAsync(rec);
		if (result == null)
		{
			return ResponseNoData(500, "Failed to create ROI record.");
		}
		return ResponseOk(TxSettlementResp.BuildFrom(rec));
	}

	/// <summary>
	/// Updates an existing Settlement record from current user's portfolio.
	/// </summary>
	/// <param name="id">ID of the portfolio.</param>
	/// <param name="txid">ID of the ROI record.</param>
	/// <param name="req">Details of the ROI record to update.</param>
	/// <returns></returns>
	[HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENT_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<TxBuySellResp>>> UpdateMyPortfolioTxSettlement([FromRoute] string id, [FromRoute] string txid, [FromBody] CreateOrUpdateTxSettlementReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var existingRec = await PortfolioRepository.GetTxSettlementByIdAsync(txid);
		if (existingRec == null)
		{
			return ResponseNoData(404, "ROI record not found.");
		}
		var (reqRec, validationResult) = ValidateTxSettlement(req, existingRec);
		if (validationResult != null)
		{
			return validationResult;
		}

		// validate portfolio, must be current user's portfolio
		var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
		if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase)??false))
		{
			return ResponseNoData(400, "Portfolio not found or mismatched.");
		}

		if (TxSettlementEntity.ImmutableStatuses.Contains(existingRec.Status?.ToUpper()??string.Empty))
		{
			// for immutable ROI records, only notes/description can be updated
			existingRec.TxDesc = reqRec!.Value.TxDesc?.Trim() ?? null;
		}
		else
		{
			existingRec.TxType = reqRec!.Value.TxType;
			existingRec.TxTime = reqRec!.Value.TxTime == default ? DateTimeOffset.UtcNow : reqRec!.Value.TxTime;
			existingRec.TxValue = reqRec!.Value.TxValue;
			existingRec.RefTxId = reqRec!.Value.RefTxId ?? null;
			existingRec.RefItemType = reqRec!.Value.RefItemType?.Trim().ToUpper() ?? null;
			existingRec.RefItemCode = reqRec!.Value.RefItemCode?.Trim().ToUpper() ?? null;
			existingRec.RefMarketId = reqRec!.Value.RefMarketId?.Trim() ?? null;
			existingRec.TxDesc = reqRec!.Value.TxDesc?.Trim() ?? null;
		}
		existingRec = await PortfolioRepository.UpdateTxSettlementAsync(existingRec);
		if (existingRec == null)
		{
			return ResponseNoData(500, "Failed to update ROI record.");
		}
		return ResponseOk(TxSettlementResp.BuildFrom(existingRec));
	}

	/// <summary>
	/// Deletes an existing Settlement record from current user's portfolio.
	/// </summary>
	/// <param name="id">ID of the portfolio.</param>
	/// <param name="txid">ID of the ROI record.</param>
	/// <returns></returns>
	[HttpDelete(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_SETTLEMENT_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<TxSettlementResp>>> DeleteMyPortfolioTxSettlement([FromRoute] string id, [FromRoute] string txid)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var existingRec = await PortfolioRepository.GetTxSettlementByIdAsync(txid);
		if (existingRec == null)
		{
			return ResponseNoData(404, "ROI record not found.");
		}

		// validate portfolio, must be current user's portfolio
		var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
		if (!(existingPortfolio?.Id.Equals(existingRec.PortfolioId, StringComparison.OrdinalIgnoreCase)??false))
		{
			return ResponseNoData(400, "Portfolio not found or mismatched.");
		}

		if (TxSettlementEntity.ImmutableStatuses.Contains(existingRec.Status.ToUpper()))
		{
			return ResponseNoData(400, $"ROI record is in status '{existingRec.Status}' and cannot be deleted.");
		}
		else
		{
			var result = await PortfolioRepository.DeleteTxSettlementAsync(existingRec);
			if (!result)
			{
				return ResponseNoData(500, "Failed to delete ROI record.");
			}
			return ResponseOk(TxSettlementResp.BuildFrom(existingRec));
		}
	}

	/// <summary>
	/// Gets current user's portfolio PnL summary.
	/// </summary>
	/// <param name="id">ID of the portfolio to fetch PnL summary from.</param>
	/// <returns></returns>
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

		var pnlSummary = await PortfolioRepository.GetPnlSummaryForPortfolioAsync(id);
		return ResponseOk(PnlSummaryResp.BuildFrom(pnlSummary));
	}
}
