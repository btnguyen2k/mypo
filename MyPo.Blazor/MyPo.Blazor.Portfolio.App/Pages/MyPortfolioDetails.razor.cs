using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using Microsoft.JSInterop;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Libs.Opurator;
using MyPo.Shared.Api;

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
	private Dictionary<string, StockQuote> QuotesMap = [];

	private bool StopRefreshQuotes { get; set; } = false;

	private void StopRefreshQuotesBackground()
	{
		StopRefreshQuotes = true;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			StopRefreshQuotesBackground();
		}
		base.Dispose(disposing);
	}

	private async Task<ApiResp<IDictionary<string, StockQuote>>> FetchQuotesForSymbols(List<string> symbolsList)
	{
		var symbols = string.Join(",", symbolsList);
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		return await apiClient.GetStocksQuotesAsync(symbols, await GetAuthTokenAsync(), ApiBaseUrl);
	}

	private async void GetStocksQuotesBackground(List<string> symbolsList)
	{
		if (symbolsList.Count > 0 && !StopRefreshQuotes)
		{
			// fech stock quotes chunk by chunk to avoid too long query string issue
			// each chunk is 5 symbols max
			var cloneSymbolList = new List<string>(symbolsList.OrderBy(_ => Random.Shared.Next()));
			while (cloneSymbolList.Count > 0 && !StopRefreshQuotes)
			{
				var currentChunk = cloneSymbolList.Take(5).ToList();
				cloneSymbolList = [.. cloneSymbolList.Skip(5)];

				var symbols = string.Join(",", currentChunk);
				SetBackgroundMsg($"⌛Fetching quotes for symbols: {symbols}");
				var quotesResp = await FetchQuotesForSymbols(currentChunk);
				if (quotesResp.Status == 200)
				{
					foreach (var quote in quotesResp.Data ?? new Dictionary<string, StockQuote>())
					{
						QuotesMap[quote.Key] = quote.Value;
					}
					StateHasChanged();
				}
				else
				{
					SetBackgroundMsg($"❗Failed to fetch quotes for symbols: {symbols}. Status: {quotesResp.Status}, Message: {quotesResp.Message}");
				}
			}
			if (!StopRefreshQuotes)
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
				while (sleepTime > 0 && !StopRefreshQuotes)
				{
					SetBackgroundMsg($"💤Sleeping {sleepTime/1000} seconds before next quotes refresh...");
					var delay = Math.Min(sleepTime, 1000);
					await Task.Delay((int)delay);
					sleepTime -= delay;
				}
				if (!StopRefreshQuotes)
				{
					await Task.Run(() => GetStocksQuotesBackground(symbolsList));
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

	private async void InitializePage()
	{
		HideUI = true;

		SwitchToSavedTab();

		Markets = await LoadMarketsAsync(await GetAuthTokenAsync());

		SelectedPortfolio = await LoadPortfolioAsync(PortfolioId, await GetAuthTokenAsync());
		if (SelectedPortfolio == null) return;

		var symbolsList = Assets?.Select(a => $"{a.ItemCode}:{a.MarketId}").ToList() ?? [];
		SetBackgroundMsg($"ℹ️Initializing page for portfolio '{SelectedPortfolio.Name}' with {Assets?.Count() ?? 0} assets. Symbols: {string.Join(", ", symbolsList)}");
		var taskOperator = ServiceProvider.GetRequiredService<ITaskOperator>();
		taskOperator.ExecuteInBackground(() => GetStocksQuotesBackground(symbolsList));

		HideUI = false;

		var (alertType, alertMessage) = GetPassedMessageFromQuery();
		if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
			ShowAlert(alertType, alertMessage, autoCloseAfterMs: ALERT_AUTO_CLOSE_MS);
		else
			CloseAlert();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender)
		{
			InitializePage();
		}

		var queryParams = System.Web.HttpUtility.ParseQueryString(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
		if (queryParams.AllKeys.Contains(QUERY_PARM_REFRESH))
		{
			// rebuild the URL without the refresh query parameter
			queryParams.Remove(QUERY_PARM_REFRESH);
			var uriBuilder = new UriBuilder(NavigationManager.ToAbsoluteUri(NavigationManager.Uri))
			{
				Query = queryParams.ToString() ?? string.Empty
			};
			NavigationManager.NavigateTo(uriBuilder.Uri.ToString(), forceLoad: false);

			// reload page data in the background
			await Task.Run(InitializePage);
		}
	}

	private string ActiveTab { get; set; } = TabIdSummary;
	private const string TabIdSummary = "nav-summary-tab";
	private const string TabIdPositions = "nav-positions-tab";
	private const string TabIdTxBuysSells = "nav-txbuyssells-tab";
	private const string TabIdTxSettled = "nav-txsettled-tab";

	[Inject]
	private IJSRuntime JS { get; set; } = default!;
	private IJSObjectReference? jsLocalStorage;

	private async void SwitchToSavedTab()
	{
		jsLocalStorage ??= await JS.InvokeAsync<IJSObjectReference>(
			"import",
			$"./_content/{typeof(MyPortfolioDetails).Assembly.GetName().Name!}/js/local-storage.js"
		);
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
		jsLocalStorage ??= await JS.InvokeAsync<IJSObjectReference>(
			"import",
			$"./_content/{typeof(MyPortfolioDetails).Assembly.GetName().Name!}/js/local-storage.js"
		);
		await jsLocalStorage.InvokeAsync<string>("LocalStoreSet", "MyPortfolioDetails-active-tab", tab);
	}
}
