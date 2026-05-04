using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioPlans : BasePage
{
	private CModal ModalDialogDelete { get; set; } = default!;

	private readonly Dictionary<string, MarketDefResp> MarketsMap = [];
	private readonly Dictionary<string, PortfolioPlanResp> PortfolioPlansMap = [];
	private PortfolioPlanResp? SelectedPortfolioPlan { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			HideUI = true;
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			MarketsMap.Clear();
			PortfolioPlansMap.Clear();

			ShowAlert("info", "Loading market info...");
			var marketResult = await apiClient.GetMarketsAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (!marketResult.IsSuccess)
			{
				ShowAlert("danger", marketResult.Message ?? "Error loading market info.");
				return;
			}
			foreach (var market in marketResult.Data ?? [])
			{
				MarketsMap[market.Id.ToUpper()] = market;
			}

			ShowAlert("info", "Loading portfolio plans...");
			var resultPortfolioPlans = await apiClient.GetMyPortfolioPlansAsync(await GetAuthTokenAsync(), ApiBaseUrl);
			if (!resultPortfolioPlans.IsSuccess)
			{
				ShowAlert("danger", resultPortfolioPlans.Message ?? "Error loading portfolio plans.");
				return;
			}
			foreach (var plan in resultPortfolioPlans.Data ?? [])
			{
				plan.Market = MarketsMap.GetValueOrDefault(plan.Portfolio?.Metadata?.DefaultMarketId?.ToUpper() ?? string.Empty);
				PortfolioPlansMap[plan.Id] = plan;
			}

			HideUI = false;
			ShowPassedMessageOrCloseAlert();
			await Task.Run(UpdatePortfolioPlansInBackground);
		}
	}

	private async void UpdatePortfolioPlansInBackground()
	{
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var plansList = PortfolioPlansMap.Values.ToList();
		foreach (var plan in plansList)
		{
			var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			if (plan.Portfolio == null || plan.Metadata == null || now-plan.Metadata.MetadataRefreshTimestamp < 24*3600)
			{
				continue;
			}
			SetBackgroundMsg($"⌛Updating portfolio plan '{plan.Name}'...");
			var req = new CreateOrUpdatePortfolioPlanReq()
			{
				Id = plan.Id,
				PortfolioId = plan.PortfolioId,
				Name = plan.Name,
				Metadata = plan.Metadata,
			};
			var updateResp = await apiClient.UpdateMyPortfolioPlanAsync(plan.Id, req, await GetAuthTokenAsync(), ApiBaseUrl);
			if (!updateResp.IsSuccess)
			{
				SetBackgroundMsg($"❗Failed to update portfolio plan '{plan.Name}': {updateResp.Message}");
				return;
			}
			var updatedPlan = updateResp.Data!;
			updatedPlan.Market = MarketsMap.GetValueOrDefault(plan.Portfolio.Metadata?.DefaultMarketId?.ToUpper() ?? string.Empty);
			PortfolioPlansMap[plan.Id] = updatedPlan;
			StateHasChanged();
		}
		ClearBackgroundMsg();
	}

	private void BtnClickAddPlan()
	{
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_ADD);
	}

	private void BtnClickViewPlan(string id)
	{
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_VIEW.Replace("{PlanId}", id, StringComparison.OrdinalIgnoreCase));
	}

	private void BtnClickEditPlan(string id)
	{
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_EDIT.Replace("{PlanId}", id, StringComparison.OrdinalIgnoreCase));
	}

	private void BtnClickDeletePlan(string id)
	{
		SelectedPortfolioPlan = PortfolioPlansMap?[id];
		if (SelectedPortfolioPlan == null)
		{
			ShowAlert("danger", "Selected portfolio plan not found.");
			return;
		}
		if (SelectedPortfolioPlan.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You are not authorized to delete this portfolio.");
			return;
		}
		ModalDialogDelete.Open();
	}

	private void BtnClickDeletePlanClose()
	{
		ModalDialogDelete.Close();
	}

	private async void BtnClickDeletePlanConfirm()
	{
		ModalDialogDelete.Close();
		HideUI = true;
		ShowAlert("info", $"Deleting portfolio plan '{SelectedPortfolioPlan?.Name}', please wait...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var result = await apiClient.DeleteMyPortfolioPlanAsync(SelectedPortfolioPlan?.Id ?? string.Empty, await GetAuthTokenAsync(), ApiBaseUrl);
		HideUI = false;
		if (!result.IsSuccess)
		{
			ShowAlert("danger", result.Message ?? "Error deleting portfolio plan.");
			return;
		}

		await OnAfterRenderAsync(true);
		ShowAlert("success", $"Portfolio plan '{SelectedPortfolioPlan?.Name}' deleted successfully.", autoCloseAfterMs: ALERT_AUTO_CLOSE_MS);
	}
}
