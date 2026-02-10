using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioTxBuysSells
{
	private CModal ModalDialogUpdateTx { get; set; } = default!;

	private void PrepareUpdateTx(TxBuySellResp tx)
	{
		Tx = NewTxReqFrom(tx);
		TxTime = Tx.Time.ToString(PortfolioUtils.DEFAULT_DATETIME_PICKER_FORMAT);
		TxId = tx.Id;
		CloseAlert();
	}

	private void BtnClickUpdateTx(string tid)
	{
		if (Portfolio?.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You do not have permission to update transactions in this portfolio.");
			return;
		}
		TxBuySellResp? selectedTx = TransactionsMap.TryGetValue(tid, out var tx) ? tx : null;
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
		ModalDialogUpdateTx.CloseAlert();
	}

	private async void BtnClickUpdateTxSave()
	{
		ModalDialogUpdateTx.ShowAlert("info", "Saving transaction...");
		if (!ValidateTx(ModalDialogUpdateTx))
		{
			return;
		}

		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioTxBuySellAsync(Tx, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogUpdateTx.ShowAlert("danger", resp.Message ?? $"Error updating transaction '{TxId}'.");
			return;
		}
		ModalDialogUpdateTx.ShowAlert("success", "Transaction updated successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{TxId}' updated successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogUpdateTx.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio!.Id, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}

	private async void BtnClickUpdateTxSettle()
	{
		ModalDialogUpdateTx.ShowAlert("info", "Settling transaction...");
		if (!ValidateTx())
		{
			return;
		}

		if (Tx.IsSettled)
		{
			ModalDialogUpdateTx.ShowAlert("warning", "Transaction has already been settled.");
			return;
		}

		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.SettleMyPortfolioTxAsync(Tx, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogUpdateTx.ShowAlert("danger", resp.Message ?? $"Error settling transaction '{TxId}'.");
			return;
		}
		ModalDialogUpdateTx.ShowAlert("success", "Transaction settled successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{TxId}' settled successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogUpdateTx.Close();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio!.Id, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
