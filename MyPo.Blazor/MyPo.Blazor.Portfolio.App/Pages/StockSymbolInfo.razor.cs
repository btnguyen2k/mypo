using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
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
	[Parameter]
	public string Symbol { get; set; } = string.Empty;
	private string SymbolCode { get; set; } = string.Empty;
	private SymbolInfo? SymbolInfo { get; set; }

	private readonly List<MarketDefResp> Markets = [];
	private string MarketId { get; set; } = string.Empty;
	private MarketDef? Market => Markets.FirstOrDefault(m => string.Equals(m.Id, MarketId, StringComparison.OrdinalIgnoreCase))?.ToModel() ?? null;
	private string PortfolioId { get; set; } = string.Empty;

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
