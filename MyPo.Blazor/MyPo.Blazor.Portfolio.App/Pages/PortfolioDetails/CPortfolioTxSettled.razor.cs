using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioTxSettled : CBase
{
	[Parameter]
	public PortfolioResp? Portfolio { get; set; }

	[Parameter]
	public IEnumerable<TxSettlementResp>? TxSettlements { get; set; }
	private Dictionary<string, TxSettlementResp> TxSettlementsMap => TxSettlements?.ToDictionary(r => r.Id, r => r) ?? [];

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	private CreateOrUpdateTxSettlementReq Tx = default!;
	private string TxTime { get; set; } = string.Empty;
	private string TxId { get; set; } = string.Empty;

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender)
		{
			Lazy<Task<IJSObjectReference>> moduleTask = new (() => JS.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(CPortfolioTxSettled).Assembly.GetName().Name!}/js/datetime-picker.js").AsTask());
			var module = await moduleTask.Value;
        	await module.InvokeAsync<string>("InitDatetimePickers");
		}
	}

	private void AutoGenTxNotes()
	{
		if (TxSettlementEntity.TxTypes.Contains(Tx.TxType) && Tx.TxValue > 0)
		{
			Tx.TxDesc = Tx.TxType switch
			{
				TxSettlementEntity.TX_TYPE_DIVIDEND => $"Dividend from {Tx.RefItemCode?.ToUpper()}",
				TxSettlementEntity.TX_TYPE_DISTRIBUTION => $"Distribution from {Tx.RefItemCode?.ToUpper()}",
				_ => Tx.TxDesc,
			};
		}
	}

	private bool ValidateTx(CModal? activeForm = null)
	{
		// validate transaction type
		Tx.TxType = Tx.TxType.ToUpper().Trim();
		if (!TxSettlementEntity.TxTypes.Contains(Tx.TxType))
		{
			var (alertType, alertMsg) = ("danger", $"Transaction type must be one of {string.Join(", ", TxSettlementEntity.TxTypes)}, currently '{Tx.TxType}'.");
			if (activeForm != null)
				activeForm.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		// validate time
		var parsedDatetime = PortfolioUtils.ParseDateTimeOffsetFromDateTimePicker(TxTime);
		if (parsedDatetime == null)
		{
			var (alertType, alertMsg) = ("danger", $"Invalid transaction time format: {TxTime}");
			if (activeForm != null)
				activeForm.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}
		Tx.TxTime = parsedDatetime.Value;

		// validate value, must be positive
		if (Tx.TxValue <= 0.00m)
		{
			var (alertType, alertMsg) = ("danger", "Transaction value must be a positive value.");
			if (activeForm != null)
				activeForm.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		return true;
	}

	private static CreateOrUpdateTxSettlementReq NewTxSettlementReqFrom(TxSettlementResp rec)
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
