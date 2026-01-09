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

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			SelectedPortfolio = await LoadPortfolioAsync(Id, await GetAuthTokenAsync());
			if (SelectedPortfolio == null)
			{
				return;
			}
			HideUI = false;
			CloseAlert();
		}
	}
}
