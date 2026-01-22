using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioRoi : CBase
{
	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;

	[Parameter]
	public IEnumerable<RoiRecResp>? RoiRecords { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender && !string.IsNullOrEmpty(PortfolioId) && RoiRecords != null)
		{
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var pnlSummary = await apiClient.GetMyPortfolioPnlSummaryAsync(PortfolioId, await GetAuthTokenAsync(), ApiBaseUrl);
			if (pnlSummary.Status != 200)
			{
				ShowAlert("danger", pnlSummary.Message ?? "Failed to load portfolio PnL summary.");
			}
		}
	}
}
