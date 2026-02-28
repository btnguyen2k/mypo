using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioAssets : CBase
{
	[Parameter]
	public IEnumerable<AssetResp>? Assets { get; set; }
	private Dictionary<string, AssetResp> AssetsMap => Assets?.ToDictionary(t => t.Id, t => t) ?? [];
	private AssetResp? SelectedAsset;
	private decimal TotalCost => Assets?.Where(a => a.Market?.Currency==Portfolio?.Currency).Sum(a => a.AveragePrice*a.Quantity) ?? 0;
	private decimal TotalMarketValue => Assets?.Where(a => a.Market?.Currency==Portfolio?.Currency).Sum(a =>
	{
		if (MarketPricesMap.TryGetValue(a.Id, out var latestPrice))
		{
			return latestPrice * a.Quantity;
		}
		return 0;
	}) ?? 0;

	[Parameter]
	public Dictionary<string, StockQuote>? QuotesMap { get; set; } // map {asset-id --> quote}

	private Dictionary<string, decimal> MarketPricesMap { get; set; } = []; // map {asset-id --> market-price}

	private Dictionary<string, decimal> UnsettledPnLMap { get; set; } = []; // map {asset-id --> unsettled-p/l}

	private Dictionary<string, decimal> UnsettledPnLPercentMap { get; set; } = []; // map {asset-id --> unsettled-p/l-percent}

	private string AssetTags = string.Empty;

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	[Parameter]
	public PortfolioResp? Portfolio { get; set; }
	private MarketDefResp? DefaultMarket => Markets?.FirstOrDefault(m => m.Id == Portfolio?.Metadata?.DefaultMarketId);

	private CModal ModalDialogAssetUpdateTags { get; set; } = default!;
	private CModal ModalDialogBuySellAssetCalculator { get; set; } = default!;

	private int MarketStatus(AssetResp? asset)
	{
		return asset == null || asset.Quantity <= 0
			? 0
			: MarketPricesMap.TryGetValue(asset.Id, out var lp)
				? lp == asset.AveragePrice
					? 0
					: lp > asset.AveragePrice
						? 1
						: -1
				: 0;
	}

	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();
		if (Assets != null && Markets != null && QuotesMap != null)
		{
			foreach (var asset in Assets ?? [])
			{
				var symbolKey = $"{asset.ItemCode}:{asset.MarketId}".ToUpper();
				if (QuotesMap.TryGetValue(symbolKey, out var quote))
				{
					var latestPrice = quote.MarketPrice;
					latestPrice /= (asset.Market?.PriceScale != 0 ? asset.Market?.PriceScale : 1) ?? 1;
					MarketPricesMap[asset.Id] = latestPrice;
					var unsettledPnL = (latestPrice - asset.AveragePrice) * asset.Quantity;
					UnsettledPnLMap[asset.Id] = unsettledPnL;
					UnsettledPnLPercentMap[asset.Id] = PortfolioUtils.CalculatePercentageChange(
						asset.AveragePrice,
						latestPrice
					);
				}
			}
			StateHasChanged();
		}
	}

	private void BtnClickAssetInfo(string assetId)
	{
		SelectedAsset = AssetsMap.TryGetValue(assetId, out var asset) ? asset : null;
		if (SelectedAsset == null)
		{
			ShowAlert("danger", "Asset not found.");
			return;
		}
		SelectedAsset.Market = Markets?.FirstOrDefault(m => m.Id == SelectedAsset.MarketId);
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_STOCK_SYMBOL_INFO.Replace("{Symbol}", $"{SelectedAsset.ItemCode}:{SelectedAsset.Market?.Id}", StringComparison.OrdinalIgnoreCase)}"
			+ $"?pid={SelectedAsset.PortfolioId}";
		NavigationManager.NavigateTo(nextUrl);
	}

	private void BtnClickAssetBuySellCalculator(string assetId)
	{
		SelectedAsset = AssetsMap.TryGetValue(assetId, out var asset) ? asset : null;
		if (SelectedAsset != null)
		{
			ModalDialogBuySellAssetCalculator.CloseAlert();
			ModalDialogBuySellAssetCalculator.Open();
		}
	}

	private void BtnClickAssetUpdateTags(string assetId)
	{
		SelectedAsset = AssetsMap.TryGetValue(assetId, out var asset) ? asset : null;
		if (SelectedAsset == null)
		{
			ShowAlert("danger", "Asset not found.");
			return;
		}
		AssetTags = string.Join(", ", SelectedAsset.Metadata?.Tags ?? new HashSet<string>());
		ModalDialogAssetUpdateTags.CloseAlert();
		ModalDialogAssetUpdateTags.Open();
	}

	private async void BtnClickAssetUpdateTagsConfirmed()
	{
		var req = new CreateOrUpdateAssetReq()
		{
			Id = SelectedAsset!.Id,
			PortfolioId = SelectedAsset!.PortfolioId,
			ItemType = SelectedAsset!.ItemType,
			ItemCode = SelectedAsset!.ItemCode,
			Quantity = SelectedAsset!.Quantity,
			AveragePrice = SelectedAsset!.AveragePrice,
			MarketId = SelectedAsset!.MarketId,
			Metadata = SelectedAsset!.Metadata ?? new AssetMetadata(),
		};
		req.Metadata.Tags = AssetTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);

		ModalDialogAssetUpdateTags.ShowAlert("info", "Updating asset tags...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioAssetAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogAssetUpdateTags.ShowAlert("danger", resp.Message ?? "Error updating asset tags.");
			return;
		}
		ModalDialogAssetUpdateTags.ShowAlert("success", "Asset tags updated successfully.");
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogAssetUpdateTags.Close();
		var passAlertMessage = $"{SelectedAsset!.ItemCode}'s tags updated successfully.";
		var passAlertType = "success";
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio?.Id, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
