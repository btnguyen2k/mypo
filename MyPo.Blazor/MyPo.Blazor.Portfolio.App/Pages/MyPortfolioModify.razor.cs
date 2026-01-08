using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using System.Text.Json;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioModify : BasePage
{
	[Parameter]
	public string Id { get; set; } = string.Empty;

	private string Name { get; set; } = string.Empty;
	private string Description { get; set; } = string.Empty;
	private string Currency { get; set; } = string.Empty;
	private bool IsActive { get; set; } = true;

	private Dictionary<string, PortfolioRecResp>? MyPortfolioMap { get; set; }
	private PortfolioRecResp? SelectedPortfolio { get; set; }

	private async Task<PortfolioRecResp?> LoadPortfolioAsync(string id, string authToken)
	{
		HideUI = true;
		var errorMsg = $"Portfolio '{id}' not found.";
		ShowAlert("info", "Loading portfolio, please wait...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var result = await apiClient.GetMyPortfolioAsync(authToken, ApiBaseUrl);
		if (result.Status != 200)
		{
			errorMsg = result.Message!;
		}
		else
		{
			var allPortfolios = result.Data ?? [];
			MyPortfolioMap = allPortfolios.ToDictionary(p => p.Id);
			var portfolioTree = PortfolioUtils.BuildPortfolioTree(allPortfolios);
			if (MyPortfolioMap.TryGetValue(id, out var portfolio))
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
			SelectedPortfolio = await LoadPortfolioAsync(Id, await GetAuthTokenAsync());
			if (SelectedPortfolio == null)
			{
				return;
			}
			Console.WriteLine($"Loaded portfolio: {JsonSerializer.Serialize(SelectedPortfolio.Value)}");
			Name = SelectedPortfolio!.Value.Name;
			Description = SelectedPortfolio!.Value.Description ?? string.Empty;
			Currency = SelectedPortfolio!.Value.Currency;
			IsActive = SelectedPortfolio!.Value.IsActive;

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
			Name = Name.Trim(),
			Description = Description.Trim(),
			Currency = Currency.ToUpper().Trim(),
			ParentId = null,
			IsActive = IsActive,
		};
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioAsync(Id, req, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			HideUI = false;
			ShowAlert("danger", resp.Message!);
			return;
		}
		ShowAlert("success", "Portfolio updated successfully. Navigating to my portfolio page...");
		var passAlertMessage = $"Portfolio '{req.Name}' updated successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		NavigationManager.NavigateTo($"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO}?alertMessage={passAlertMessage}&alertType={passAlertType}");
	}
}
