using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using Microsoft.JSInterop;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Libs.Opurator;
using MyPo.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Blazor.Portfolio.App.Shared;
using System.Text.Json;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioDetails : BasePage
{
	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;
	private PortfolioResp? SelectedPortfolio { get; set; }

	private IEnumerable<MarketDefResp>? Markets { get; set; }
	private IEnumerable<TxBuySellResp>? TxBuySells { get; set; }
	private IEnumerable<AssetResp>? Assets { get; set; }
	private IEnumerable<TxSettlementResp>? TxSettlements { get; set; }

	// map {asset-id --> quote}
	private readonly Dictionary<string, StockQuote> QuotesMap = [];

	private int RefreshBackgroundTaskId = Random.Shared.Next();

	private void StopRefreshQuotesBackground()
	{
		RefreshBackgroundTaskId = 0;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			StopRefreshQuotesBackground();
		}
		base.Dispose(disposing);
	}

	private async void GetStocksQuotesBackground(List<string> symbolsList, int myTaskId)
	{
		var hasError = false;

		void prefetchCallback(IEnumerable<string> symbols)
		{
			SetBackgroundMsg($"⌛Fetching quotes for symbols: {string.Join(",", symbols)}");
		}

		bool postfetchCallback(ApiResp<IDictionary<string, StockQuote>> quotesResp)
		{
			var emptyDict = new Dictionary<string, StockQuote>();
			if (!quotesResp.IsSuccess)
			{
				hasError = true;
				SetBackgroundMsg($"❗Failed to fetch quotes for symbols: {string.Join(",", symbolsList)}. Status: {quotesResp.Status}, Message: {quotesResp.Message}");
			}
			else
			{
				foreach (var quote in quotesResp.Data ?? emptyDict)
				{
					QuotesMap[quote.Key] = quote.Value;
				}
				StateHasChanged();
			}
			return myTaskId == RefreshBackgroundTaskId;
		}

		if (symbolsList.Count > 0 && myTaskId == RefreshBackgroundTaskId)
		{
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var authToken = await GetAuthTokenAsync();
			await TickerUtils.FetchQuotesForTickers(symbolsList, apiClient, authToken, ApiBaseUrl, prefetchCallback, postfetchCallback);

			if (!hasError)
			{
				// calculate base cost and market value for the portfolio
				var baseCost = Assets?
					.Where(a => a.Market?.Currency.Equals(SelectedPortfolio?.Currency, StringComparison.OrdinalIgnoreCase)??false)
					.Sum(a => a.AveragePrice*a.Quantity) ?? 0;
				var marketValue = Assets?
					.Where(a => a.Market?.Currency.Equals(SelectedPortfolio?.Currency, StringComparison.OrdinalIgnoreCase)??false)
					.Sum(a => {
						var m = Markets?.FirstOrDefault(m => string.Equals(m.Id, a.MarketId, StringComparison.OrdinalIgnoreCase));
						var symbol = $"{m?.Code??string.Empty}:{a.ItemCode}";
						return (QuotesMap.TryGetValue(symbol, out var quote) ? quote.MarketPrice : 0) * a.Quantity;
					}) ?? 0;
				// TODO
			}

			if (myTaskId == RefreshBackgroundTaskId)
			{
				var sleepTime = Random.Shared.NextInt64(30*1000, 60*1000);
				var market = Markets?.FirstOrDefault(m => string.Equals(m.Id, SelectedPortfolio?.Metadata?.DefaultMarketId, StringComparison.OrdinalIgnoreCase))?.ToModel();
				if (market == null)
				{
					SetBackgroundMsg($"❗Default market '{SelectedPortfolio?.Metadata?.DefaultMarketId}' not found in markets metadata. Not refreshing quotes.");
					return;
				}
				if (!market.IsCurrentlyOpen())
				{
					var timeTillOpen = market.TimeTillOpen();
					if (timeTillOpen > TimeSpan.FromMinutes(60))
					{
						SetBackgroundMsg($"❗Market '{market.Id}' is currently closed. Not refreshing.");
						return;
					}
					sleepTime = Random.Shared.NextInt64(5*60*1000, 10*60*1000);
					sleepTime = Math.Min(sleepTime, (long)timeTillOpen.TotalMilliseconds)+1000;
				}
				while (sleepTime > 0 && myTaskId == RefreshBackgroundTaskId)
				{
					SetBackgroundMsg($"💤Sleeping {sleepTime/1000} seconds before next quotes refresh...");
					var delay = Math.Min(sleepTime, 1000);
					await Task.Delay((int)delay);
					sleepTime -= delay;
				}
				if (myTaskId == RefreshBackgroundTaskId)
				{
					await Task.Run(() => GetStocksQuotesBackground(symbolsList, myTaskId));
				}
			}
		}
	}

	private async Task<PortfolioResp?> LoadPortfolioAsync(string id, string authToken)
	{
		HideUI = true;
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();

		ShowAlert("info", "Loading portfolio, please wait...");
		var apiRespPortfolio = await apiClient.GetMyPortfoliosAsync(authToken, ApiBaseUrl);
		if (apiRespPortfolio.Status != 200)
		{
			ShowAlert("danger", apiRespPortfolio.Message ?? "Error while loading portfolio.");
			return null;
		}

		var myPortfolioMap = (apiRespPortfolio.Data ?? []).ToDictionary(p => p.Id);
		if (!myPortfolioMap.TryGetValue(id, out var portfolio))
		{
			ShowAlert("danger", $"Portfolio '{id}' not found.");
			return null;
		}

		ShowAlert("info", "Loading portfolio buy/sell transactions, please wait...");
		var apiRespTx = await apiClient.GetMyPortfolioTxBuySellsAsync(portfolio.Id, authToken, ApiBaseUrl);
		if (apiRespTx.Status != 200)
		{
			ShowAlert("danger", apiRespTx.Message ?? $"Error while loading portfolio buy/sell transactions.");
			return null;
		}
		TxBuySells = apiRespTx.Data ?? [];

		ShowAlert("info", "Loading portfolio assets, please wait...");
		var apiRespAssets = await apiClient.GetMyPortfolioAssetsAsync(portfolio.Id, authToken, ApiBaseUrl);
		if (apiRespAssets.Status != 200)
		{
			ShowAlert("danger", apiRespAssets.Message ?? $"Error while loading portfolio assets.");
			return null;
		}
		Assets = apiRespAssets.Data ?? [];

		ShowAlert("info", "Loading portfolio settlement records, please wait...");
		var apiRespRoiRecs = await apiClient.GetMyPortfolioTxSettlementsAsync(portfolio.Id, authToken, ApiBaseUrl);
		if (apiRespRoiRecs.Status != 200)
		{
			ShowAlert("danger", apiRespRoiRecs.Message ?? $"Error while loading portfolio settlement records.");
			return null;
		}
		TxSettlements = apiRespRoiRecs.Data ?? [];

		return portfolio;
	}

	private async Task<IEnumerable<MarketDefResp>?> LoadMarketsAsync(string authToken)
	{
		ShowAlert("info", "Loading markets metadata...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var result = await apiClient.GetMarketsAsync(authToken, ApiBaseUrl);
		if (result.Status == 200)
		{
			return result.Data ?? [];
		}
		ShowAlert("danger", result.Message ?? "Unknown error while loading markets metadata.");
		return null;
	}

	private async void AutoPopulateAssetMetadata()
	{
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		foreach (var asset in (Assets ?? []).Where(a => a.Metadata==null || string.IsNullOrEmpty(a.Metadata.CorpName)
				|| string.IsNullOrEmpty(a.Metadata.Industry) || string.IsNullOrEmpty(a.Metadata.Industry)
				|| a.Metadata.Tags == null || a.Metadata.Tags.Count == 0))
		{
			var symbol = $"{asset.ItemCode}:{asset.MarketId}";
			SetBackgroundMsg($"🔍Fetching overview info for asset '{symbol}'...");
			var apiResp = await apiClient.GetStockSymbolOverviewAsync(symbol, await GetAuthTokenAsync(), ApiBaseUrl);
			if (apiResp.Status != 200)
			{
				SetBackgroundMsg($"❗Failed to fetch overview info for asset '{symbol}'. Status: {apiResp.Status}, Message: {apiResp.Message}");
			}
			else
			{
				var overview = apiResp.Data;
				if (overview != null)
				{
					asset.Metadata ??= new AssetMetadata();
					asset.Metadata.CorpName = overview.LongName ?? overview.ShortName ?? "";
					asset.Metadata.Industry = overview.Industry ?? "";
					asset.Metadata.Sector = overview.Sector ?? "";
					asset.Metadata.Tags ??= new HashSet<string>();
					if (!string.IsNullOrEmpty(asset.Metadata.Industry))
					{
						asset.Metadata.Tags.Add(asset.Metadata.Industry);
					}
				}
				SetBackgroundMsg($"⌛Updating asset metadata for '{symbol}'...");
				var updateReq = new CreateOrUpdateAssetReq()
				{
					Id = asset.Id,
					PortfolioId = asset.PortfolioId,
					ItemType = asset.ItemType,
					ItemCode = asset.ItemCode,
					Quantity = asset.Quantity,
					AveragePrice = asset.AveragePrice,
					MarketId = asset.MarketId,
					Metadata = asset.Metadata,
				};
				var updateResp = await apiClient.UpdateMyPortfolioAssetAsync(updateReq, await GetAuthTokenAsync(), ApiBaseUrl);
				if (updateResp.Status != 200)
				{
					SetBackgroundMsg($"❗Failed to update asset metadata for '{symbol}'. Status: {updateResp.Status}, Message: {updateResp.Message}");
				}
				else
				{
					SetBackgroundMsg($"✅Successfully updated asset metadata for '{symbol}'.");
				}
			}
		}
	}

	private async void InitializePage()
	{
		HideUI = true;

		SwitchToSavedTab();

		Markets = await LoadMarketsAsync(await GetAuthTokenAsync());

		SelectedPortfolio = await LoadPortfolioAsync(PortfolioId, await GetAuthTokenAsync());
		if (SelectedPortfolio == null) return;

		// var symbolsList = Assets?.Select(a => $"{a.ItemCode}:{a.MarketId}").ToList() ?? [];
		var symbolsList = Assets?.Select(a => {
			var market = Markets?.FirstOrDefault(m => string.Equals(m.Id, a.MarketId, StringComparison.OrdinalIgnoreCase));
			return $"{market?.Code??string.Empty}:{a.ItemCode}";
		}).Distinct().ToList() ?? [];
		SetBackgroundMsg($"ℹ️Initializing page for portfolio '{SelectedPortfolio.Name}' with {Assets?.Count() ?? 0} assets. Symbols: {string.Join(", ", symbolsList)}");
		var taskOperator = ServiceProvider.GetRequiredService<ITaskOperator>();
		taskOperator.ExecuteInBackground(() => GetStocksQuotesBackground(symbolsList, RefreshBackgroundTaskId = Random.Shared.Next()));
		taskOperator.ExecuteInBackground(() => AutoPopulateAssetMetadata());

		HideUI = false;

		ShowPassedMessageOrCloseAlert();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender) InitializePage();

		if (IsRefreshRequested()) await Task.Run(InitializePage);
	}

	private string ActiveTab { get; set; } = TabIdSummary;
	private const string TabIdSummary = "nav-summary-tab";
	private const string TabIdPositions = "nav-positions-tab";
	private const string TabIdTxBuysSells = "nav-txbuyssells-tab";
	private const string TabIdTxSettled = "nav-txsettled-tab";

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	private async void SwitchToSavedTab()
	{
		var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
		var savedTab = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "MyPortfolioDetails-active-tab");
		ActiveTab = string.IsNullOrEmpty(savedTab) ? TabIdSummary : savedTab;
		if (ActiveTab != TabIdSummary && ActiveTab != TabIdPositions && ActiveTab != TabIdTxBuysSells && ActiveTab != TabIdTxSettled)
		{
			ActiveTab = TabIdSummary;
		}
	}

	private async void SwitchTab(string tab)
	{
		CloseAlert();
		var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
		await jsLocalStorage.InvokeAsync<string>("LocalStoreSet", "MyPortfolioDetails-active-tab", tab);
	}
}
