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

	[HttpDelete(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_REC_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<RoiRecResp>>> DeleteRoiRec([FromRoute] string id, [FromRoute] string rid)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var existingRec = await PortfolioRepository.GetRoiRecByIdAsync(rid);
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

	private static (CreateOrUpdateRoiRecReq?, ObjectResult?) ValidateRoiRec(CreateOrUpdateRoiRecReq reqRec, RoiRec? existingRec)
	{
		// validate transaction type
		reqRec.TxType = reqRec.TxType.ToUpper().Trim();
		if (!RoiRec.TxTypes.Contains(reqRec.TxType))
		{
			return (null, ResponseNoData(400, $"Transaction type must be one of: {string.Join(", ", RoiRec.TxTypes)}, currently '{reqRec.TxType}'."));
		}

		// validate value, must be positive
		if (reqRec.TxValue <= 0.00m)
		{
			return (null, ResponseNoData(400, "Transaction value must be a positive value."));
		}

		// validate market, must be a valid one or empty
		reqRec.RefMarketId = reqRec.RefMarketId?.Trim() ?? null;
		if (!string.IsNullOrEmpty(reqRec.RefMarketId))
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

	[HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ROI_RECS)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<RoiRecResp>>> AddRoiRecToPortfolio([FromRoute] string id, [FromBody] CreateOrUpdateRoiRecReq req)
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
