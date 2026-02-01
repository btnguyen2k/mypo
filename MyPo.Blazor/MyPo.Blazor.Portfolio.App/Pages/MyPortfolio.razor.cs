using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolio : BasePage
{
	private CModal ModalDialogDelete { get; set; } = default!;
	private IEnumerable<PortfolioResp>? MyActivePortfolioList { get; set; }
	private IEnumerable<PortfolioResp>? MyInactivePortfolioList { get; set; }

	private Dictionary<string, PortfolioResp>? MyPortfolioMap { get; set; }
	private PortfolioResp? SelectedPortfolio { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			ShowAlert("info", "Loading portfolio...");
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var result = await apiClient.GetMyPortfoliosAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (result.Status == 200)
			{
				HideUI = false;
				var allPortfolios = result.Data ?? [];
				MyPortfolioMap = allPortfolios.ToDictionary(p => p.Id);
				var portfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);
				MyActivePortfolioList = portfolioTree.Where(p => p.IsActive);
				MyInactivePortfolioList = portfolioTree.Where(p => !p.IsActive);

				var (alertType, alertMessage) = GetPassedMessageFromQuery();
				if (!string.IsNullOrEmpty(alertMessage) && !string.IsNullOrEmpty(alertType))
				{
					ShowAlert(alertType, alertMessage, ALERT_AUTO_CLOSE_MS);
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
		SelectedPortfolio = MyPortfolioMap?[pid];
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", pid, StringComparison.OrdinalIgnoreCase));
	}

	private void BtnClickModify(string pid)
	{
		SelectedPortfolio = MyPortfolioMap?[pid];
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_MODIFY.Replace("{PortfolioId}", pid, StringComparison.OrdinalIgnoreCase));
	}

	private void BtnClickDelete(string pid)
	{
		SelectedPortfolio = MyPortfolioMap?[pid];
		if (SelectedPortfolio == null)
		{
			ShowAlert("danger", "Selected portfolio not found.");
			return;
		}
		if (SelectedPortfolio.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You are not authorized to delete this portfolio.");
			return;
		}
		ModalDialogDelete.Open();
	}

	private void BtnClickDeleteClose()
	{
		ModalDialogDelete.Close();
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
