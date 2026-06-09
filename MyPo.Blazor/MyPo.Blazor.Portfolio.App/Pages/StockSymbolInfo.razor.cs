using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Libs.Opurator;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Portfolio.Shared.Utils;
using MyPo.Shared.Api;
using System.Globalization;

namespace MyPo.Blazor.Portfolio.App.Pages;

public sealed partial class StockSymbolInfo : BasePage
{
	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	[Parameter]
	public string Symbol { get; set; } = string.Empty;

	private SymbolInfo? SymbolInfo { get; set; }

	private string PortfolioId { get; set; } = string.Empty;
	private AssetResp? OwningAsset { get; set; }

	private readonly List<MarketDefResp> Markets = [];
	private MarketDef? Market = null;
	// private string MarketId { get; set; } = string.Empty;
	// private MarketDef? Market => Markets.FirstOrDefault(m => string.Equals(m.Id, MarketId, StringComparison.OrdinalIgnoreCase))?.ToModel() ?? null;

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

	private static string FormatDecimal(decimal? value, string format) =>
		value.HasValue ? value.Value.ToString(format, CultureInfo.InvariantCulture) : "N/A";

	private static string FormatLong(long? value) =>
		value.HasValue ? value.Value.ToString("N0", CultureInfo.InvariantCulture) : "N/A";

	private string BuildAnalysisInputs()
	{
		var inputs = $"""
		## Company Classification

		- Currency: {SymbolInfo?.Currency??"USD"}
		- Quote type: {SymbolInfo?.QuoteType??"N/A"}
		- Industry: {SymbolInfo?.Industry??"N/A"}
		- Sector: {SymbolInfo?.Sector??"N/A"}


		## Financials

		- Total cash: {FormatLong(SymbolInfo?.TotalCash)}
		- Total debt: {FormatLong(SymbolInfo?.TotalDebt)}
		- Total revenue: {FormatLong(SymbolInfo?.TotalRevenue)}
		- Revenue growth: {FormatDecimal(SymbolInfo?.RevenueGrowth, "P2")}
		- Earnings growth: {FormatDecimal(SymbolInfo?.EarningsGrowth, "P2")}
		- EBITDA: {FormatLong(SymbolInfo?.Ebitda)}
		- EBITDA margins: {FormatDecimal(SymbolInfo?.EbitdaMargins, "P2")}
		- Gross margins: {FormatDecimal(SymbolInfo?.GrossMargins, "P2")}
		- Operating margins: {FormatDecimal(SymbolInfo?.OperatingMargins, "P2")}
		- Profit margins: {FormatDecimal(SymbolInfo?.ProfitMargins, "P2")}


		## Valuation

		- Market capitalization: {(SymbolInfo?.StockQuote?.MarketCap > 0 ? FormatUtils.FormatVolume(SymbolInfo.StockQuote!.MarketCap.Value) : "N/A")}
		- Current price: {FormatDecimal(SymbolInfo?.StockQuote?.MarketPrice, "F2")}
		- Shares outstanding: {(SymbolInfo?.StockQuote?.MarketCap > 0 && SymbolInfo?.StockQuote?.MarketPrice > 0 ? ((decimal)SymbolInfo.StockQuote!.MarketCap / SymbolInfo.StockQuote.MarketPrice).ToString("F0", CultureInfo.InvariantCulture) : "N/A")}
		- Trailing EPS: {FormatDecimal(SymbolInfo?.StockQuote?.TrailingEps, "F2")}
		- Forward EPS: {FormatDecimal(SymbolInfo?.StockQuote?.ForwardEps, "F2")}
		- Trailing P/E: {FormatDecimal(SymbolInfo?.StockQuote?.TrailingPE, "F2")}
		- Forward P/E: {FormatDecimal(SymbolInfo?.StockQuote?.ForwardPE, "F2")}


		## Technical Indicators

		- 52-week low/high: {FormatDecimal(SymbolInfo?.StockQuote?.FiftyTwoWeekLow, "F2")} / {FormatDecimal(SymbolInfo?.StockQuote?.FiftyTwoWeekHigh, "F2")}
		- Beta: {FormatDecimal(SymbolInfo?.StockQuote?.Beta, "F2")}
		- MA10: {FormatDecimal(SymbolInfo?.StockHistory?.MA10, "F2")}
		- MA20: {FormatDecimal(SymbolInfo?.StockHistory?.MA20, "F2")}
		- MA50: {FormatDecimal(SymbolInfo?.StockHistory?.MA50, "F2")}
		- MA100: {FormatDecimal(SymbolInfo?.StockHistory?.MA100, "F2")}
		- MA200: {FormatDecimal(SymbolInfo?.StockHistory?.MA200, "F2")}
		- RSI-14: {FormatDecimal(SymbolInfo?.StockHistory?.RSI14, "F2")}
		- Current volume ({((Market?.IsCurrentlyOpen()??false)?"Market still open":"Market is closed")}) : {(SymbolInfo?.StockQuote?.MarketVolume > 0 ? FormatUtils.FormatVolume(SymbolInfo.StockQuote!.MarketVolume.Value) : "N/A")}
		- Yesterday volume: {(SymbolInfo?.StockHistory?.YesterdayVolume > 0 ? FormatUtils.FormatVolume(SymbolInfo.StockHistory!.YesterdayVolume) : "N/A")}
		- 30-day average volume: {(SymbolInfo?.StockHistory?.AverageVolume30d > 0 ? FormatUtils.FormatVolume(SymbolInfo.StockHistory!.AverageVolume30d) : "N/A")}
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
				inputs += $"| {DateTimeOffset.FromUnixTimeSeconds(h.Timestamp):yyyy-MMM-dd} | {h.Open:N2} | {h.High:N2} | {h.Low:N2} | {h.Close:N2} | {h.Volume} | {h.RSI14:N2} |\n";
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
		// var symbol = $"{SymbolCode}:{MarketId}";
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_STOCK_SYMBOL_INFO.Replace("{Symbol}", Symbol, StringComparison.OrdinalIgnoreCase)}?{QUERY_PARM_REFRESH}=true";
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

	private async Task<ApiResp<SymbolInfo>> FetchSymbolInfo(string symbol)
	{
		SetBackgroundMsg($"⌛Loading symbol info for {symbol}...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var symbolResult = await apiClient.GetStockSymbolInfoAsync(Symbol, await GetAuthTokenAsync(), ApiBaseUrl);
		if (!symbolResult.IsSuccess)
		{
			SetBackgroundMsg($"❗Error loading symbol info for {symbol}. Status: {symbolResult.Status}, Message: {symbolResult.Message}");
		}
		return symbolResult;
	}

	private async void InitializePage()
	{
		HideUI = true;

		ShowAlert("info", "Loading symbol info...");
		SymbolInfo = null;
		var symbolResult = await FetchSymbolInfo(Symbol);
		if (!symbolResult.IsSuccess || symbolResult.Data is null)
		{
			ShowAlert("danger", symbolResult.Message ?? $"Error loading symbol info for {Symbol}.");
			return;
		}
		SymbolInfo = symbolResult.Data;

		var parts = SymbolInfo.NormalizedSymbol.Split(":") ?? [];
		var (exchange, symbol) = (parts.Length > 1 ? parts[0] : string.Empty, parts.Length > 1 ? parts[1] : parts[0]);
		Market = Markets.FirstOrDefault(m => string.Equals(m.Code, exchange, StringComparison.OrdinalIgnoreCase))?.ToModel();

		if (!string.IsNullOrEmpty(PortfolioId))
		{
			ShowAlert("info", "Loading owning asset info...");
			OwningAsset = null;
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var apiResult = await apiClient.GetMyPortfolioAssetsAsync(PortfolioId, await GetAuthTokenAsync(), ApiBaseUrl);
			if (!apiResult.IsSuccess)
			{
				ShowAlert("danger", apiResult.Message ?? $"Error loading owning asset info for {Symbol}.");
				return;
			}
			OwningAsset = apiResult.Data?.FirstOrDefault(a =>
				string.Equals(a.ItemCode, symbol, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(a.Market?.Code, exchange, StringComparison.OrdinalIgnoreCase)
			);
		}

		var taskOperator = ServiceProvider.GetRequiredService<ITaskOperator>();
		taskOperator.ExecuteInBackground(() => LoadSymbolInfoBackground(Symbol, RefreshBackgroundTaskId = Random.Shared.Next()));

		HideUI = false;
		CloseAlert();
	}

	private async void LoadSymbolInfoBackground(string symbol, int myTaskId)
	{
		if (myTaskId == RefreshBackgroundTaskId)
		{
			if (Market == null)
			{
				SetBackgroundMsg($"❗Market info not found. Cannot determine refresh timing for symbol info.");
				return;
			}
			var sleepTime = Random.Shared.NextInt64(10*1000, 20*1000);
			if (!Market.IsCurrentlyOpen())
			{
				var timeTillOpen = Market.TimeTillOpen();
				if (timeTillOpen > TimeSpan.FromMinutes(60))
				{
					SetBackgroundMsg($"❗Market '{Market.Code}' is currently closed. Not refreshing.");
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
				var symbolResult = await FetchSymbolInfo(symbol);
				if (symbolResult.Status == 200)
				{
					SymbolInfo = symbolResult.Data;
					StateHasChanged();
				}
				await Task.Run(() => LoadSymbolInfoBackground(symbol, myTaskId));
			}
		}
	}

	protected override async Task OnInitializedAsync()
	{
		await base.OnInitializedAsync();
		// store the portfolio id for later.
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

			CloseAlert();

			HideUI = false;
		}

		if (firstRender && !string.IsNullOrEmpty(Symbol))
		{
			InitializePage();
		}

		if (RemoveRefreshParamIfPresent()) await Task.Run(InitializePage);
	}
}
