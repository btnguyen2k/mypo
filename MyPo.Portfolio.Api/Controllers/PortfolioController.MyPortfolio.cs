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
	/// Gets current user's portfolio records.
	/// </summary>
	/// <returns></returns>
	/// <remarks>The user is identified by the auth token.</remarks>
	[HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO)]
	public async ValueTask<ActionResult<ApiResp<List<PortfolioResp>>>> GetMyPortfolios()
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var result = new List<PortfolioResp>();
		var myPortfolioList = await PortfolioRepository.GetPortfoliosByUserAsync(currentUser);
		foreach (var portfolio in myPortfolioList)
		{
			result.Add(PortfolioResp.BuildFrom(portfolio));
		}
		return ResponseOk(result);
	}

	/// <summary>
	/// Creates a new portfolio record for the current user.
	/// </summary>
	/// <param name="req">The create portfolio request.</param>
	/// <returns>The created portfolio record.</returns>
	/// <remarks>The user is identified by the auth token.</remarks>
	[HttpPost(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<PortfolioResp>>> CreatePortfolio([FromBody] CreateOrUpdatePortfolioReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		// validate name and currency
		if (string.IsNullOrWhiteSpace(req.Name))
		{
			return ResponseNoData(400, "Name is required.");
		}
		if (string.IsNullOrWhiteSpace(req.Currency))
		{
			return ResponseNoData(400, "Currency is required.");
		}

		// validate parent id
		var parentId = req.ParentId?.Trim();
		if (!string.IsNullOrWhiteSpace(parentId))
		{
			var parentPortfolio = await PortfolioRepository.GetPortfolioByIdAsync(parentId);
			if (parentPortfolio == null || !parentPortfolio.OwnerUserId.Equals(currentUser.Id, StringComparison.OrdinalIgnoreCase))
			{
				return ResponseNoData(400, "Invalid parent portfolio ID.");
			}

			// TODO portfolio tree depth limit check!
		}

		// Create new portfolio record
		var portfolioRec = new PortfolioEntity
		{
			// Id = Guid.NewGuid().ToString(),
			ParentId = string.IsNullOrEmpty(parentId) ? null : parentId,
			Name = req.Name.Trim(),
			Description = req.Description?.Trim(),
			Currency = req.Currency.ToUpper().Trim(),
			OwnerUserId = currentUser.Id,
			IsActive = true,
			Metadata = req.Metadata,
		};
		var result = await PortfolioRepository.CreatePortfolioAsync(portfolioRec);
		if (result == null)
		{
			return ResponseNoData(500, "Failed to create new portfolio.");
		}
		return ResponseOk(PortfolioResp.BuildFrom(result));
	}

	[HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<PortfolioResp>>> UpdateMyPortfolio([FromRoute] string id, [FromBody] CreateOrUpdatePortfolioReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		// validate name and currency
		if (string.IsNullOrWhiteSpace(req.Name))
		{
			return ResponseNoData(400, "Name is required.");
		}
		if (string.IsNullOrWhiteSpace(req.Currency))
		{
			return ResponseNoData(400, "Currency is required.");
		}

		var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
		if (existingPortfolio == null)
		{
			return ResponseNoData(404, "Portfolio not found.");
		}

		// validate parent id
		var parentId = req.ParentId?.Trim();
		if (!string.IsNullOrWhiteSpace(parentId))
		{
			var parentPortfolio = await PortfolioRepository.GetPortfolioByIdAsync(parentId);
			if (parentPortfolio == null
				|| !parentPortfolio.OwnerUserId.Equals(currentUser.Id, StringComparison.OrdinalIgnoreCase)
				|| parentPortfolio.Id.Equals(existingPortfolio.Id, StringComparison.OrdinalIgnoreCase))
			{
				return ResponseNoData(400, "Invalid parent portfolio ID.");
			}

			// TODO portfolio tree depth limit check!
		}

		existingPortfolio.IsActive = req.IsActive;
		existingPortfolio.Name = req.Name.Trim();
		existingPortfolio.Description = req.Description?.Trim() ?? string.Empty;
		existingPortfolio.Currency = req.Currency.ToUpper().Trim();
		existingPortfolio.ParentId = string.IsNullOrEmpty(parentId) ? null : parentId;
		existingPortfolio.Metadata = req.Metadata;

		existingPortfolio = await PortfolioRepository.UpdatePortfolioAsync(existingPortfolio);
		if (existingPortfolio == null)
		{
			return ResponseNoData(500, $"Failed to update portfolio '{id}'.");
		}
		return ResponseOk(PortfolioResp.BuildFrom(existingPortfolio));
	}

	[HttpDelete(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<PortfolioResp>>> DeleteMyPortfolio([FromRoute] string id)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var myPortfolioList = await PortfolioRepository.GetPortfoliosByUserIdAsync(currentUser.Id);
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id == id);
		if (existingPortfolio == null)
		{
			return ResponseNoData(404, "Portfolio not found.");
		}

		var children = myPortfolioList.Where(p => p.ParentId == existingPortfolio.Id).ToList();
		if (children.Count > 0)
		{
			return ResponseNoData(409, "Cannot delete portfolio with child portfolios. Please delete or reassign the child portfolios first.");
		}

		var resultDelete = await PortfolioRepository.DeletePortfolioAsync(existingPortfolio);
		if (!resultDelete)
		{
			return ResponseNoData(500, $"Failed to delete portfolio '{id}'.");
		}
		return ResponseOk(PortfolioResp.BuildFrom(existingPortfolio));
	}
}
