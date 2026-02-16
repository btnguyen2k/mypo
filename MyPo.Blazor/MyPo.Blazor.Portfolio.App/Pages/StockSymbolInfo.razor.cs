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
		var inputs = $"- Currency: {SymbolInfo?.Currency??"USD"}\n"
			+ $"- Quote type: {SymbolInfo?.Overview?.QuoteType??"N/A"}\n"
			+ $"- Industry: {SymbolInfo?.Overview?.Industry??"N/A"}\n"
			+ $"- Sector: {SymbolInfo?.Overview?.Sector??"N/A"}\n"
			+ $"- Total cash: {SymbolInfo?.Overview?.TotalCash.ToString("F0")??"N/A"}\n"
			+ $"- Total cash per share: {SymbolInfo?.Overview?.TotalCashPerShare.ToString("F2")??"N/A"}\n"
			+ $"- Total debt: {SymbolInfo?.Overview?.TotalDebt.ToString("F0")??"N/A"}\n"
			+ $"- Total debt per share: {SymbolInfo?.Overview?.TotalDebtPerShare.ToString("F2")??"N/A"}\n"
			+ $"- Total revenue: {SymbolInfo?.Overview?.TotalRevenue.ToString("F0")??"N/A"}\n"
			+ $"- Total revenue per share: {SymbolInfo?.Overview?.TotalRevenuePerShare.ToString("F2")??"N/A"}\n"
			+ $"- Revenue growth: {SymbolInfo?.Overview?.RevenueGrowth.ToString("P2")??"N/A"}\n"
			+ $"- Earnings growth: {SymbolInfo?.Overview?.EarningsGrowth.ToString("P2")??"N/A"}\n"
			+ $"- EBITDA: {SymbolInfo?.Overview?.Ebitda.ToString("F0")??"N/A"}\n"
			+ $"- EBITDA margins: {SymbolInfo?.Overview?.EbitdaMargins.ToString("P2")??"N/A"}\n"
			+ $"- Gross margins: {SymbolInfo?.Overview?.GrossMargins.ToString("P2")??"N/A"}\n"
			+ $"- Profit margins: {SymbolInfo?.Overview?.ProfitMargins.ToString("P2")??"N/A"}\n"
			+ $"- Operating margins: {SymbolInfo?.Overview?.OperatingMargins.ToString("P2")??"N/A"}\n"
			+ $"- Market capitalization: {SymbolInfo?.StockQuote?.MarketCap.ToString("F0")??"N/A"}\n"
			+ $"- Current price: {SymbolInfo?.StockQuote?.MarketPrice.ToString("F2")??"N/A"}\n"
			+ $"- 52-week low/high: {SymbolInfo?.StockQuote?.FiftyTwoWeekLow.ToString("F2")??"N/A"} / {SymbolInfo?.StockQuote?.FiftyTwoWeekHigh.ToString("F2")??"N/A"}\n"
			+ $"- Current volume ({((Market?.IsCurrentlyOpen()??false)?"Market still open":"Market is closed")}): {SymbolInfo?.StockQuote?.MarketVolume.ToString("F0")??"N/A"}\n"
			+ $"- Yesterday volume: {SymbolInfo?.StockHistory?.YesterdayVolume.ToString("F0")??"N/A"}\n"
			+ $"- Average volume last 30 days: {SymbolInfo?.StockHistory?.AverageVolume30d.ToString("F0")??"N/A"}\n"
			+ $"- Trailing EPS: {SymbolInfo?.StockQuote?.TrailingEps.ToString("F2")??"N/A"}\n"
			+ $"- Forward EPS: {SymbolInfo?.StockQuote?.ForwardEps.ToString("F2")??"N/A"}\n"
			+ $"- Trailing P/E: {SymbolInfo?.StockQuote?.TrailingPE.ToString("F2")??"N/A"}\n"
			+ $"- Forward P/E: {SymbolInfo?.StockQuote?.ForwardPE.ToString("F2")??"N/A"}\n"
			+ $"- Industry P/E average: N/A\n"
			+ $"- Beta: {SymbolInfo?.StockQuote?.Beta.ToString("F2")??"N/A"}\n"
			+ $"- MA10: {SymbolInfo?.StockHistory?.MA10.ToString("F2")??"N/A"}\n"
			+ $"- MA20: {SymbolInfo?.StockHistory?.MA20.ToString("F2")??"N/A"}\n"
			+ $"- MA50: {SymbolInfo?.StockHistory?.MA50.ToString("F2")??"N/A"}\n"
			+ $"- MA100: {SymbolInfo?.StockHistory?.MA100.ToString("F2")??"N/A"}\n"
			+ $"- MA200: {SymbolInfo?.StockHistory?.MA200.ToString("F2")??"N/A"}\n"
			+ $"- RSI-14: {SymbolInfo?.StockHistory?.RSI14.ToString("F2")??"N/A"}\n"
		;
		return inputs;
	}

	private static string BuidAnalysisExpectedOutputs()
	{
		var expectedOutputs = $"- What is the overall analysis of this stock based on the above information? Is it a good buy/sell/hold? Why?\n"
			+ $"- What are the key strengths and weaknesses of this stock?\n"
			+ $"- What are the potential risks and opportunities for this stock?\n"
			+ $"- What is the expected price movement for this stock in the next 1 month, 3 months, and 6 months? Please provide a brief explanation for each time frame.\n"
			+ $"- What are the Big Picture Summary and Trading Game Strategy.\n"
			+ $"- Estimated Fair Value and Optimal Buy/Sell Zones.\n";
		return expectedOutputs;
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
			ExpectedOutputs = BuidAnalysisExpectedOutputs(),
			MaxOutputTokens = SelectedAITier.Equals(AIVendor.TIER_FREE, StringComparison.OrdinalIgnoreCase) ? 0 : 3000,
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
		ModalDialogAnalyzeSymbol.CloseAlert();
		AnalysisResponse = analysisResponse.Data;
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

	private bool StopRefreshBackground { get; set; } = false;

	private void StopRefresBackground()
	{
		StopRefreshBackground = true;
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

		var taskOperator = ServiceProvider.GetRequiredService<ITaskOperator>();
		taskOperator.ExecuteInBackground(() => LoadSymbolInfoBackground());

		HideUI = false;
		CloseAlert();
	}

	private async void LoadSymbolInfoBackground()
	{
		if (!StopRefreshBackground)
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
			while (sleepTime > 0 && !StopRefreshBackground)
			{
				SetBackgroundMsg($"💤Sleeping {sleepTime/1000} seconds before next info refresh...");
				var delay = Math.Min(sleepTime, 1000);
				await Task.Delay((int)delay);
				sleepTime -= delay;
			}
			if (!StopRefreshBackground)
			{
				var symbolResult = await FetchSymbolInfo();
				if (symbolResult.Status == 200)
				{
					SymbolInfo = symbolResult.Data;
					StateHasChanged();
				}
				await Task.Run(() => LoadSymbolInfoBackground());
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

			var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
			SelectedAIVendor = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "StockSymbolInfo-ai-vendor");
			SelectedAITier = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "StockSymbolInfo-ai-tier");
			SelectedAIModel = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "StockSymbolInfo-ai-model");

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
