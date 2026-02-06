using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioAssets : CBase
{
	[Parameter]
	public IEnumerable<AssetResp>? Assets { get; set; }
	private Dictionary<string, AssetResp> AssetsMap => Assets?.ToDictionary(t => t.Id, t => t) ?? [];
	private AssetResp? SelectedAsset;
	private decimal TotalCost => Assets?.Where(a => a.Market?.Currency==Portfolio?.Currency).Sum(a => a.AveragePrice*a.Quantity) ?? 0;
	private decimal TotalMarketValue => Assets?.Where(a => a.Market?.Currency==Portfolio?.Currency).Sum(a =>
	{
		if (LatestPricesMap.TryGetValue(a.Id, out var latestPrice))
		{
			return latestPrice * a.Quantity;
		}
		return 0;
	}) ?? 0;
	private Dictionary<string, StockQuote> QuotesMap = []; // map {asset-id --> quote}
	private readonly Dictionary<string, decimal> LatestPricesMap = [];
	private readonly Dictionary<string, decimal> UnsettledPnLMap = [];
	private readonly Dictionary<string, decimal> UnsettledPnLPercentMap = [];

	private string AssetTags = string.Empty;

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	[Parameter]
	public PortfolioResp? Portfolio { get; set; }
	private MarketDefResp? DefaultMarket => Markets?.FirstOrDefault(m => m.Id == Portfolio?.Metadata?.DefaultMarketId);

	private CModal ModalDialogAssetUpdateTags { get; set; } = default!;
	private CModal ModalDialogBuySellAssetCalculator { get; set; } = default!;

	private bool StopRefreshQuotes { get; set; } = false;

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			StopRefreshQuotesBackground();
		}
		base.Dispose(disposing);
	}

	private void StopRefreshQuotesBackground()
	{
		StopRefreshQuotes = true;
	}

	private async Task GetStocksQuotesBackground(List<string> symbolsList)
	{
		if (symbolsList.Count > 0 && !StopRefreshQuotes)
		{
			var symbols = string.Join(",",symbolsList);
			Console.WriteLine($"[DEBUG] Fetching quotes for symbols: {symbols}");
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var quotesResp = await apiClient.GetStocksQuotesAsync(symbols, await GetAuthTokenAsync(), ApiBaseUrl);
			if (quotesResp.Status == 200)
			{
				QuotesMap = quotesResp.Data?.ToDictionary(q => q.Key, q => q.Value) ?? [];
				foreach (var asset in Assets ?? [])
				{
					var symbolKey = $"{asset.ItemCode}:{asset.MarketId}".ToUpper();
					if (QuotesMap.TryGetValue(symbolKey, out var quote))
					{
						var latestPrice = quote.MarketPrice ?? 0;
						latestPrice /= (asset.Market?.PriceScale != 0 ? asset.Market?.PriceScale : 1) ?? 1;
						LatestPricesMap[asset.Id] = latestPrice;
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
			else
			{
				Console.WriteLine($"[ERROR] Failed to fetch quotes for symbols: {symbols}. Status: {quotesResp.Status}, Message: {quotesResp.Message}");
			}
			if (!StopRefreshQuotes)
			{
				var sleepTime = Random.Shared.NextInt64(60000, 180000);
				Console.WriteLine($"[DEBUG] Sleeping {sleepTime} ms before next quotes refresh...");
				await Task.Delay((int)sleepTime);
				await Task.Run(async () => await GetStocksQuotesBackground(symbolsList));
			}
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender && Assets != null && Markets != null && Portfolio != null)
		{
			var symbolsList = Assets.Select(a => $"{a.ItemCode}:{a.MarketId}").ToList() ?? [];
			await Task.Run(async () => await GetStocksQuotesBackground(symbolsList));
		}
	}

	private int MarketStatus(AssetResp? asset)
	{
		return asset == null || asset.Quantity <= 0
			? 0
			: LatestPricesMap.TryGetValue(asset.Id, out var lp)
				? lp == asset.AveragePrice
					? 0
					: lp > asset.AveragePrice
						? 1
						: -1
				: 0;
	}

	private void BtnClickAssetBuySellCalculator(string assetId)
	{
		SelectedAsset = AssetsMap.TryGetValue(assetId, out var asset) ? asset : null;
		if (SelectedAsset != null)
		{
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
		AssetTags = SelectedAsset.Tags ?? string.Empty;
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
			Tags = AssetTags,
		};

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
