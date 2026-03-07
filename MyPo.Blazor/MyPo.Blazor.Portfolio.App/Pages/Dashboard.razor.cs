using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class Dashboard : BasePage
{
	private List<MarketEventResp>? MarketEventsList { get; set; }
	private List<MarketEventResp> EventsDistribution => MarketEventsList?.Where(e =>MarketEventEntity.EVENT_DIVIDEND.Equals(e.EventType, StringComparison.OrdinalIgnoreCase)
			|| MarketEventEntity.EVENT_DISTRIBUTION.Equals(e.EventType, StringComparison.OrdinalIgnoreCase))
		.Where(e => e.Metadata?.Amount >= 0.03m)
		.OrderBy(e => e.EventTime)
		.ToList() ?? [];
	private List<MarketEventResp> EventsEarnings => MarketEventsList?.Where(e =>MarketEventEntity.EVENT_EARNINGS.Equals(e.EventType, StringComparison.OrdinalIgnoreCase))
		.OrderBy(e => e.EventTime)
		.ToList() ?? [];
	private List<MarketEventResp> EventsListing => MarketEventsList?.Where(e =>MarketEventEntity.EVENT_LISTING.Equals(e.EventType, StringComparison.OrdinalIgnoreCase))
		.OrderBy(e => e.EventTime)
		.ToList() ?? [];

	private string ActiveTab { get; set; } = TabIdDividend;
	private const string TabIdDividend = "nav-dividend-tab";
	private const string TabIdListing = "nav-listing-tab";
	private const string TabIdEarnings = "nav-earnings-tab";

	// map {symbol --> quote}
	private readonly Dictionary<string, StockQuote> QuotesMap = [];

	// map {symbol --> yield_vs_current_price}
	private readonly Dictionary<string, decimal> YieldsMap = [];

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	private async void SwitchToSavedTab()
	{
		var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
		var savedTab = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "Dashboard-active-tab");
		ActiveTab = string.IsNullOrEmpty(savedTab) ? TabIdDividend : savedTab;
		if (ActiveTab != TabIdDividend && ActiveTab != TabIdListing && ActiveTab != TabIdEarnings)
		{
			ActiveTab = TabIdDividend;
		}
		StateHasChanged();
	}

	private async void SwitchTab(string tab)
	{
		CloseAlert();
		var jsLocalStorage = await PortfolioUtils.LoadJSLocalStorage(JS);
		await jsLocalStorage.InvokeAsync<string>("LocalStoreSet", "Dashboard-active-tab", tab);
	}

	private async Task<ApiResp<IDictionary<string, StockQuote>>> FetchQuotesForSymbols(List<string> symbolsList)
	{
		var symbols = string.Join(",", symbolsList);
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		return await apiClient.GetStocksQuotesAsync(symbols, await GetAuthTokenAsync(), ApiBaseUrl);
	}

	private async void GetStocksQuotesBackground()
	{
		var symbolsList = MarketEventsList?
			.Where(e => !e.EventType.Equals(MarketEventEntity.EVENT_EARNINGS, StringComparison.CurrentCultureIgnoreCase))
			.Select(e => e.ItemCode).ToList() ?? [];
		while (symbolsList.Count > 0)
		{
			var currentChunk = symbolsList.Take(5).ToList();
			symbolsList = [.. symbolsList.Skip(5)];

			var symbols = string.Join(",", currentChunk);
			SetBackgroundMsg($"⌛Fetching quotes for symbols: {symbols}");
			var quotesResp = await FetchQuotesForSymbols(currentChunk);
			if (quotesResp.Status == 200)
			{
				foreach (var quote in quotesResp.Data ?? new Dictionary<string, StockQuote>())
				{
					QuotesMap[quote.Key] = quote.Value;
					var eventInfo = MarketEventsList?.FirstOrDefault(e => e.ItemCode.Equals(quote.Key, StringComparison.OrdinalIgnoreCase));
					var amount = eventInfo?.Metadata?.Amount ?? 0;
					YieldsMap[quote.Key] = amount > 0 && quote.Value.MarketPrice > 0 ? amount/quote.Value.MarketPrice : 0;
				}
				StateHasChanged();
			}
			else
			{
				SetBackgroundMsg($"❗Failed to fetch quotes for symbols: {symbols}. Status: {quotesResp.Status}, Message: {quotesResp.Message}");
			}
		}
		SetBackgroundMsg(string.Empty);
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			ShowAlert("info", "Loading markets metadata...");
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var result = await apiClient.GetUpcomingMarketEventsAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (result.Status == 200)
			{
				HideUI = false;
				MarketEventsList = [.. result.Data ?? []];
				var (alertType, alertMessage) = GetPassedMessageFromQuery();
				if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
				{
					ShowAlert(alertType, alertMessage, ALERT_AUTO_CLOSE_MS);
				}
				else
				{
					CloseAlert();
					await Task.Run(GetStocksQuotesBackground);
				}
			}
			else
			{
				ShowAlert("danger", result.Message ?? "Error loading portfolios.");
			}

			SwitchToSavedTab();
		}
	}
}
