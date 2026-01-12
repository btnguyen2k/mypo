using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioDetails : BasePage
{
	[Parameter]
	public string Id { get; set; } = string.Empty;
	private PortfolioRecResp? SelectedPortfolio { get; set; }
	private Dictionary<string, PortfolioRecResp>? MyPortfolioMap { get; set; }
	private IEnumerable<TransactionRecResp>? Transactions { get; set; }
	private IEnumerable<MarketDefResp>? Markets { get; set; }

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
		MyPortfolioMap = allPortfolios.ToDictionary(p => p.Id);
		var portfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);
		PortfolioRecResp portfolio = default!;
		if (!MyPortfolioMap.TryGetValue(id, out portfolio))
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
		Console.WriteLine($"Loaded {Transactions.Count()} transactions for portfolio '{portfolio.Name}'.");

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

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;

			Markets = await LoadMarketsAsync(await GetAuthTokenAsync());

			SelectedPortfolio = await LoadPortfolioAsync(Id, await GetAuthTokenAsync());
			if (SelectedPortfolio == null)
			{
				return;
			}

			HideUI = false;
			CloseAlert();
		}
	}

	private static void BtnClickAddTx()
	{
		Console.WriteLine("Add transaction button clicked.");
	}
}
