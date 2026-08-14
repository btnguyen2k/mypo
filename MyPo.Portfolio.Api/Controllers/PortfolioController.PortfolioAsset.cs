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
    /// Gets current user's portfolio assets.
    /// </summary>
    /// <param name="id">ID of the portfolio to get assets from.</param>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ASSETS)]
    public async ValueTask<ActionResult<ApiResp<IEnumerable<TxBuySellResp>>>> GetMyPortfolioAssets([FromRoute] string id)
    {
        var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
        if (authErrorResult != null)
        {
            // current auth token and signed-in user should all be valid
            return authErrorResult;
        }

        // validate portfolio
        var existingPortfolio = await GetPortfolioIfAccessible(currentUser, id);
        if (existingPortfolio == null)
        {
            return ResponseNoData(404, "Portfolio not found or not accessible.");
        }

        var assetList = await PortfolioRepository.GetAssetsByPortfolioIdAsync(id);
        var result = new List<AssetResp>();
        foreach (var asset in assetList)
        {
            var market = Globals.MarketsMap.TryGetValue(asset.MarketId?.ToUpper() ?? string.Empty, out var mkt) ? mkt : null;
            result.Add(AssetResp.BuildFrom(asset, market));
        }
        return ResponseOk(result);
    }

    /// <summary>
    /// Updates an asset in current user's portfolio (only asset tags can be updated!).
    /// </summary>
    /// <param name="id">ID of the portfolio to update asset in.</param>
    /// <param name="aid">ID of the asset to update.</param>
    /// <param name="req">Request body containing updated asset details.</param>
    /// <returns></returns>
    [HttpPut(IPortfolioApiClient.API_PORTFOLIO_ENDPOINT_MY_PORTFOLIO_ID_ASSET_ID)]
    [Authorize(Policy = PortfolioPolicies.POLICY_NAME_ADMIN_ROLE_OR_PORTFOLIO_MANAGER)]
    public async ValueTask<ActionResult<ApiResp<AssetResp>>> UpdateMyPortfolioAsset([FromRoute] string id, [FromRoute] string aid, [FromBody] CreateOrUpdateAssetReq req)
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

        // validate portfolio, must be current user's portfolio
        var existingPortfolio = await GetPortfolioIfOwnedByUser(currentUser, id);
        if (!(existingPortfolio?.Id.Equals(req.PortfolioId, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return ResponseNoData(400, "Portfolio not found or asset does not belong to the portfolio.");
        }

        // only asset's metadata can be updated
        existingAsset.Metadata ??= new AssetMetadata();
        existingAsset.Metadata.CorpName = req.Metadata?.CorpName ?? existingAsset.Metadata.CorpName;
        existingAsset.Metadata.Industry = req.Metadata?.Industry ?? existingAsset.Metadata.Industry;
        existingAsset.Metadata.Sector = req.Metadata?.Sector ?? existingAsset.Metadata.Sector;
        existingAsset.Metadata.AssetType = req.Metadata?.AssetType ?? existingAsset.Metadata.AssetType;

        // update tags
        existingAsset.Metadata.Tags ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (req.Metadata?.Tags is not null)
        {
            existingAsset.Metadata.Tags = req.Metadata.Tags;
        }
        // sort tags alphabetically for better readability
        existingAsset.Metadata.Tags = new SortedSet<string>(existingAsset.Metadata.Tags);

        existingAsset = await PortfolioRepository.UpdateAssetAsync(existingAsset);
        if (existingAsset == null)
        {
            return ResponseNoData(500, "Failed to update asset.");
        }
        return ResponseOk(AssetResp.BuildFrom(existingAsset));
    }
}
