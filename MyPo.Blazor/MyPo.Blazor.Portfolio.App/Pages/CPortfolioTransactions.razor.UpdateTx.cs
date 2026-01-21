using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTransactions : CBase
{
	private CModal ModalDialogUpdateTx { get; set; } = default!;

	private void PrepareUpdateTx(TransactionRecResp tx)
	{
		Tx = NewTxReqFrom(tx);
		TxTime = Tx.Time.ToString(TX_DATETIME_FORMAT);
		TxId = tx.Id;
		CloseAlert();
	}

	private void BtnClickUpdateTx(string tid)
	{
		TransactionRecResp? selectedTx = TransactionsMap.TryGetValue(tid, out var tx) ? tx : null;
		if (selectedTx != null)
		{
			PrepareUpdateTx(selectedTx.Value);
			ModalDialogUpdateTx.Open();
		}
		else
		{
			ShowAlert("danger", $"Transaction '{tid}' not found.");
		}
	}

	private void BtnClickUpdateTxClose()
	{
		ModalDialogUpdateTx.Close();
		CloseAlert();
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
			ShowAlert("danger", resp.Message ?? $"Error updating transaction '{TxId}'.");
			return;
		}
		ShowAlert("success", "Transaction updated successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{TxId}' updated successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogUpdateTx.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
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
			ShowAlert("danger", resp.Message ?? $"Error settling transaction '{TxId}'.");
			return;
		}
		ShowAlert("success", "Transaction settled successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{TxId}' settled successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogUpdateTx.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}
}
