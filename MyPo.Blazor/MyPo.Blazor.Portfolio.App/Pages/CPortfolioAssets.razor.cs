using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioAssets : BaseComponent
{
	[Parameter]
	public IEnumerable<TransactionRecResp>? Transactions { get; set; }
	private Dictionary<string, TransactionRecResp> TransactionsMap => Transactions?.ToDictionary(t => t.Id, t => t) ?? [];
	private Dictionary<string, TransactionRecResp> SelectedTransactionsMap { get; set; } = [];

	[Parameter]
	public IEnumerable<AssetResp>? Assets { get; set; }
	private Dictionary<string, AssetResp> AssetsMap => Assets?.ToDictionary(t => t.Id, t => t) ?? [];

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }

	[Parameter]
	public string PortfolioId { get; set; } = string.Empty;

	private string AlertType { get; set; } = string.Empty;
	private string AlertMessage { get; set; } = string.Empty;
	protected bool AlertHasChanged {get; set; } = false;

	protected void CloseAlert()
	{
		AlertMessage = string.Empty;
		AlertHasChanged = false;
		StateHasChanged();
	}

	protected void ShowAlert(string type, string message)
	{
		var oldAlertType = AlertType;
		var oldAlertMessage = AlertMessage;
		AlertType = type;
		AlertMessage = message;
		AlertHasChanged = !String.IsNullOrEmpty(oldAlertMessage)
			&& (String.Compare(oldAlertMessage, message, MyPo.Shared.Globals.StringComparison) != 0
				|| String.Compare(oldAlertType, type, MyPo.Shared.Globals.StringComparison) != 0);
		StateHasChanged();
	}

	private CModal ModalDialogAddTx { get; set; } = default!;
	private CModal ModalDialogUpdateTx { get; set; } = default!;
	private CModal ModalDialogDeleteTx { get; set; } = default!;
	private CModal ModalDialogSettleTxMultiple { get; set; } = default!;

	private CreateOrUpdateTransactionRecReq Tx = default!;
	private string TxTime { get; set; } = string.Empty;
	private string TxId { get; set; } = string.Empty;

	private void InitSettleTxMultiple()
	{
		var keys = SelectedTransactionsMap.Keys.ToArray();
		foreach (var key in keys)
		{
			if (TransactionsMap.TryGetValue(key, out var tx) && tx.IsSettled)
			{
				SelectedTransactionsMap.Remove(key);
			}
		}
	}

	private void InitAddTx()
	{
		Tx = new()
		{
			PortfolioId = PortfolioId,
			Type = string.Empty,
			// MarketId = string.Empty,
			Time = DateTimeOffset.Now,
			ItemType = string.Empty,
			ItemCode = string.Empty,
			Price = 0.00m,
			Quantity = 0,
			FeeTx = 0.00m,
			FeeTax = 0.00m,
			FeeOther = 0.00m,
			// Notes = string.Empty
			IsSettled = false,
		};
		TxTime = Tx.Time.ToString("dd-MMM-yyyy HH:mm");
		TxId = string.Empty;
		CloseAlert();
	}

	private static CreateOrUpdateTransactionRecReq NewTxReqFrom(TransactionRecResp tx)
	{
		return new CreateOrUpdateTransactionRecReq()
		{
			Id = tx.Id,
			PortfolioId = tx.PortfolioId,
			Type = tx.Type,
			MarketId = tx.MarketId,
			Time = tx.Time,
			ItemType = tx.ItemType,
			ItemCode = tx.ItemCode,
			Price = tx.Price,
			Quantity = tx.Quantity,
			FeeTx = tx.FeeTx,
			FeeTax = tx.FeeTax,
			FeeOther = tx.FeeOther,
			Notes = tx.Notes,
			IsSettled = tx.IsSettled,
		};
	}

	private void InitUpdateTx(TransactionRecResp tx)
	{
		Tx = NewTxReqFrom(tx);
		TxTime = Tx.Time.ToString("dd-MMM-yyyy HH:mm");
		TxId = tx.Id;
		CloseAlert();
	}

	private void InitDeleteTx(TransactionRecResp tx)
	{
		InitUpdateTx(tx);
	}

	[Inject]
	private IJSRuntime JS { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			Lazy<Task<IJSObjectReference>> moduleTask = new (() => JS.InvokeAsync<IJSObjectReference>("import", $"./_content/{typeof(CPortfolioTransactions).Assembly.GetName().Name!}/js/datetime-picker.js").AsTask());
			var module = await moduleTask.Value;
        	await module.InvokeAsync<string>("InitDatetimePickers");

			// MarketsMap = Markets?.ToDictionary(m => m.Id, m => m) ?? [];
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
			var time = DateTimeOffset.ParseExact(TxTime, "dd-MMM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
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

	private void BtnClickUpdateTx(string tid)
	{
		TransactionRecResp? selectedTx = TransactionsMap.TryGetValue(tid, out var tx) ? tx : null;
		if (selectedTx != null)
		{
			InitUpdateTx(selectedTx.Value);
			ModalDialogUpdateTx.Open();
		}
		else
		{
			ShowAlert("danger", $"Transaction '{tid}' not found.");
		}
	}

	private async void BtnClickUpdateTxSave()
	{
		if (!ValidateTx())
		{
			return;
		}

		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateTransactionAsync(TxId, Tx, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ShowAlert("danger", resp.Message!);
			return;
		}
		ShowAlert("success", "Transaction updated successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{resp.Data.Id}' updated successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogUpdateTx.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{id}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}

	private async void BtnClickUpdateTxSettle()
	{
		if (!ValidateTx())
		{
			return;
		}

		if (Tx.IsSettled)
		{
			ShowAlert("warning", "Transaction has already been settled.");
			return;
		}

		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.SettleTransactionAsync(TxId, Tx, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ShowAlert("danger", resp.Message!);
			return;
		}
		ShowAlert("success", "Transaction settled successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{resp.Data.Id}' settled successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogUpdateTx.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{id}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}

	private void BtnClickAddTx()
	{
		InitAddTx();
		ModalDialogAddTx.Open();
	}

	private async void BtnClickAddTxSave()
	{
		if (!ValidateTx())
		{
			return;
		}

		Tx.PortfolioId = PortfolioId;
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.CreateTransactionAsync(Tx, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ShowAlert("danger", resp.Message!);
			return;
		}
		ShowAlert("success", "Transaction added successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{resp.Data.Id}' added successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogAddTx.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{id}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}

	private void BtnClickDeleteTx(string tid)
	{
		TransactionRecResp? selectedTx = TransactionsMap.TryGetValue(tid, out var tx) ? tx : null;
		if (selectedTx != null)
		{
			InitDeleteTx(selectedTx.Value);
			ModalDialogDeleteTx.Open();
		}
		else
		{
			ShowAlert("danger", $"Transaction '{tid}' not found.");
		}
	}

	private async void BtnClickDeleteTxConfirm()
	{
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.DeleteTransactionAsync(Tx.PortfolioId, TxId, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ShowAlert("danger", resp.Message!);
			return;
		}
		ShowAlert("success", "Transaction deleted successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{TxId}' deleted successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogDeleteTx.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{id}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
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

	private void BtnClickSettleTxMultiple()
	{
		InitSettleTxMultiple();
		if (SelectedTransactionsMap.Count == 0)
		{
			ShowAlert("warning", "No transactions valid for settlement.");
		}
		else
		{
			ShowAlert("info", $"{SelectedTransactionsMap.Count} transaction(s) selected for settlement.");
		}
		ModalDialogSettleTxMultiple.Open();
	}

	private async void BtnClickSettleTxMultipleConfirm()
	{
		var txList = SelectedTransactionsMap.Values.OrderBy(t => t.Time).ToList();
		ShowAlert("info", $"Settling {txList.Count} transaction(s)...");

		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var (numTxDone, numTxTotal) = (0, txList.Count);
		foreach (var tx in txList)
		{
			var txReq = NewTxReqFrom(tx);
			ShowAlert("info", $"Settling transaction '{txReq.Id}'...");
			var resp = await apiClient.SettleTransactionAsync(txReq.Id, txReq, await GetAuthTokenAsync(), ApiBaseUrl);
			if (resp.Status != 200)
			{
				ShowAlert("danger", $"Failed to settle transaction '{tx.Id}': {resp.Message}");
				break;
			}
			numTxDone++;
		}

		var (passAlertType, passAlertMessage) = ("success", $"All {numTxTotal} selected transaction(s) settled successfully.");
		if (numTxDone != numTxTotal)
		{
			ShowAlert("warning", $"Failed to settle all selected transaction(s). {numTxDone} out of {numTxTotal} settled.");
			(passAlertType, passAlertMessage) = ("warning", $"Failed to settle all selected transaction(s). {numTxDone} out of {numTxTotal} settled.");
		}
		else
		{
			ShowAlert("success", $"All {numTxTotal} selected transaction(s) settled successfully. Navigating to portfolio page...");
		}
		await Task.Delay(500);
		ModalDialogSettleTxMultiple.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{id}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}
}
