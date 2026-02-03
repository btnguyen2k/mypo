using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTxBuysSells
{
	private CModal ModalDialogSettleTxMultiple { get; set; } = default!;

	private void PrepareSettleTxMultiple()
	{
		var keys = SelectedTransactionsMap.Keys.ToArray();
		foreach (var key in keys.Where(k => TransactionsMap.TryGetValue(k, out var tx) && tx.IsSettled))
		{
			SelectedTransactionsMap.Remove(key);
		}
	}

	private void BtnClickSettleTxMultiple()
	{
		if (Portfolio?.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You do not have permission to add transactions to this portfolio.");
			return;
		}
		PrepareSettleTxMultiple();
		ModalDialogSettleTxMultiple.Open();
		if (SelectedTransactionsMap.Count == 0)
		{
			ModalDialogSettleTxMultiple.ShowAlert("warning", "No transactions selected for settlement.");
		}
		else
		{
			ModalDialogSettleTxMultiple.ShowAlert("info", $"{SelectedTransactionsMap.Count} transaction(s) selected for settlement.");
		}
	}

	private void BtnClickSettleTxMultipleClose()
	{
		ModalDialogSettleTxMultiple.Close();
		ModalDialogSettleTxMultiple.CloseAlert();
	}

	private async void BtnClickSettleTxMultipleConfirm()
	{
		var txList = SelectedTransactionsMap.Values.OrderBy(t => t.Time).ToList();
		ModalDialogSettleTxMultiple.ShowAlert("info", $"Settling {txList.Count} transaction(s)...");

		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var (numTxDone, numTxTotal) = (0, txList.Count);
		foreach (var tx in txList)
		{
			var txReq = NewTxReqFrom(tx);
			ModalDialogSettleTxMultiple.ShowAlert("info", $"Settling transaction '{txReq.Id}'...");
			var resp = await apiClient.SettleMyPortfolioTxAsync(txReq, await GetAuthTokenAsync(), ApiBaseUrl);
			if (resp.Status != 200)
			{
				ModalDialogSettleTxMultiple.ShowAlert("danger", $"Failed to settle transaction '{tx.Id}': {resp.Message ?? "Unknown error"}");
				break;
			}
			numTxDone++;
		}

		var (passAlertType, passAlertMessage) = ("success", $"All {numTxTotal} selected transaction(s) settled successfully.");
		if (numTxDone != numTxTotal)
		{
			ModalDialogSettleTxMultiple.ShowAlert("warning", $"Failed to settle all selected transaction(s). {numTxDone} out of {numTxTotal} settled.");
			(passAlertType, passAlertMessage) = ("warning", $"Failed to settle all selected transaction(s). {numTxDone} out of {numTxTotal} settled.");
		}
		else
		{
			ModalDialogSettleTxMultiple.ShowAlert("success", $"All {numTxTotal} selected transaction(s) settled successfully. Navigating to portfolio page...");
		}
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogSettleTxMultiple.Close();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio!.Id, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
