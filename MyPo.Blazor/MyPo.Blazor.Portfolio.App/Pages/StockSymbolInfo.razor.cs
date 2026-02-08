using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;

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

	private async void LoadSymbolInfo()
	{
		HideUI = true;
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();

		ShowAlert("info", "Loading symbol info...");
		SymbolCode = (Symbol.Split(':').FirstOrDefault() ?? string.Empty).ToUpper().Trim();
		MarketId = (Symbol.Split(':').LastOrDefault() ?? string.Empty).Trim();
		SymbolInfo = null;
		var symbolResult = await apiClient.GetStockSymbolInfoAsync(Symbol, await GetAuthTokenAsync(), ApiBaseUrl);
		if (symbolResult.Status != 200)
		{
			ShowAlert("danger", symbolResult.Message ?? "Error loading symbol info.");
			return;
		}
		SymbolInfo = symbolResult.Data;

		HideUI = false;
		CloseAlert();
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
			LoadSymbolInfo();
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
			await Task.Run(() => LoadSymbolInfo());
		}
	}
}
