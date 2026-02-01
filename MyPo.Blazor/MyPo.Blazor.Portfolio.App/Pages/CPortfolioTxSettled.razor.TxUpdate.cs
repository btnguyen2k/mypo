using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTxSettled
{
	private CModal ModalDialogUpdateRecord { get; set; } = default!;

	private void PrepareUpdateRecord(TxSettlementResp rec)
	{
		Tx = NewRoiRecReqFrom(rec);
		TxTime = Tx.TxTime.ToString(TX_DATETIME_FORMAT);
		TxId = rec.Id;
		CloseAlert();
	}

	private void BtnClickUpdateRecord(string rid)
	{
		if (Portfolio?.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You do not have permission to update ROI records in this portfolio.");
			return;
		}
		TxSettlementResp? selectedRec = TxSettlementsMap.TryGetValue(rid, out var rec) ? rec : null;
		if (selectedRec != null)
		{
			PrepareUpdateRecord(selectedRec.Value);
			ModalDialogUpdateRecord.Open();
		}
		else
		{
			ShowAlert("danger", $"ROI record '{rid}' not found.");
		}
	}

	private void BtnClickUpdateRecordClose()
	{
		ModalDialogUpdateRecord.Close();
		ModalDialogUpdateRecord.CloseAlert();
	}

	private async void BtnClickUpdateRecordSave()
	{
		ModalDialogUpdateRecord.ShowAlert("info", "Saving ROI record...");
		if (!ValidateTx(ModalDialogAddRecord))
		{
			return;
		}

		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioTxSettlementAsync(Tx, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogUpdateRecord.ShowAlert("danger", resp.Message ?? $"Error updating ROI record '{TxId}'.");
			return;
		}
		ModalDialogUpdateRecord.ShowAlert("success", "ROI record updated successfully. Navigating to portfolio page...");
		var passAlertMessage = $"ROI record '{TxId}' updated successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogUpdateRecord.Close();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio?.Id, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
