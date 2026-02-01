using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using Microsoft.JSInterop;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioDetails : BasePage
{
	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;
	private PortfolioResp? SelectedPortfolio { get; set; }

	private IEnumerable<MarketDefResp>? Markets { get; set; }
	private IEnumerable<TxBuySellResp>? Transactions { get; set; }
	private IEnumerable<AssetResp>? Assets { get; set; }
	private IEnumerable<TxSettlementResp>? RoiRecords { get; set; }

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

		ShowAlert("info", "Loading portfolio transactions, please wait...");
		var apiRespTx = await apiClient.GetMyPortfolioTxBuySellsAsync(portfolio.Id, authToken, ApiBaseUrl);
		if (apiRespTx.Status != 200)
		{
			ShowAlert("danger", apiRespTx.Message ?? $"Error while loading portfolio transactions.");
			return null;
		}
		Transactions = apiRespTx.Data ?? [];

		ShowAlert("info", "Loading portfolio assets, please wait...");
		var apiRespAssets = await apiClient.GetMyPortfolioAssetsAsync(portfolio.Id, authToken, ApiBaseUrl);
		if (apiRespAssets.Status != 200)
		{
			ShowAlert("danger", apiRespAssets.Message ?? $"Error while loading portfolio assets.");
			return null;
		}
		Assets = apiRespAssets.Data ?? [];

		ShowAlert("info", "Loading portfolio ROI records, please wait...");
		var apiRespRoiRecs = await apiClient.GetMyPortfolioTxSettlementsAsync(portfolio.Id, authToken, ApiBaseUrl);
		if (apiRespRoiRecs.Status != 200)
		{
			ShowAlert("danger", apiRespRoiRecs.Message ?? $"Error while loading portfolio ROI records.");
			return null;
		}
		RoiRecords = apiRespRoiRecs.Data ?? [];

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
			await Task.Run(() => InitializePage());
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
