using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioSummary : CBase
{
	public const string TX_DATETIME_FORMAT = "dd-MMM-yyyy HH:mm";
	public const string TX_DATETIME_FORMAT_2 = "dd-MM-yyyy HH:mm";
	public const string TX_DATETIME_FORMAT_3 = "dd-MMMM-yyyy HH:mm";

	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;
	[Parameter]
	public PortfolioResp? Portfolio { get; set; }

	[Parameter]
	public IEnumerable<TxSettlementResp>? RoiRecords { get; set; }
	private Dictionary<string, TxSettlementResp> RoiRecordsMap => RoiRecords?.ToDictionary(r => r.Id, r => r) ?? [];

	private PnlSummaryResp? PnlSummary { get; set; }

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	private MarketDefResp? DefaultMarket => RoiRecords?.FirstOrDefault(a => a.Market!=null).Market;

	private CreateOrUpdateTxSettlementReq Rec = default!;
	private string TxTime { get; set; } = string.Empty;
	private string RecId { get; set; } = string.Empty;

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender)
		{
			Lazy<Task<IJSObjectReference>> moduleTask = new (() => JS.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(CPortfolioSummary).Assembly.GetName().Name!}/js/datetime-picker.js").AsTask());
			var module = await moduleTask.Value;
        	await module.InvokeAsync<string>("InitDatetimePickers");
		}

		if (firstRender && !string.IsNullOrEmpty(PortfolioId) && Portfolio != null && RoiRecords != null)
		{
			var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
			var pnlSummaryResp = await apiClient.GetMyPortfolioPnlSummaryAsync(PortfolioId, await GetAuthTokenAsync(), ApiBaseUrl);
			if (pnlSummaryResp.Status != 200)
				ShowAlert("danger", pnlSummaryResp.Message ?? "Failed to load portfolio PnL summary.");
			else
				PnlSummary = pnlSummaryResp.Data;
			StateHasChanged();
		}
	}

	private bool ValidateRoiRec(CModal? form = null)
	{
		// validate transaction type
		Rec.TxType = Rec.TxType.ToUpper().Trim();
		if (!TxSettlementEntity.TxTypes.Contains(Rec.TxType))
		{
			var (alertType, alertMsg) = ("danger", $"Transaction type must be one of {string.Join(", ", TxSettlementEntity.TxTypes)}, currently '{Rec.TxType}'.");
			if (form != null)
				form.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		// validate time
		Exception? exPartTime = null;
		foreach (var format in new[] { TX_DATETIME_FORMAT, TX_DATETIME_FORMAT_2, TX_DATETIME_FORMAT_3 })
		{
			try
			{
				var time = DateTimeOffset.ParseExact(TxTime, format, System.Globalization.CultureInfo.InvariantCulture);
				Rec.TxTime = time;
				exPartTime = null;
				break;
			}
			catch (Exception e)
			{
				exPartTime = e;
			}
		}
		if (exPartTime != null)
		{
			var (alertType, alertMsg) = ("danger", $"Invalid transaction time: {exPartTime.Message}");
			if (form != null)
				form.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		// validate value, must be positive
		if (Rec.TxValue <= 0.00m)
		{
			var (alertType, alertMsg) = ("danger", "Transaction value must be a positive value.");
			if (form != null)
				form.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		return true;
	}

	private static CreateOrUpdateTxSettlementReq NewRoiRecReqFrom(TxSettlementResp rec)
	{
		return new CreateOrUpdateTxSettlementReq()
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
