using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Libs.Opurator;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public sealed partial class StockSymbolInfo : BasePage
{
	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	[Parameter]
	public string Symbol { get; set; } = string.Empty;
	private string SymbolCode { get; set; } = string.Empty;
	private SymbolInfo? SymbolInfo { get; set; }

	private string PortfolioId { get; set; } = string.Empty;
	private AssetResp? OwningAsset { get; set; }

	private readonly List<MarketDefResp> Markets = [];
	private string MarketId { get; set; } = string.Empty;
	private MarketDef? Market => Markets.FirstOrDefault(m => string.Equals(m.Id, MarketId, StringComparison.OrdinalIgnoreCase))?.ToModel() ?? null;

	private readonly List<AIVendor> AIVendors = [];
	private readonly List<string> AITiers = [];
	private readonly List<string> AIModels = [];

	private string SelectedAIVendor { get; set; } = string.Empty;
	private string SelectedAITier { get; set; } = string.Empty;
	private string SelectedAIModel { get; set; } = string.Empty;

	private CModal ModalDialogAnalyzeSymbol { get; set; } = default!;
	private SymbolAnalysisResp? AnalysisResponse { get; set; }

	private void OnChangeAIVendor()
	{
		AITiers.Clear();
		AIModels.Clear();
		SelectedAITier = string.Empty;
		SelectedAIModel = string.Empty;
		var aiVendor = AIVendors.FirstOrDefault(v => string.Equals(v.Name, SelectedAIVendor, StringComparison.OrdinalIgnoreCase)) ?? null;
		if (aiVendor != null)
		{
			AITiers.AddRange(aiVendor.TieredModels.Keys);
		}
	}

	private void OnChangeAITier()
	{
		AIModels.Clear();
		SelectedAIModel = string.Empty;
		var aiVendor = AIVendors.FirstOrDefault(v => string.Equals(v.Name, SelectedAIVendor, StringComparison.OrdinalIgnoreCase)) ?? null;
		var aiTier = AITiers.FirstOrDefault(t => string.Equals(t, SelectedAITier, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
		if (aiVendor != null && !string.IsNullOrEmpty(aiTier))
		{
			AIModels.AddRange(aiVendor.TieredModels[aiTier]);
		}
	}

	private string BuildAnalysisInputs()
	{
		var inputs = $"""
		## Company Classification

		- Currency: {SymbolInfo?.Currency??"USD"}
		- Quote type: {SymbolInfo?.Overview?.QuoteType??"N/A"}
		- Industry: {SymbolInfo?.Overview?.Industry??"N/A"}
		- Sector: {SymbolInfo?.Overview?.Sector??"N/A"}


		## Financials

		- Total cash: {((SymbolInfo?.Overview?.TotalCash??0)>0?SymbolInfo?.Overview?.TotalCash.ToString("F0"):"N/A")}
		- Total debt: {((SymbolInfo?.Overview?.TotalDebt??0)>0?SymbolInfo?.Overview?.TotalDebt.ToString("F0"):"N/A")}
		- Total revenue: {((SymbolInfo?.Overview?.TotalRevenue??0)>0?SymbolInfo?.Overview?.TotalRevenue.ToString("F0"):"N/A")}
		- Revenue growth: {((SymbolInfo?.Overview?.TotalRevenue??0)>0?SymbolInfo?.Overview?.RevenueGrowth.ToString("P2"):"N/A")}
		- Earnings growth: {((SymbolInfo?.Overview?.TotalRevenue??0)>0?SymbolInfo?.Overview?.EarningsGrowth.ToString("P2"):"N/A")}
		- EBITDA: {((SymbolInfo?.Overview?.Ebitda??0)>0?SymbolInfo?.Overview?.Ebitda.ToString("F0"):"N/A")}
		- EBITDA margins: {((SymbolInfo?.Overview?.Ebitda??0)>0?SymbolInfo?.Overview?.EbitdaMargins.ToString("P2"):"N/A")}
		- Gross margins: {SymbolInfo?.Overview?.GrossMargins.ToString("P2")??"N/A"}
		- Operating margins: {SymbolInfo?.Overview?.OperatingMargins.ToString("P2")??"N/A"}
		- Profit margins: {SymbolInfo?.Overview?.ProfitMargins.ToString("P2")??"N/A"}


		## Valuation

		- Market capitalization: {SymbolInfo?.StockQuote?.MarketCap.ToString("F0")??"N/A"}
		- Current price: {SymbolInfo?.StockQuote?.MarketPrice.ToString("F2")??"N/A"}
		- Shares outstanding: {(SymbolInfo?.StockQuote?.MarketCap > 0 && SymbolInfo?.StockQuote?.MarketPrice > 0 ? (SymbolInfo.StockQuote.MarketCap / SymbolInfo.StockQuote.MarketPrice).ToString("F0") : "N/A")}
		- Trailing EPS: {SymbolInfo?.StockQuote?.TrailingEps.ToString("F2")??"N/A"}
		- Forward EPS: {SymbolInfo?.StockQuote?.ForwardEps.ToString("F2")??"N/A"}
		- Trailing P/E: {SymbolInfo?.StockQuote?.TrailingPE.ToString("F2")??"N/A"}
		- Forward P/E: {SymbolInfo?.StockQuote?.ForwardPE.ToString("F2")??"N/A"}


		## Technical Indicators

		- 52-week low/high: {SymbolInfo?.StockQuote?.FiftyTwoWeekLow.ToString("F2")??"N/A"} / {SymbolInfo?.StockQuote?.FiftyTwoWeekHigh.ToString("F2")??"N/A"}
		- Beta: {SymbolInfo?.StockQuote?.Beta.ToString("F2")??"N/A"}
		- MA10: {SymbolInfo?.StockHistory?.MA10.ToString("F2")??"N/A"}
		- MA20: {SymbolInfo?.StockHistory?.MA20.ToString("F2")??"N/A"}
		- MA50: {SymbolInfo?.StockHistory?.MA50.ToString("F2")??"N/A"}
		- MA100: {SymbolInfo?.StockHistory?.MA100.ToString("F2")??"N/A"}
		- MA200: {SymbolInfo?.StockHistory?.MA200.ToString("F2")??"N/A"}
		- RSI-14: {SymbolInfo?.StockHistory?.RSI14.ToString("F2")??"N/A"}
		- Current volume ({((Market?.IsCurrentlyOpen()??false)?"Market still open":"Market is closed")}): {SymbolInfo?.StockQuote?.MarketVolume.ToString("F0")??"N/A"}
		- Yesterday volume: {SymbolInfo?.StockHistory?.YesterdayVolume.ToString("F0")??"N/A"}
		- 30-day average volume: {SymbolInfo?.StockHistory?.AverageVolume30d.ToString("F0")??"N/A"}
		""";

		var historyData = SymbolInfo?.StockHistory?.History90d?.TakeLast(30)??[];
		if (historyData.Any())
		{
			inputs += "\n\n\n";
			inputs += """
			## 30-days history

			| Date        | Open   | High   | Low    | Close  | Volume     | RSI-14 |
			|-------------|--------|--------|--------|--------|------------|--------|
			""";
			foreach(var h in historyData)
			{
				inputs += $"| {DateTimeOffset.FromUnixTimeSeconds(h.Timestamp):yyyy-MMM-dd} | {h.OpenValue:N2} | {h.HighValue:N2} | {h.LowValue:N2} | {h.CloseValue:N2} | {h.Volume} | {h.RSI14:N2} |\n";
			}
		}

		return inputs;
	}

	private async void BtnClickAIAnalyze()
	{
		var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
		await jsLocalStorage.InvokeAsync<string>("LocalStoreSet", $"StockSymbolInfo-ai-vendor", SelectedAIVendor);
		await jsLocalStorage.InvokeAsync<string>("LocalStoreSet", $"StockSymbolInfo-ai-tier", SelectedAITier);
		await jsLocalStorage.InvokeAsync<string>("LocalStoreSet", $"StockSymbolInfo-ai-model", SelectedAIModel);

		var alertMsg = $"Analyzing symbol with AI (Vendor: {SelectedAIVendor} / Tier: {SelectedAITier} / Model: {SelectedAIModel})...";
		AnalysisResponse = null;
		ModalDialogAnalyzeSymbol.Open();
		ModalDialogAnalyzeSymbol.ShowAlert("info", alertMsg);
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var req = new SymbolAnalysisReq
		{
			AIVendor = SelectedAIVendor,
			AITier = SelectedAITier,
			AIModel = SelectedAIModel,
			Symbol = Symbol,
			Inputs = BuildAnalysisInputs(),
			OwningAmount = (OwningAsset?.Quantity??0) > 0 ? OwningAsset?.Quantity : null,
			OwningAveragePrice = (OwningAsset?.AveragePrice??0) > 0 ? OwningAsset?.AveragePrice : null,
		};
		var stopFlag = false;
		var startTimestamp = DateTime.UtcNow;
		_ = Task.Run(async () =>
		{
			while (!stopFlag)
			{
				var elapsed = DateTime.UtcNow - startTimestamp;
				if (elapsed > TimeSpan.FromSeconds(1))
				{
					ModalDialogAnalyzeSymbol.ShowAlert("info", $"{alertMsg} {elapsed.TotalSeconds:F0} seconds.");
				}
				await Task.Delay(100);
			}
		});
		var analysisResponse = await apiClient.AnalyzeSymbolAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
		stopFlag = true;
		if (analysisResponse.Status != 200)
		{
			ModalDialogAnalyzeSymbol.ShowAlert("danger", analysisResponse.Message ?? "Error analyzing symbol.");
			return;
		}
		AnalysisResponse = analysisResponse.Data;
		ModalDialogAnalyzeSymbol.CloseAlert();
	}

	private void BtnClickLoadData()
	{
		var symbol = $"{SymbolCode}:{MarketId}";
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_STOCK_SYMBOL_INFO.Replace("{Symbol}", symbol, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{QUERY_PARM_REFRESH}=true";
		if (!string.IsNullOrEmpty(PortfolioId))
		{
			nextUrl += $"&pid={PortfolioId}";
		}
		NavigationManager.NavigateTo(nextUrl);
	}

	private void BtnClickReturnToPortfolio()
	{
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}";
		NavigationManager.NavigateTo(nextUrl);
	}

	private int RefreshBackgroundTaskId = Random.Shared.Next();

	private void StopRefresBackground()
	{
		RefreshBackgroundTaskId = 0;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			StopRefresBackground();
		}
		base.Dispose(disposing);
	}

	private async Task<ApiResp<SymbolInfo>> FetchSymbolInfo()
	{
		SetBackgroundMsg($"⌛Loading symbol info for {Symbol}...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var symbolResult = await apiClient.GetStockSymbolInfoAsync(Symbol, await GetAuthTokenAsync(), ApiBaseUrl);
		if (symbolResult.Status != 200)
		{
			SetBackgroundMsg($"❗Error loading symbol info for {Symbol}. Status: {symbolResult.Status}, Message: {symbolResult.Message}");
		}
		return symbolResult;
	}

	private async void InitializePage()
	{
		HideUI = true;

		ShowAlert("info", "Loading symbol info...");
		SymbolCode = (Symbol.Split(':').FirstOrDefault() ?? string.Empty).ToUpper().Trim();
		MarketId = (Symbol.Split(':').LastOrDefault() ?? string.Empty).Trim();
		SymbolInfo = null;
		var symbolResult = await FetchSymbolInfo();
		if (symbolResult.Status != 200)
		{
			ShowAlert("danger", symbolResult.Message ?? $"Error loading symbol info for {Symbol}.");
			return;
		}
		SymbolInfo = symbolResult.Data;

		if (!string.IsNullOrEmpty(PortfolioId))
		{
			ShowAlert("info", "Loading owning asset info...");
			OwningAsset = null;
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var apiResult = await apiClient.GetMyPortfolioAssetsAsync(PortfolioId, await GetAuthTokenAsync(), ApiBaseUrl);
			if (apiResult.Status != 200)
			{
				ShowAlert("danger", apiResult.Message ?? $"Error loading owning asset info for {Symbol}.");
				return;
			}
			OwningAsset = apiResult.Data?.FirstOrDefault(a => string.Equals(a.ItemCode, SymbolCode, StringComparison.OrdinalIgnoreCase));
		}

		var taskOperator = ServiceProvider.GetRequiredService<ITaskOperator>();
		taskOperator.ExecuteInBackground(() => LoadSymbolInfoBackground(RefreshBackgroundTaskId = Random.Shared.Next()));

		HideUI = false;
		CloseAlert();
	}

	private async void LoadSymbolInfoBackground(int myTaskId)
	{
		if (myTaskId == RefreshBackgroundTaskId)
		{
			if (Market == null)
			{
				SetBackgroundMsg($"❗Market info not found for market id '{MarketId}'. Cannot determine refresh timing for symbol info.");
				return;
			}
			var sleepTime = Random.Shared.NextInt64(10*1000, 20*1000);
			if (!Market.IsCurrentlyOpen())
			{
				var timeTillOpen = Market.TimeTillOpen();
				if (timeTillOpen > TimeSpan.FromMinutes(60))
				{
					SetBackgroundMsg($"❗Market '{MarketId}' is currently closed. Not refreshing.");
					return;
				}
				sleepTime = Random.Shared.NextInt64(5*60*1000, 10*60*1000);
				sleepTime = Math.Min(sleepTime, (long)timeTillOpen.TotalMilliseconds)+1000;
			}
			while (sleepTime > 0 && myTaskId == RefreshBackgroundTaskId)
			{
				SetBackgroundMsg($"💤Sleeping {sleepTime/1000} seconds before next info refresh...");
				var delay = Math.Min(sleepTime, 1000);
				await Task.Delay((int)delay);
				sleepTime -= delay;
			}
			if (myTaskId == RefreshBackgroundTaskId)
			{
				var symbolResult = await FetchSymbolInfo();
				if (symbolResult.Status == 200)
				{
					SymbolInfo = symbolResult.Data;
					StateHasChanged();
				}
				await Task.Run(() => LoadSymbolInfoBackground(myTaskId));
			}
		}
	}

	protected override async Task OnInitializedAsync()
	{
		await base.OnInitializedAsync();
		var queryParams = System.Web.HttpUtility.ParseQueryString(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
		PortfolioId = queryParams.Get("pid") ?? string.Empty;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender)
		{
			HideUI = true;
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();

			ShowAlert("info", "Loading market info...");
			var marketResult = await apiClient.GetMarketsAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (marketResult.Status != 200)
			{
				ShowAlert("danger", marketResult.Message ?? "Error loading market info.");
				return;
			}
			Markets.AddRange(marketResult.Data ?? []);

			ShowAlert("info", "Loading AI vendor list...");
			var aiVendorResult = await apiClient.GetAIVendorsAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (aiVendorResult.Status != 200)
			{
				ShowAlert("danger", aiVendorResult.Message ?? "Error loading AI vendor info.");
				return;
			}
			AIVendors.AddRange(aiVendorResult.Data ?? []);

			var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
			SelectedAIVendor = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "StockSymbolInfo-ai-vendor");
			var aiVendor = AIVendors.FirstOrDefault(v => string.Equals(v.Name, SelectedAIVendor, StringComparison.OrdinalIgnoreCase)) ?? null;
			if (aiVendor != null)
			{
				AITiers.AddRange(aiVendor.TieredModels.Keys);
				SelectedAITier = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "StockSymbolInfo-ai-tier");
				var aiTier = AITiers.FirstOrDefault(t => string.Equals(t, SelectedAITier, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
				if (!string.IsNullOrEmpty(aiTier))
				{
					AIModels.AddRange(aiVendor.TieredModels[aiTier]);
					SelectedAIModel = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "StockSymbolInfo-ai-model");
				}
			}

			CloseAlert();

			HideUI = false;
		}

		if (firstRender && !string.IsNullOrEmpty(Symbol))
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
			await Task.Run(() => InitializePage());
		}
	}
}
