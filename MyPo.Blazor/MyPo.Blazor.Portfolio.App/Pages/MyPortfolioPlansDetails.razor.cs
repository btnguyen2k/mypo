using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using System.Text.Json;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioPlansDetails : BasePage
{
	[Parameter]
	public string PlanId { get; set; } = string.Empty;
	private PortfolioPlanResp SelectedPortfolioPlan { get; set; } = default!;

	private CModal ModalDialogDelete { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();

			ShowAlert("info", "Loading portfolio plan...");
			var planResp = await apiClient.GetMyPortfolioPlanByIdAsync(PlanId, await GetAuthTokenAsync(), ApiBaseUrl);
			if (!planResp.IsSuccess || planResp.Data is null)
			{
				ShowAlert("danger", planResp.Message ?? "Error loading portfolio plan.");
				return;
			}
			SelectedPortfolioPlan = planResp.Data;

			ShowAlert("info", "Loading market info...");
			var marketResult = await apiClient.GetMarketsAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (!marketResult.IsSuccess)
			{
				ShowAlert("danger", marketResult.Message ?? "Error loading market info.");
				return;
			}
			SelectedPortfolioPlan.Market = marketResult.Data?.FirstOrDefault(m => string.Equals(m.Id, SelectedPortfolioPlan.Portfolio?.Metadata?.DefaultMarketId, StringComparison.OrdinalIgnoreCase));

			HideUI = false;
			ShowPassedMessageOrCloseAlert();
		}
	}

	private void BtnClickEdit()
	{
		var id = SelectedPortfolioPlan.Id;
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_EDIT.Replace("{PlanId}", id, StringComparison.OrdinalIgnoreCase));
	}

	private void BtnClickDelete()
	{
		if (SelectedPortfolioPlan.OwnerUserId.Equals(CurrentUser?.Id, StringComparison.Ordinal))
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
		ShowAlert("info", $"Deleting portfolio plan '{SelectedPortfolioPlan.Name}', please wait...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var result = await apiClient.DeleteMyPortfolioPlanAsync(SelectedPortfolioPlan.Id, await GetAuthTokenAsync(), ApiBaseUrl);
		if (!result.IsSuccess)
		{
			HideUI = false;
			ShowAlert("danger", result.Message ?? "Error deleting portfolio plan.");
			return;
		}

		ShowAlert("success", $"Portfolio plan '{SelectedPortfolioPlan.Name}' deleted successfully. Navigating to my portfolio plans page...");
		var passAlertMessage = $"Portfolio plan '{SelectedPortfolioPlan.Name}' deleted successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS}?alertMessage={passAlertMessage}&alertType={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}

	private async void BtnClickAnalyze()
	{
		ShowAlert("info", $"Analyzing portfolio plan '{SelectedPortfolioPlan.Name}', please wait...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var result = await apiClient.AnalyzePortfolioPlanAsync(SelectedPortfolioPlan.Id, await GetAuthTokenAsync(), ApiBaseUrl);
		if (!result.IsSuccess || result.Data is null)
		{
			ShowAlert("danger", result.Message ?? "Error analyzing portfolio plan.");
			return;
		}

		SelectedPortfolioPlan.Metadata ??= new();
		SelectedPortfolioPlan.Metadata.AnalysisRefreshTimestsmp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		SelectedPortfolioPlan.Metadata.Analysis = result.Data.Analysis;
		ShowAlert("success", $"Portfolio plan '{SelectedPortfolioPlan.Name}' analyzed successfully.");
	}
}
