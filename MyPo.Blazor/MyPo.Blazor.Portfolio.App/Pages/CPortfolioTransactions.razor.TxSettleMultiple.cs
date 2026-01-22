using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTransactions
{
	private CModal ModalDialogSettleTxMultiple { get; set; } = default!;

	private void PrepareSettleTxMultiple()
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

	private void BtnClickSettleTxMultiple()
	{
		PrepareSettleTxMultiple();
		if (SelectedTransactionsMap.Count == 0)
		{
			ShowAlert("warning", "No transactions selected for settlement.");
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
			var resp = await apiClient.SettleTransactionAsync(txReq.Id!, txReq, await GetAuthTokenAsync(), ApiBaseUrl);
			if (resp.Status != 200)
			{
				ShowAlert("danger", $"Failed to settle transaction '{tx.Id}': {resp.Message ?? "Unknown error"}");
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
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl, forceLoad: false);
	}
}
