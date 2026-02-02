using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTxBuysSells : CBase
{
	[Parameter]
	public IEnumerable<TxBuySellResp>? Transactions { get; set; }
	private Dictionary<string, TxBuySellResp> TransactionsMap => Transactions?.ToDictionary(t => t.Id, t => t) ?? [];
	private Dictionary<string, TxBuySellResp> SelectedTransactionsMap { get; set; } = [];

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }
	private MarketDefResp? Market => Markets?.FirstOrDefault(m => m.Id == Tx.MarketId);

	[Parameter]
	public PortfolioResp? Portfolio { get; set; }

	private CreateOrUpdateTxBuySellReq Tx = default!;
	private string TxTime { get; set; } = string.Empty;
	private string TxId { get; set; } = string.Empty;

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (firstRender)
		{
			Lazy<Task<IJSObjectReference>> moduleTask = new (() => JS.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(CPortfolioTxBuysSells).Assembly.GetName().Name!}/js/datetime-picker.js").AsTask());
			var module = await moduleTask.Value;
        	await module.InvokeAsync<string>("InitDatetimePickers");
		}
	}

	private bool ValidateTx(CModal? form = null)
	{
		// validate transaction type
		Tx.Type = Tx.Type.ToUpper().Trim();
		if (!Tx.IsSettled && !TxBuySellEntity.TxTypes.Contains(Tx.Type))
		{
			var (alertType, alertMsg) = ("danger", $"Transaction type must be one of {string.Join(", ", TxBuySellEntity.TxTypes)}, currently '{Tx.Type}'.");
			if (form != null)
				form.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		// validate time
		var parsedDatetime = PortfolioUtils.ParseDateTimeOffsetFromDateTimePicker(TxTime);
		if (parsedDatetime == null)
		{
			var (alertType, alertMsg) = ("danger", $"Invalid transaction time format: {TxTime}");
			if (form != null)
				form.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}
		Tx.Time = parsedDatetime.Value;

		// validate item, must not be empty
		Tx.ItemType = Tx.ItemType.ToUpper().Trim();
		if (!Tx.IsSettled && string.IsNullOrEmpty(Tx.ItemType))
		{
			var (alertType, alertMsg) = ("danger", "Item type must not be empty.");
			if (form != null)
				form.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		// validate item code, must not be empty
		Tx.ItemCode = Tx.ItemCode.ToUpper().Trim();
		if (!Tx.IsSettled && string.IsNullOrEmpty(Tx.ItemCode))
		{
			var (alertType, alertMsg) = ("danger", "Item code must not be empty.");
			if (form != null)
				form.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		// validate price, must be positive
		if (!Tx.IsSettled && Tx.Price <= 0.00m)
		{
			var (alertType, alertMsg) = ("danger", "Price must be a positive value.");
			if (form != null)
				form.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		// validate quantity, must be positive
		if (!Tx.IsSettled && Tx.Quantity <= 0)
		{
			var (alertType, alertMsg) = ("danger", "Quantity must be a positive value.");
			if (form != null)
				form.ShowAlert(alertType, alertMsg);
			else
				ShowAlert(alertType, alertMsg);
			return false;
		}

		return true;
	}

	private void AutoGenTxNotes()
	{
		if (TxBuySellEntity.TxTypes.Contains(Tx.Type) && Tx.ItemType==TxBuySellEntity.ITEM_TYPE_STOCK && !string.IsNullOrWhiteSpace(Tx.ItemCode))
		{
			Tx.Notes = $"{(Tx.Type==TxBuySellEntity.TX_TYPE_BUY?"Bought":"Sold")} {Tx.Quantity} {Tx.ItemCode.Trim().ToUpper()} share(s).";
		}
	}

	private void TxCheckboxClicked(string txid)
	{
		if (!SelectedTransactionsMap.Remove(txid))
		{
			TxBuySellResp? tx = TransactionsMap.TryGetValue(txid, out var t) ? t : null;
			if (tx != null)
			{
				SelectedTransactionsMap[txid] = tx.Value;
			}
		}
	}
}
