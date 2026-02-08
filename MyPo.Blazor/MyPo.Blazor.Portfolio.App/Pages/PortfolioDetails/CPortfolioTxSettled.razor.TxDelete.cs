using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioTxSettled
{
	private CModal ModalDialogDeleteRecord { get; set; } = default!;

	private void PrepareDeleteRecord(TxSettlementResp rec)
	{
		PrepareUpdateRecord(rec);
	}

	private void BtnClickDeleteRecord(string rid)
	{
		if (Portfolio?.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You do not have permission to delete Settlement records from this portfolio.");
			return;
		}
		TxSettlementResp? selectedRec = TxSettlementsMap.TryGetValue(rid, out var rec) ? rec : null;
		if (selectedRec != null)
		{
			PrepareDeleteRecord(selectedRec.Value);
			ModalDialogDeleteRecord.Open();
		}
		else
		{
			ShowAlert("danger", $"Settlement record '{rid}' not found.");
		}
	}

	private void BtnClickDeleteRecordClose()
	{
		ModalDialogDeleteRecord.Close();
		ModalDialogDeleteRecord.CloseAlert();
	}

	private async void BtnClickDeleteRecordConfirm()
	{
		ModalDialogDeleteRecord.ShowAlert("info", "Deleting Settlement record...");
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.DeleteMyPortfolioTxSettlementAsync(Tx.PortfolioId, TxId, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogDeleteRecord.ShowAlert("danger", resp.Message ?? $"Error deleting Settlement record '{TxId}'.");
			return;
		}
		ModalDialogDeleteRecord.ShowAlert("success", "Settlement record deleted successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Settlement record '{TxId}' deleted successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogDeleteRecord.Close();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio?.Id, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
