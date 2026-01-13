using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTransactions : BaseComponent
{
	[Parameter]
	public IEnumerable<TransactionRecResp>? Transactions { get; set; }
	private Dictionary<string, TransactionRecResp> TransactionsMap => Transactions?.ToDictionary(t => t.Id, t => t) ?? [];
	// private TransactionRecResp SelectedTx { get; set; } = default!;

	[Parameter]
	public IEnumerable<MarketDefResp>? Markets { get; set; }
	// private Dictionary<string, MarketDefResp> MarketsMap { get; set; } = default!;

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

	private CreateOrUpdateTransactionRecReq Tx = default!;
	private string TxTime { get; set; } = string.Empty;
	private string TxId { get; set; } = string.Empty;

	private void InitAddTx()
	{
		Tx = new()
		{
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
		};
		TxTime = Tx.Time.ToString("dd-MMM-yyyy HH:mm");
		CloseAlert();
	}

	private void InitUpdateTx(TransactionRecResp tx)
	{
		Tx = new()
		{
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
		TxTime = Tx.Time.ToString("dd-MMM-yyyy HH:mm");
		TxId = tx.Id;
		CloseAlert();
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

	private bool validateTxForUpdateOrSettle()
	{
		// validate transaction type, must be either BUY or SELL
		Tx.Type = Tx.Type.ToUpper().Trim();
		if (!Tx.IsSettled && (Tx.Type != "BUY" && Tx.Type != "SELL"))
		{
			ShowAlert("danger", $"Transaction type must be either 'BUY' or 'SELL', currently '{Tx.Type}'.");
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

	private async void BtnClickUpdateTxSave()
	{
		if (!validateTxForUpdateOrSettle())
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
		NavigationManager.NavigateTo($"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{id}", PortfolioId, StringComparison.OrdinalIgnoreCase)}?{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}", forceLoad: true);
		ModalDialogAddTx.Close();
	}

	private async void BtnClickUpdateTxSettle()
	{
		await Task.CompletedTask;
		ModalDialogUpdateTx.Close();
	}

	private void BtnClickAddTx()
	{
		InitAddTx();
		ModalDialogAddTx.Open();
	}

	private async void BtnClickAddTxSave()
	{
		// validate transaction type, must be either BUY or SELL
		Tx.Type = Tx.Type.ToUpper().Trim();
		if (Tx.Type != "BUY" && Tx.Type != "SELL")
		{
			ShowAlert("danger", $"Transaction type must be either 'BUY' or 'SELL', currently '{Tx.Type}'.");
			return;
		}

		// validate item, must not be empty
		Tx.ItemType = Tx.ItemType.ToUpper().Trim();
		if (string.IsNullOrEmpty(Tx.ItemType))
		{
			ShowAlert("danger", "Item type must not be empty.");
			return;
		}

		// validate item code, must not be empty
		Tx.ItemCode = Tx.ItemCode.ToUpper().Trim();
		if (string.IsNullOrEmpty(Tx.ItemCode))
		{
			ShowAlert("danger", "Item code must not be empty.");
			return;
		}

		// validate price, must be positive
		if (Tx.Price <= 0.00m)
		{
			ShowAlert("danger", "Price must be a positive value.");
			return;
		}

		// validate quantity, must be positive
		if (Tx.Quantity <= 0)
		{
			ShowAlert("danger", "Quantity must be a positive value.");
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
		NavigationManager.NavigateTo($"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{id}", PortfolioId, StringComparison.OrdinalIgnoreCase)}?{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}", forceLoad: true);
		ModalDialogAddTx.Close();
	}
}
