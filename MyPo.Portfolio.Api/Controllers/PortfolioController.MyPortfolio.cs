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
	public async Task<ActionResult<ApiResp<List<PortfolioRecResp>>>> GetMyPortfolio()
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var result = new List<PortfolioRecResp>();
		var myPortfolioList = await PortfolioRepository.GetPortfolioByUserIdAsync(currentUser.Id);
		foreach (var portfolio in myPortfolioList)
		{
			result.Add(PortfolioRecResp.BuildFrom(portfolio));
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
	public async Task<ActionResult<ApiResp<PortfolioRecResp>>> CreatePortfolio([FromBody] CreateOrUpdatePortfolioRecReq req)
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

		// Create new portfolio record
		var portfolioRec = new PortfolioRec
		{
			// Id = Guid.NewGuid().ToString(),
			ParentId = req.ParentId?.Trim(),
			Name = req.Name.Trim(),
			Description = req.Description?.Trim(),
			Currency = req.Currency.ToUpper().Trim(),
			OwnerUserId = currentUser.Id,
			IsActive = true,

		};
		var result = await PortfolioRepository.CreatePortfolioAsync(portfolioRec);
		if (result == null)
		{
			return ResponseNoData(500, "Failed to create new portfolio.");
		}
		return ResponseOk(PortfolioRecResp.BuildFrom(result));
	}

	[HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async Task<ActionResult<ApiResp<PortfolioRecResp>>> UpdateMyPortfolio([FromRoute] string id, [FromBody] CreateOrUpdatePortfolioRecReq req)
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

		var myPortfolioList = await PortfolioRepository.GetPortfolioByUserIdAsync(currentUser.Id);
		var existingPortfolio = myPortfolioList.FirstOrDefault(p => p.Id == id);
		if (existingPortfolio == null)
		{
			return ResponseNoData(404, "Portfolio not found.");
		}

		existingPortfolio.IsActive = req.IsActive;
		existingPortfolio.Name = req.Name.Trim();
		existingPortfolio.Description = req.Description?.Trim() ?? string.Empty;
		existingPortfolio.Currency = req.Currency.ToUpper().Trim();

		existingPortfolio = await PortfolioRepository.UpdatePortfolioAsync(existingPortfolio);
		if (existingPortfolio == null)
		{
			return ResponseNoData(500, $"Failed to update portfolio '{id}'.");
		}
		return ResponseOk(PortfolioRecResp.BuildFrom(existingPortfolio));
	}

	[HttpDelete(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async Task<ActionResult<ApiResp<PortfolioRecResp>>> DeleteMyPortfolio([FromRoute] string id)
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
		return ResponseOk(PortfolioRecResp.BuildFrom(existingPortfolio));
	}
}
