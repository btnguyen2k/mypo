using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
	private static (CreateOrUpdateRoiRecReq?, ObjectResult?) ValidateRoiRec(CreateOrUpdateRoiRecReq reqRec, RoiRec? existingRec)
	{
		var isImmutable = existingRec != null && RoiRec.ImmutableStatuses.Contains(existingRec.Status.ToUpper());

		// validate transaction type
		reqRec.TxType = reqRec.TxType.ToUpper().Trim();
		if (!isImmutable && !RoiRec.TxTypes.Contains(reqRec.TxType))
		{
			return (null, ResponseNoData(400, $"Transaction type must be one of: {string.Join(", ", RoiRec.TxTypes)}, currently '{reqRec.TxType}'."));
		}

		// validate value, must be positive
		if (!isImmutable && reqRec.TxValue <= 0.00m)
		{
			return (null, ResponseNoData(400, "Transaction value must be a positive value."));
		}

		// validate market, must be a valid one or empty
		reqRec.RefMarketId = reqRec.RefMarketId?.Trim() ?? null;
		if (!isImmutable && !string.IsNullOrEmpty(reqRec.RefMarketId))
		{
			var market = Globals.MarketsMap.TryGetValue(reqRec.RefMarketId.ToUpper(), out var mkt) ? mkt : null;
			if (market == null)
			{
				return (null, ResponseNoData(400, $"Market '{reqRec.RefMarketId}' is not recognized."));
			}
			// shift the transaction's timezone to market's timezone
			reqRec.TxTime = new DateTimeOffset(reqRec.TxTime.DateTime, market.TZ.BaseUtcOffset);
		}
		reqRec.TxTime = reqRec.TxTime.ToUniversalTime(); // convert to UTC for storing into database

		return (reqRec, null);
	}

	/// <summary>
	/// Creates a new ROI record and Adds it to current user's portfolio.
	/// </summary>
	/// <param name="id">ID of the portfolio to add ROI record to.</param>
	/// <param name="req">Details of the ROI record to add.</param>
	/// <returns></returns>
	[HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_RECS)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<RoiRecResp>>> AddRoiRecToMyPortfolio([FromRoute] string id, [FromBody] CreateOrUpdateRoiRecReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var (reqRoiRec, validationResult) = ValidateRoiRec(req, null);
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

		var rec = new RoiRec
		{
			Status = RoiRec.STATUS_NEW,
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
		var result = await PortfolioRepository.CreateRoiRecAsync(rec);
		if (result == null)
		{
			return ResponseNoData(500, "Failed to create ROI record.");
		}
		return ResponseOk(RoiRecResp.BuildFrom(rec));
	}

	/// <summary>
	/// Updates an existing ROI record from current user's portfolio.
	/// </summary>
	/// <param name="id">ID of the portfolio.</param>
	/// <param name="rrid">ID of the ROI record.</param>
	/// <param name="req">Details of the ROI record to update.</param>
	/// <returns></returns>
	[HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_REC_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<TransactionRecResp>>> UpdateMyPortfolioRoiRec([FromRoute] string id, [FromRoute] string rrid, [FromBody] CreateOrUpdateRoiRecReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var existingRec = await PortfolioRepository.GetRoiRecByIdAsync(rrid);
		if (existingRec == null)
		{
			return ResponseNoData(404, "ROI record not found.");
		}
		var (reqRec, validationResult) = ValidateRoiRec(req, existingRec);
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

		if (RoiRec.ImmutableStatuses.Contains(existingRec.Status?.ToUpper()??string.Empty))
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
		existingRec = await PortfolioRepository.UpdateRoiRecAsync(existingRec);
		if (existingRec == null)
		{
			return ResponseNoData(500, "Failed to update ROI record.");
		}
		return ResponseOk(RoiRecResp.BuildFrom(existingRec));
	}

	/// <summary>
	/// Deletes an existing ROI record from current user's portfolio.
	/// </summary>
	/// <param name="id">ID of the portfolio.</param>
	/// <param name="rrid">ID of the ROI record.</param>
	/// <returns></returns>
	[HttpDelete(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_REC_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<RoiRecResp>>> DeleteMyPortfolioRoiRec([FromRoute] string id, [FromRoute] string rrid)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var existingRec = await PortfolioRepository.GetRoiRecByIdAsync(rrid);
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

		if (RoiRec.ImmutableStatuses.Contains(existingRec.Status.ToUpper()))
		{
			return ResponseNoData(400, $"ROI record is in status '{existingRec.Status}' and cannot be deleted.");
		}
		else
		{
			var result = await PortfolioRepository.DeleteRoiRecAsync(existingRec);
			if (!result)
			{
				return ResponseNoData(500, "Failed to delete ROI record.");
			}
			return ResponseOk(RoiRecResp.BuildFrom(existingRec));
		}
	}

	/// <summary>
	/// Gets current user's portfolio ROI records.
	/// </summary>
	/// <param name="id">ID of the portfolio to fetch ROI records from.</param>
	/// <returns></returns>
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

		var pnlSummary = await PortfolioRepository.GetRoiSummaryForPortfolioAsync(id);
		return ResponseOk(PnlSummaryResp.BuildFrom(pnlSummary));
	}
}
