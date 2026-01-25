using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTransactions : CBase
{
	public const string TX_DATETIME_FORMAT = "dd-MMM-yyyy HH:mm";

	[Parameter]
	public IEnumerable<TransactionRecResp>? Transactions { get; set; }
	private Dictionary<string, TransactionRecResp> TransactionsMap => Transactions?.ToDictionary(t => t.Id, t => t) ?? [];
	private Dictionary<string, TransactionRecResp> SelectedTransactionsMap { get; set; } = [];

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;

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

	private bool ValidateTx()
	{
		// validate transaction type, must be either BUY or SELL
		Tx.Type = Tx.Type.ToUpper().Trim();
		if (!Tx.IsSettled && (Tx.Type != "BUY" && Tx.Type != "SELL"))
		{
			ShowAlert("danger", $"Transaction type must be either 'BUY' or 'SELL', currently '{Tx.Type}'.");
			return false;
		}

		// validate time
		try
		{
			var time = DateTimeOffset.ParseExact(TxTime, TX_DATETIME_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
			Tx.Time = time;
		}
		catch (Exception e)
		{
			ShowAlert("danger", $"Invalid transaction time: {e.Message}");
			return false;
		}

		// validate item, must not be empty
		Tx.ItemType = Tx.ItemType.ToUpper().Trim();
		if (!Tx.IsSettled && string.IsNullOrEmpty(Tx.ItemType))
		{
			ShowAlert("danger", "Item type must not be empty.");
			return false;
		}

		// validate item code, must not be empty
		Tx.ItemCode = Tx.ItemCode.ToUpper().Trim();
		if (!Tx.IsSettled && string.IsNullOrEmpty(Tx.ItemCode))
		{
			ShowAlert("danger", "Item code must not be empty.");
			return false;
		}

		// validate price, must be positive
		if (!Tx.IsSettled && Tx.Price <= 0.00m)
		{
			ShowAlert("danger", "Price must be a positive value.");
			return false;
		}

		// validate quantity, must be positive
		if (!Tx.IsSettled && Tx.Quantity <= 0)
		{
			ShowAlert("danger", "Quantity must be a positive value.");
			return false;
		}
		return true;
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
