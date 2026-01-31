using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTransactions : CBase
{
	public const string TX_DATETIME_FORMAT = "dd-MMM-yyyy HH:mm";
	public const string TX_DATETIME_FORMAT_2 = "dd-MM-yyyy HH:mm";
	public const string TX_DATETIME_FORMAT_3 = "dd-MMMM-yyyy HH:mm";

	[Parameter]
	public IEnumerable<TransactionRecResp>? Transactions { get; set; }
	private Dictionary<string, TransactionRecResp> TransactionsMap => Transactions?.ToDictionary(t => t.Id, t => t) ?? [];
	private Dictionary<string, TransactionRecResp> SelectedTransactionsMap { get; set; } = [];

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }
	private MarketDefResp? Market => Markets?.FirstOrDefault(m => m.Id == Tx.MarketId);

	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;
	[Parameter]
	public PortfolioRecResp? Portfolio { get; set; }

	private CreateOrUpdateTransactionRecReq Tx = default!;
	private string TxTime { get; set; } = string.Empty;
	private string TxId { get; set; } = string.Empty;

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
	}

	private bool ValidateTx(CModal? form = null)
	{
		// validate transaction type
		Tx.Type = Tx.Type.ToUpper().Trim();
		if (!Tx.IsSettled && !TransactionRec.TxTypes.Contains(Tx.Type))
		{
			var (alertType, alertMsg) = ("danger", $"Transaction type must be one of {string.Join(", ", TransactionRec.TxTypes)}, currently '{Tx.Type}'.");
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
				Tx.Time = time;
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
		if (TransactionRec.TxTypes.Contains(Tx.Type) && Tx.ItemType==TransactionRec.ITEM_TYPE_STOCK && !string.IsNullOrWhiteSpace(Tx.ItemCode))
		{
			Tx.Notes = $"{(Tx.Type==TransactionRec.TXTYPE_BUY?"Bought":"Sold")} {Tx.Quantity} {Tx.ItemCode.Trim().ToUpper()} share(s).";
		}
	}

	private void TxCheckboxClicked(string txid)
	{
		if (!SelectedTransactionsMap.Remove(txid))
		{
			TransactionRecResp? tx = TransactionsMap.TryGetValue(txid, out var t) ? t : null;
			if (tx != null)
			{
				SelectedTransactionsMap[txid] = tx.Value;
			}
		}
	}
}
