using Ddth.Utilities.Tempus;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class Dashboard : BasePage
{
	private List<MarketEventResp>? MarketEventsList { get; set; }
	private List<MarketEventResp> EventsDistribution => MarketEventsList?.Where(e =>MarketEventEntity.EVENT_DIVIDEND.Equals(e.EventType, StringComparison.OrdinalIgnoreCase)
			|| MarketEventEntity.EVENT_DISTRIBUTION.Equals(e.EventType, StringComparison.OrdinalIgnoreCase))
		.Where(e => e.Metadata?.Dividend?.Amount >= 0.03m)
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

	// map {symbol --> market close price before ex-dividend date}
	private readonly Dictionary<string, decimal> PreExDivPrice = [];

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

	private async void GetStocksQuotesBackground()
	{
		var symbolsList = MarketEventsList?
			.Where(e => !e.EventType.Equals(MarketEventEntity.EVENT_EARNINGS, StringComparison.CurrentCultureIgnoreCase))
			.Select(e => e.ItemCode).Distinct().ToList() ?? [];
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var authToken = await GetAuthTokenAsync();
		await TickerUtils.FetchQuotesForTickers(
			symbolsList,
			apiClient,
			authToken,
			ApiBaseUrl,
			callbackPrefetch: (currentChunk) =>
			{
				var symbols = string.Join(",", currentChunk);
				SetBackgroundMsg($"⌛Fetching quotes for symbols: {symbols}");
			},
			callbackPostfetch: (quotesResp) =>
			{
				if (quotesResp.Status == 200)
				{
					foreach (var quote in quotesResp.Data ?? new Dictionary<string, StockQuote>())
					{
						QuotesMap[quote.Key] = quote.Value;
						var eventInfo = MarketEventsList?.FirstOrDefault(e => e.ItemCode.Equals(quote.Key, StringComparison.OrdinalIgnoreCase));
						// Console.WriteLine($"[DEBUG] {quote.Key}: {JsonSerializer.Serialize(eventInfo)}");
						// var amount = eventInfo?.Metadata?.Dividend?.Amount ?? 0;
						// YieldsMap[quote.Key] = amount > 0 && quote.Value.MarketPrice > 0 ? amount/quote.Value.MarketPrice : 0;
						YieldsMap[quote.Key] = eventInfo?.Metadata?.Dividend?.DividendYield ?? 0;
					}
					StateHasChanged();
				}
				else
				{
					var symbols = string.Join(",", quotesResp.Data?.Keys ?? []);
					SetBackgroundMsg($"❗Failed to fetch quotes for symbols: {symbols}. Status: {quotesResp.Status}, Message: {quotesResp.Message}");
				}
			}
		);
		SetBackgroundMsg(string.Empty);
	}

	private async void GetPricePreExDivBackground()
	{
		var now = DateTimeOffset.UtcNow;
		var events = MarketEventsList?
			.Where(e => e.EventType.Equals(MarketEventEntity.EVENT_DIVIDEND, StringComparison.CurrentCultureIgnoreCase)
				|| e.EventType.Equals(MarketEventEntity.EVENT_DISTRIBUTION, StringComparison.CurrentCultureIgnoreCase))
			.Where(e => e.EventTime < now) ?? [];
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		foreach (var e in events)
		{
			var tz = e.MarketId.ToUpper() switch
			{
				"AU" => "Australia/Sydney",
				"VN" => "Asia/Ho_Chi_Minh",
				"US" => "America/New_York",
				_ => "UTC"
			};
			var dateAt = (e.EventTime.ToTimeZoneSilently(tz) ?? e.EventTime).AddDays(-1).Date;
			var quoteAtResp = await apiClient.GetStockQuoteAtDateAsync(e.ItemCode, dateAt, await GetAuthTokenAsync(), ApiBaseUrl);
			if (quoteAtResp.Status == 200 && quoteAtResp.Data != null)
			{
				PreExDivPrice[e.ItemCode] = quoteAtResp.Data.Close;
				StateHasChanged();
			}
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			ShowAlert("info", "Loading upcoming market events...");
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
					// await Task.Run(GetPricePreExDivBackground);
				}
			}
			else
			{
				ShowAlert("danger", result.Message ?? "Error loading portfolios.");
			}

			var jsDatatable = await PortfolioUtils.LoadJSDatatable(JS);
			await jsDatatable.InvokeAsync<string>("MakeDatatable", "#tblDividendEvents");
			await jsDatatable.InvokeAsync<string>("MakeDatatable", "#tblListingEvents");
			await jsDatatable.InvokeAsync<string>("MakeDatatable", "#tblEarningsEvents");

			SwitchToSavedTab();
		}
	}
}
