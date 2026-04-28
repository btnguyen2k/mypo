using MyPo.Portfolio.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class MyPortfolioPlansDetails : BasePage
{
	[Parameter]
	public string PlanId { get; set; } = string.Empty;
	private PortfolioPlanResp? SelectedPortfolioPlan { get; set; }

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
			CloseAlert();
		}
	}

	private void BtnClickEdit()
	{
		var id = SelectedPortfolioPlan?.Id;
		NavigationManager.NavigateTo(PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_PLANS_EDIT.Replace("{PlanId}", id, StringComparison.OrdinalIgnoreCase));
	}
}
