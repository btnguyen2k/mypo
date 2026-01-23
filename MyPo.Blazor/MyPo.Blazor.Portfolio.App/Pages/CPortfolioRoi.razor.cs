using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioRoi : CBase
{
	public const string TX_DATETIME_FORMAT = "dd-MMM-yyyy HH:mm";

	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;

	[Parameter]
	public IEnumerable<RoiRecResp>? RoiRecords { get; set; }

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	private CreateOrUpdateRoiRecReq Rec = default!;
	private string TxTime { get; set; } = string.Empty;
	private string RecId { get; set; } = string.Empty;

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender)
		{
			Lazy<Task<IJSObjectReference>> moduleTask = new (() => JS.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(CPortfolioTransactions).Assembly.GetName().Name!}/js/datetime-picker.js").AsTask());
			var module = await moduleTask.Value;
        	await module.InvokeAsync<string>("InitDatetimePickers");
		}

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
