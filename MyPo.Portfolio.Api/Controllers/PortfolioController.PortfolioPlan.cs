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
	/// Gets current user's portfolio plan records.
	/// </summary>
	/// <returns></returns>
	/// <remarks>The user is identified by the auth token.</remarks>
	[HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_PLANS)]
	public async ValueTask<ActionResult<ApiResp<List<PortfolioPlanResp>>>> GetMyPortfolioPlans()
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var result = new List<PortfolioPlanResp>();
		var myPortfolioPlanList = await PortfolioRepository.GetPortfolioPlansAccessibleByUserAsync(currentUser);
		foreach (var plan in myPortfolioPlanList)
		{
			result.Add(PortfolioPlanResp.BuildFrom(plan));
		}
		return ResponseOk(result);
	}

	/// <summary>
	/// Creates a new portfolio plan record for the current user.
	/// </summary>
	/// <param name="req">The create portfolio plan request.</param>
	/// <returns>The created portfolio plan record.</returns>
	/// <remarks>The user is identified by the auth token.</remarks>
	[HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_PORTFOLIO_PLANS)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<PortfolioPlanResp>>> CreatePortfolioPlan([FromBody] CreateOrUpdatePortfolioPlanReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		// validate linked portfolio
		if (!string.IsNullOrWhiteSpace(req.PortfolioId))
		{
			var portfolio = await PortfolioRepository.GetPortfolioByIdAsync(req.PortfolioId);
			if (portfolio == null || !portfolio.OwnerUserId.Equals(currentUser.Id, StringComparison.OrdinalIgnoreCase))
			{
				return ResponseNoData(400, "Invalid linked portfolio ID.");
			}
		}

		// validate name
		if (string.IsNullOrWhiteSpace(req.Name))
		{
			return ResponseNoData(400, "Name is required.");
		}

		// Create new portfolio plan record
		var portfolioPlanRec = new PortfolioPlanEntity
		{
			// Id = Guid.NewGuid().ToString(),
			OwnerUserId = currentUser.Id,
			PortfolioId = string.IsNullOrWhiteSpace(req.PortfolioId) ? null : req.PortfolioId.Trim(),
			Name = req.Name.Trim(),
			Metadata = req.Metadata,
		};
		var result = await PortfolioRepository.CreatePortfolioPlanAsync(portfolioPlanRec);
		if (result == null)
		{
			return ResponseNoData(500, "Failed to create new portfolio plan.");
		}
		return ResponseOk(PortfolioPlanResp.BuildFrom(result));
	}

	[HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_PORTFOLIO_PLAN_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<PortfolioPlanResp>>> UpdateMyPortfolioPlan([FromRoute] string id, [FromBody] CreateOrUpdatePortfolioPlanReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		// validate linked portfolio
		if (!string.IsNullOrWhiteSpace(req.PortfolioId))
		{
			var portfolio = await PortfolioRepository.GetPortfolioByIdAsync(req.PortfolioId);
			if (portfolio == null || !portfolio.OwnerUserId.Equals(currentUser.Id, StringComparison.OrdinalIgnoreCase))
			{
				return ResponseNoData(400, "Invalid linked portfolio ID.");
			}
		}

		// validate name
		if (string.IsNullOrWhiteSpace(req.Name))
		{
			return ResponseNoData(400, "Name is required.");
		}

		var existingPortfolioPlan = await GetPortfolioPlanIfOwnedByUser(currentUser, id);
		if (existingPortfolioPlan == null)
		{
			return ResponseNoData(404, "Portfolio plan not found.");
		}

		existingPortfolioPlan.PortfolioId = string.IsNullOrWhiteSpace(req.PortfolioId) ? null : req.PortfolioId.Trim();
		existingPortfolioPlan.Name = req.Name.Trim();
		existingPortfolioPlan.Metadata = req.Metadata;

		existingPortfolioPlan = await PortfolioRepository.UpdatePortfolioPlanAsync(existingPortfolioPlan);
		if (existingPortfolioPlan == null)
		{
			return ResponseNoData(500, $"Failed to update portfolio plan '{id}'.");
		}
		return ResponseOk(PortfolioPlanResp.BuildFrom(existingPortfolioPlan));
	}

	[HttpDelete(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_PORTFOLIO_PLAN_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<PortfolioPlanResp>>> DeleteMyPortfolioPlan([FromRoute] string id)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

			var existingPortfolioPlan = await GetPortfolioPlanIfOwnedByUser(currentUser, id);
		if (existingPortfolioPlan == null)
		{
			return ResponseNoData(404, "Portfolio plan not found.");
		}

		var resultDelete = await PortfolioRepository.DeletePortfolioPlanAsync(existingPortfolioPlan);
		if (!resultDelete)
		{
			return ResponseNoData(500, $"Failed to delete portfolio plan '{id}'.");
		}
		return ResponseOk(PortfolioPlanResp.BuildFrom(existingPortfolioPlan));
	}
}
