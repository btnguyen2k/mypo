using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTransactions
{
	private CModal ModalDialogDeleteTx { get; set; } = default!;

	private void PrepareDeleteTx(TransactionRecResp tx)
	{
		PrepareUpdateTx(tx);
	}

	private void BtnClickDeleteTx(string tid)
	{
		TransactionRecResp? selectedTx = TransactionsMap.TryGetValue(tid, out var tx) ? tx : null;
		if (selectedTx != null)
		{
			PrepareDeleteTx(selectedTx.Value);
			ModalDialogDeleteTx.Open();
		}
		else
		{
			ShowAlert("danger", $"Transaction '{tid}' not found.");
		}
	}

	private void BtnClickDeleteTxClose()
	{
		ModalDialogDeleteTx.Close();
		CloseAlert();
	}

	private async void BtnClickDeleteTxConfirm()
	{
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.DeleteTransactionAsync(Tx.PortfolioId, TxId, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ShowAlert("danger", resp.Message ?? $"Error deleting transaction '{TxId}'.");
			return;
		}
		ShowAlert("success", "Transaction deleted successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{TxId}' deleted successfully.";
		var passAlertType = "success";
		await Task.Delay(500);
		ModalDialogDeleteTx.Close();
		CloseAlert();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}
}
