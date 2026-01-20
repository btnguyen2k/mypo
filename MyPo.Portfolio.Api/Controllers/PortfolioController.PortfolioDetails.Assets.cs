using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Identity;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

public partial class PortfolioController
{
	/// <summary>
	/// Gets current user's portfolio assets.
	/// </summary>
	[HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ASSETS)]
	public async ValueTask<ActionResult<ApiResp<IEnumerable<TransactionRecResp>>>> GetMyPortfolioAssets([FromRoute] string id)
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

		var assetList = await PortfolioRepository.GetAssetsByPortfolioIdAsync(id);
		var result = new List<AssetResp>();
		foreach (var asset in assetList)
		{
			var market = Globals.MarketsMap.TryGetValue(asset.MarketId?.ToUpper()??string.Empty, out var mkt) ? mkt : null;
			result.Add(AssetResp.BuildFrom(asset, market));
		}
		return ResponseOk(result);
	}

	[HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ASSET_ID)]
	[Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
	public async ValueTask<ActionResult<ApiResp<AssetResp>>> UpdateAssetFromPortfolio([FromRoute] string id, [FromRoute] string aid, [FromBody] CreateOrUpdateAssetReq req)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var existingAsset = await PortfolioRepository.GetAssetAsync(aid);
		if (existingAsset == null)
		{
			return ResponseNoData(404, "Asset not found.");
		}
		// var (reqTx, validationResult) = ValidateAsset(req, existingAsset);
		// if (validationResult != null)
		// {
		// 	return validationResult;
		// }

		// validate portfolio, must be current user's portfolio
		var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
		if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase)??false))
		{
			return ResponseNoData(400, "Portfolio not found or mismatched.");
		}

		// only tags list can be updated for asset
		var tagsSet = (req.Tags?.Trim() ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
		existingAsset.Tags = tagsSet.Count > 0 ? string.Join(", ", tagsSet) : string.Empty;

		existingAsset = await PortfolioRepository.UpdateAssetAsync(existingAsset);
		if (existingAsset == null)
		{
			return ResponseNoData(500, "Failed to update asset.");
		}
		return ResponseOk(AssetResp.BuildFrom(existingAsset));
	}
}
