using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolio : BasePage
{
	private CModal ModalDialogDelete { get; set; } = default!;

	private IEnumerable<PortfolioRecResp>? MyActivePortfolioList { get; set; }
	private IEnumerable<PortfolioRecResp>? MyInactivePortfolioList { get; set; }

	private Dictionary<string, PortfolioRecResp>? MyPortfolioMap { get; set; }
	private PortfolioRecResp? SelectedPortfolio { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			ShowAlert("info", "Loading portfolio...");
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var result = await apiClient.GetMyPortfolioAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (result.Status == 200)
			{
				HideUI = false;
				var allPortfolios = result.Data ?? [];
				MyPortfolioMap = allPortfolios.ToDictionary(p => p.Id);
				var portfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);
				MyActivePortfolioList = portfolioTree.Where(p => p.IsActive);
				MyInactivePortfolioList = portfolioTree.Where(p => !p.IsActive);

				var queryParameters = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
				var alertMessage = queryParameters.TryGetValue("alertMessage", out var alertMessageValue) ? alertMessageValue.ToString() : string.Empty;
				var alertType = queryParameters.TryGetValue("alertType", out var alertTypeValue) ? alertTypeValue.ToString() : string.Empty;
				if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
				{
					ShowAlert(alertType, alertMessage);
				}
				else
				{
					CloseAlert();
				}
			}
			else
			{
				ShowAlert("danger", result.Message ?? "Unknown error");
			}
		}
	}

	private void BtnClickAdd()
	{
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_ADD);
	}

	private void BtnClickInfo(string pid)
	{
		// SelectedPortfolio = AppMap?[appId];
		// ModalDialogInfo.Open();
	}

	private void BtnClickModify(string pid)
	{
		SelectedPortfolio = MyPortfolioMap?[pid];
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_MODIFY.Replace("{id}", pid, StringComparison.OrdinalIgnoreCase));
	}

	private void BtnClickDelete(string pid)
	{
		SelectedPortfolio = MyPortfolioMap?[pid];
		ModalDialogDelete.Open();
	}

	private async void BtnClickDeleteConfirm()
	{
		ModalDialogDelete.Close();
		HideUI = true;
		ShowAlert("info", $"Deleting portfolio '{SelectedPortfolio?.Name}', please wait...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var result = await apiClient.DeleteMyPortfolioAsync(SelectedPortfolio?.Id ?? string.Empty, await GetAuthTokenAsync(), ApiBaseUrl);
		HideUI = false;
		if (result.Status == 200)
		{
			await OnAfterRenderAsync(true);
			ShowAlert("success", $"Portfolio '{SelectedPortfolio?.Name}' deleted successfully.");
		}
		else
		{
			ShowAlert("danger", result.Message ?? "Unknown error");
		}
	}
}
