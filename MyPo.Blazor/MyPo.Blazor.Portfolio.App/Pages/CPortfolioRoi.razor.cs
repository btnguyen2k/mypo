using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioRoi : CBase
{
	public const string TX_DATETIME_FORMAT = "dd-MMM-yyyy HH:mm";

	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;
	[Parameter]
	public PortfolioRecResp? Portfolio { get; set; }

	[Parameter]
	public IEnumerable<RoiRecResp>? RoiRecords { get; set; }
	private Dictionary<string, RoiRecResp> RoiRecordsMap => RoiRecords?.ToDictionary(r => r.Id, r => r) ?? [];

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

		// if (firstRender && !string.IsNullOrEmpty(PortfolioId) && Portfolio != null && RoiRecords != null)
		// {
		// 	var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		// 	var pnlSummary = await apiClient.GetMyPortfolioPnlSummaryAsync(PortfolioId, await GetAuthTokenAsync(), ApiBaseUrl);
		// 	if (pnlSummary.Status != 200)
		// 	{
		// 		ShowAlert("danger", pnlSummary.Message ?? "Failed to load portfolio PnL summary.");
		// 	}
		// }
	}

	private bool ValidateRoiRec()
	{
		// validate transaction type
		Rec.TxType = Rec.TxType.ToUpper().Trim();
		if (!RoiRec.TxTypes.Contains(Rec.TxType))
		{
			ShowAlert("danger", $"Invalid transaction type: {Rec.TxType}");
			return false;
		}

		// validate time
		try
		{
			var time = DateTimeOffset.ParseExact(TxTime, TX_DATETIME_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
			Rec.TxTime = time;
		}
		catch (Exception e)
		{
			ShowAlert("danger", $"Invalid transaction time: {e.Message}");
			return false;
		}

		// validate value, must be positive
		if (Rec.TxValue <= 0.00m)
		{
			ShowAlert("danger", "Transaction value must be a positive value.");
			return false;
		}

		return true;
	}

	private static CreateOrUpdateRoiRecReq NewRoiRecReqFrom(RoiRecResp rec)
	{
		return new CreateOrUpdateRoiRecReq()
		{
			Id = rec.Id,
			Status = rec.Status,
			PortfolioId = rec.PortfolioId,
			TxType = rec.TxType,
			TxTime = rec.TxTime,
			TxValue = rec.TxValue,
			RefTxId = rec.RefTxId,
			RefItemType = rec.RefItemType,
			RefItemCode = rec.RefItemCode,
			RefMarketId = rec.RefMarketId,
			TxDesc = rec.TxDesc,
		};
	}
}
