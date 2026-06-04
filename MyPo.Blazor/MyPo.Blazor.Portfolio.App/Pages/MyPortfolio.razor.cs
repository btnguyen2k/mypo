using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolio : BasePage
{
	private IEnumerable<PortfolioResp>? MyPortfolioList { get; set; }
	private IEnumerable<PortfolioResp>? MyActivePortfolioList { get; set; }
	private IEnumerable<PortfolioResp>? MyInactivePortfolioList { get; set; }

	private Dictionary<string, PortfolioResp>? MyPortfolioMap { get; set; }
	private PortfolioResp? SelectedPortfolio { get; set; }
	private Dictionary<string, PnlSummaryResp> PortfolioPnlSummaryMap { get; set; } = [];

	private CModal ModalDialogDelete { get; set; } = default!;

	[Inject]
	private ILogger<MyPortfolio>? Logger { get; set; }

	private readonly List<MarketDefResp> Markets = [];

	private async void FetchPortfolioPnlSummaryInBackground()
	{
		PortfolioPnlSummaryMap.Clear();
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		foreach (var portfolio in MyPortfolioMap!.Values)
		{
			await Task.Run(async () =>
			{
				SetBackgroundMsg($"⌛Fetching PnL summary for portfolio {portfolio.Name}...");
				var result = await apiClient.GetMyPortfolioPnlSummaryAsync(portfolio.Id, await GetAuthTokenAsync(), ApiBaseUrl);
				if (!result.IsSuccess)
				{
					Logger?.LogWarning("Failed to fetch PnL summary for portfolio {PortfolioId}: {ErrorMessage}", portfolio.Id, result.Message);
					SetBackgroundMsg($"❗Failed to fetch PnL summary for portfolio {portfolio.Name}: {result.Message}");
				}
				else
				{
					PortfolioPnlSummaryMap[portfolio.Id] = result.Data;
					SetBackgroundMsg($"✅Fetched PnL summary for portfolio '{portfolio.Name}'.");
					StateHasChanged();
				}
			});
		}
		ClearBackgroundMsg();
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

			ShowAlert("info", "Loading portfolio...");
			var resultPortfolio = await apiClient.GetMyPortfoliosAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (resultPortfolio.Status == 200)
			{
				HideUI = false;
				var allPortfolios = resultPortfolio.Data ?? [];
				MyPortfolioMap = allPortfolios.ToDictionary(p => p.Id);
				MyPortfolioList = PortfolioUtils.BuildPortfolioTree(allPortfolios);
				MyActivePortfolioList = MyPortfolioList.Where(p => p.IsActive);
				MyInactivePortfolioList = MyPortfolioList.Where(p => !p.IsActive);

				var (alertType, alertMessage) = GetPassedMessageFromQuery();
				if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
				{
					ShowAlert(alertType, alertMessage, ALERT_AUTO_CLOSE_MS);
				}
				else
				{
					CloseAlert();
				}
				await Task.Run(FetchPortfolioPnlSummaryInBackground);
			}
			else
			{
				ShowAlert("danger", resultPortfolio.Message ?? "Error loading portfolios.");
			}
		}
	}

	private void BtnClickAdd()
	{
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_ADD);
	}

	public void OnClickDeletePortfolio(PortfolioResp p)
	{
		SelectedPortfolio = p;
		ModalDialogDelete.Open();
	}

	private void BtnClickDeleteClose()
	{
		ModalDialogDelete.Close();
	}

	private async void BtnClickDeleteConfirm()
	{
		ModalDialogDelete.Close();
		@HideUI = true;
		ShowAlert("info", $"Deleting portfolio '{SelectedPortfolio?.Name}', please wait...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var result = await apiClient.DeleteMyPortfolioAsync(SelectedPortfolio?.Id ?? string.Empty, await GetAuthTokenAsync(), ApiBaseUrl);
		HideUI = false;
		if (!result.IsSuccess)
		{
			ShowAlert("danger", result.Message ?? "Error deleting portfolio.");
		}
		else
		{
			await OnAfterRenderAsync(true);
			ShowAlert("success", $"Portfolio '{SelectedPortfolio?.Name}' deleted successfully.", ALERT_AUTO_CLOSE_MS);
		}
	}
}
