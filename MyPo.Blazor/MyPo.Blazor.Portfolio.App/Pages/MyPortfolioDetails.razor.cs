using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using Microsoft.JSInterop;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioDetails : BasePage
{
	[Parameter]
	public string Id { get; set; } = string.Empty;
	private PortfolioRecResp? SelectedPortfolio { get; set; }
	// private Dictionary<string, PortfolioRecResp>? MyPortfolioMap { get; set; }

	private IEnumerable<MarketDefResp>? Markets { get; set; }
	private IEnumerable<TransactionRecResp>? Transactions { get; set; }
	private IEnumerable<AssetResp>? Assets { get; set; }

	private async Task<PortfolioRecResp?> LoadPortfolioAsync(string id, string authToken)
	{
		HideUI = true;
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();

		ShowAlert("info", "Loading portfolio, please wait...");
		var apiRespPortfolio = await apiClient.GetMyPortfolioAsync(authToken, ApiBaseUrl);
		if (apiRespPortfolio.Status != 200)
		{
			ShowAlert("danger", apiRespPortfolio.Message ?? $"Portfolio '{id}' not found.");
			return null;
		}

		var allPortfolios = apiRespPortfolio.Data ?? [];
		var myPortfolioMap = allPortfolios.ToDictionary(p => p.Id);
		var portfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);
		PortfolioRecResp portfolio = default!;
		if (!myPortfolioMap.TryGetValue(id, out portfolio))
		{
			ShowAlert("danger", $"Portfolio '{id}' not found.");
			return null;
		}

		ShowAlert("info", "Loading portfolio transactions, please wait...");
		var apiRespTx = await apiClient.GetMyPortfolioTransactionsAsync(portfolio.Id, authToken, ApiBaseUrl);
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

		return portfolio;
	}

	[Inject]
	private IJSRuntime JS { get; set; } = default!;
	private IJSObjectReference jsLocalStorage = default!;

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

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender)
		{
			HideUI = true;

			Lazy<Task<IJSObjectReference>> moduleTask = new (() => JS.InvokeAsync<IJSObjectReference>(
				"import", $"./_content/{typeof(MyPortfolioDetails).Assembly.GetName().Name!}/js/local-storage.js")
				.AsTask());
			jsLocalStorage = await moduleTask.Value;
			var activeTab = await jsLocalStorage.InvokeAsync<string>("LocalStoreGet", "MyPortfolioDetails-active-tab");
			ActiveTab = string.IsNullOrEmpty(activeTab) ? TabIdHoldings : activeTab;

			Markets = await LoadMarketsAsync(await GetAuthTokenAsync());

			SelectedPortfolio = await LoadPortfolioAsync(Id, await GetAuthTokenAsync());
			if (SelectedPortfolio == null)
			{
				return;
			}

			HideUI = false;

			var (alertType, alertMessage) = GetPassedMessageFromQuery();
			if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
				ShowAlert(alertType, alertMessage);
			else
				CloseAlert();
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
			// fire a background task
			await Task.Run(async () =>
			{
				HideUI = true;
				Markets = await LoadMarketsAsync(await GetAuthTokenAsync());
				SelectedPortfolio = await LoadPortfolioAsync(Id, await GetAuthTokenAsync());
				HideUI = false;

				var (alertType, alertMessage) = GetPassedMessageFromQuery();
				if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
				{
					ShowAlert(alertType, alertMessage);
				}
				else
				{
					CloseAlert();
				}
				await Task.CompletedTask;
			});
		}
	}

	private string ActiveTab { get; set; } = TabIdHoldings;
	private const string TabIdHoldings = "nav-holdings-tab";
	private const string TabIdTransactions = "nav-tx-tab";
	private const string TabIdRoi = "nav-roi-tab";

	private async void SwitchTab(string tab)
	{
		await jsLocalStorage.InvokeAsync<string>("LocalStoreSet", "MyPortfolioDetails-active-tab", tab);
	}
}
