using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioModify : BasePage
{
	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;

	private string ParentPortfolioId { get; set; } = string.Empty;
	private string Name { get; set; } = string.Empty;
	private string Description { get; set; } = string.Empty;
	private string Currency { get; set; } = string.Empty;
	private bool IsActive { get; set; } = true;

	private IEnumerable<PortfolioRecResp> MyPortfolioTree = [];
	// private Dictionary<string, PortfolioRecResp>? MyPortfolioMap { get; set; }
	// private PortfolioRecResp? SelectedPortfolio { get; set; }

	private async Task<PortfolioRecResp?> LoadPortfolioAsync(string id, string authToken)
	{
		HideUI = true;
		var errorMsg = $"Portfolio '{id}' not found.";
		ShowAlert("info", "Loading portfolio, please wait...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var result = await apiClient.GetMyPortfolioAsync(authToken, ApiBaseUrl);
		if (result.Status != 200)
		{
			errorMsg = result.Message ?? "Error loading portfolio.";
		}
		else
		{
			var allPortfolios = result.Data ?? [];
			MyPortfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);
			var myPortfolioMap = allPortfolios.ToDictionary(p => p.Id);
			if (myPortfolioMap.TryGetValue(id, out var portfolio))
			{
				return portfolio;
			}
		}
		ShowAlert("danger", errorMsg);
		return null;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			var portfolio = await LoadPortfolioAsync(PortfolioId, await GetAuthTokenAsync());
			if (portfolio == null)
			{
				return;
			}
			ParentPortfolioId = portfolio.ParentId ?? string.Empty;
			Name = portfolio.Name;
			Description = portfolio.Description ?? string.Empty;
			Currency = portfolio.Currency;
			IsActive = portfolio.IsActive;

			HideUI = false;
			CloseAlert();
		}
	}

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
			Id = PortfolioId,
			Name = Name.Trim(),
			Description = Description.Trim(),
			Currency = Currency.ToUpper().Trim(),
			ParentId = ParentPortfolioId,
			IsActive = IsActive,
		};
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioAsync(PortfolioId, req, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			HideUI = false;
			ShowAlert("danger", resp.Message ?? "Error updating the portfolio.");
			return;
		}
		ShowAlert("success", "Portfolio updated successfully. Navigating to my portfolio page...");
		var passAlertMessage = $"Portfolio '{req.Name}' updated successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		NavigationManager.NavigateTo($"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO}?alertMessage={passAlertMessage}&alertType={passAlertType}");
	}
}
