using Finance.Net.Models.Yahoo;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioAssets : CBase
{
	[Parameter]
	public IEnumerable<AssetResp>? Assets { get; set; }
	private Dictionary<string, AssetResp> AssetsMap => Assets?.ToDictionary(t => t.Id, t => t) ?? [];
	private decimal TotalCost => Assets?.Where(a => a.Market?.Currency==Portfolio?.Currency).Sum(a => a.AveragePrice*(decimal)a.Quantity) ?? 0;
	private decimal TotalMarketValue => Assets?.Where(a => a.Market?.Currency==Portfolio?.Currency).Sum(a =>
	{
		if (LatestPricesMap.TryGetValue(a.Id, out var latestPrice))
		{
			return latestPrice * (decimal)a.Quantity;
		}
		return 0;
	}) ?? 0;
	private MarketDefResp? DefaultMarket => Assets?.FirstOrDefault(a => a.Market!=null).Market;
	private Dictionary<string, Quote> QuotesMap = []; // map {asset-id --> quote}
	private readonly Dictionary<string, decimal> LatestPricesMap = [];
	private readonly Dictionary<string, decimal> UnsettledPnLMap = [];
	private readonly Dictionary<string, decimal> UnsettledPnLPercentMap = [];

	private AssetResp? SelectedAsset;
	private MarketDef? SelectedMarket;
	private string AssetTags = string.Empty;

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;
	private PortfolioRecResp? Portfolio { get; set; }

	private CModal ModalDialogAssetInfo { get; set; } = default!;

	private bool StopRefreshQuotes = false;

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
						var latestPrice = (decimal)(quote.RegularMarketPrice ?? 0);
						latestPrice /= (asset.Market?.PriceScale != 0 ? asset.Market?.PriceScale : 1) ?? 1;
						LatestPricesMap[asset.Id] = latestPrice;
						var unsettledPnL = (latestPrice - asset.AveragePrice) * (decimal)asset.Quantity;
						UnsettledPnLMap[asset.Id] = unsettledPnL;
						UnsettledPnLPercentMap[asset.Id] = PortfolioUtils.CalculatePercentageChange(
							asset.AveragePrice,
							latestPrice
						);
					}
				}
				StateHasChanged();
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
		if (firstRender && Assets != null && Markets != null && !string.IsNullOrEmpty(PortfolioId))
		{
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var myPortfolios = await apiClient.GetMyPortfolioAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			Portfolio = myPortfolios.Data?.FirstOrDefault(p => p.Id == PortfolioId) ?? null;

			var symbolsList = Assets?.Select(a => $"{a.ItemCode}:{a.MarketId}").ToList() ?? [];
			await Task.Run(async () => await GetStocksQuotesBackground(symbolsList));
		}
	}

	private async void BtnClickAssetInfo(string assetId)
	{
		SelectedAsset = AssetsMap.TryGetValue(assetId, out var asset) ? asset : null;
		if (SelectedAsset != null)
		{
			SelectedMarket = Markets?.FirstOrDefault(m => m.Id == SelectedAsset!.Value.MarketId).ToModel() ?? null;
			AssetTags = SelectedAsset?.Tags ?? string.Empty;
			ModalDialogAssetInfo.Open();
			await Task.CompletedTask;
		}
	}

	private async void BtnClickUpdateAssetTags()
	{
		var req = new CreateOrUpdateAssetReq()
		{
			Id = SelectedAsset!.Value.Id,
			PortfolioId = SelectedAsset!.Value.PortfolioId,
			ItemType = SelectedAsset!.Value.ItemType,
			ItemCode = SelectedAsset!.Value.ItemCode,
			Quantity = SelectedAsset!.Value.Quantity,
			AveragePrice = SelectedAsset!.Value.AveragePrice,
			MarketId = SelectedAsset!.Value.MarketId,
			Tags = AssetTags,
		};

		ModalDialogAssetInfo.ShowAlert("info", "Updating asset tags...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioAssetAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogAssetInfo.ShowAlert("danger", resp.Message!);
			return;
		}
		ModalDialogAssetInfo.ShowAlert("success", "Asset tags updated successfully.");
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogAssetInfo.Close();
		var passAlertMessage = $"{SelectedAsset!.Value.ItemCode}'s tags updated successfully.";
		var passAlertType = "success";
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
