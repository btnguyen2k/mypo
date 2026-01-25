using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioAdd : BasePage
{
	private string ParentPortfolioId { get; set; } = string.Empty;
	private string Name { get; set; } = string.Empty;
	private string Description { get; set; } = string.Empty;
	private string Currency { get; set; } = string.Empty;

	private IEnumerable<PortfolioRecResp> MyPortfolioTree = [];

	private void BtnClickCancel()
	{
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO);
	}

	private async Task BtnClickSaveAndOpen()
	{
		await BtnClickSave(true);
	}

	private async Task BtnClickSave(bool openAfterCreate = false)
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
			ParentId = ParentPortfolioId,
		};
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.CreatePortfolioAsync(req, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			HideUI = false;
			ShowAlert("danger", resp.Message ?? "Error creating the portfolio.");
			return;
		}
		ShowAlert("success", "Portfolio created successfully. Navigating to my portfolio page...");
		var passAlertMessage = $"Portfolio '{req.Name}' created successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO}?alertMessage={passAlertMessage}&alertType={passAlertType}";
		if (openAfterCreate)
		{
			var pid = resp.Data?.Id ?? string.Empty;
			nextUrl = PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", pid, StringComparison.OrdinalIgnoreCase);
			NavigationManager.NavigateTo($"{nextUrl}?alertMessage={passAlertMessage}&alertType={passAlertType}");
		}
		else
		{
			NavigationManager.NavigateTo(nextUrl);
		}
	}

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
				var allPortfolios = result.Data ?? [];
				MyPortfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);
				HideUI = false;
				CloseAlert();
			}
			else
			{
				ShowAlert("danger", result.Message ?? "Unknown error");
			}
		}
	}
}
