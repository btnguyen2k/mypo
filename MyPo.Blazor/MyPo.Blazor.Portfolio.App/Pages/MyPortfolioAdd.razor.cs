using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioAdd : BasePage
{
	private string Name { get; set; } = string.Empty;
	private string Description { get; set; } = string.Empty;
	private string Currency { get; set; } = string.Empty;

	private void BtnClickCancel()
	{
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO);
	}

	private async Task BtnClickSave()
	{
		HideUI = true;
		ShowAlert("info", "Please wait...");

		// Validate name
		if (string.IsNullOrWhiteSpace(Name))
		{
			HideUI = false;
			ShowAlert("warning", "Name is required.");
			return;
		}

		// Validate currency
		if (string.IsNullOrWhiteSpace(Currency))
		{
			HideUI = false;
			ShowAlert("warning", "Currency is required.");
			return;
		}

		var req = new CreateOrUpdatePortfolioRecReq
		{
			Name = Name.Trim(),
			Description = Description.Trim(),
			Currency = Currency.ToUpper().Trim(),
			ParentId = null,
		};
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.CreatePortfolioAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			HideUI = false;
			ShowAlert("danger", resp.Message!);
			return;
		}
		ShowAlert("success", "Portfolio created successfully. Navigating to my portfolio page...");
		var passAlertMessage = $"Portfolio '{req.Name}' created successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		NavigationManager.NavigateTo($"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO}?alertMessage={passAlertMessage}&alertType={passAlertType}");
	}
}
