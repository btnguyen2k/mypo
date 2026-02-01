using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioSummary : CBase
{
	[Parameter]
	public PortfolioResp? Portfolio { get; set; }

	[Parameter]
	public IEnumerable<TxSettlementResp>? TxSettlements { get; set; }

	private PnlSummaryResp? PnlSummary { get; set; }

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	private MarketDefResp? DefaultMarket => TxSettlements?.FirstOrDefault(a => a.Market!=null).Market;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender && Portfolio != null && TxSettlements != null)
		{
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var pnlSummaryResp = await apiClient.GetMyPortfolioPnlSummaryAsync(Portfolio.Id, await GetAuthTokenAsync(), ApiBaseUrl);
			if (pnlSummaryResp.Status != 200)
				ShowAlert("danger", pnlSummaryResp.Message ?? "Failed to load portfolio PnL summary.");
			else
				PnlSummary = pnlSummaryResp.Data;
			StateHasChanged();
		}
	}
}
