using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioTxSettled
{
	private CModal ModalDialogUpdateRecord { get; set; } = default!;

	private void PrepareUpdateRecord(TxSettlementResp rec)
	{
		Tx = NewTxSettlementReqFrom(rec);
		TxTime = Tx.TxTime.ToString(PortfolioUtils.DEFAULT_DATETIME_PICKER_FORMAT);
		TxId = rec.Id;
		CloseAlert();
	}

	private void BtnClickUpdateRecord(string rid)
	{
		if (Portfolio?.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You do not have permission to update Settlement records in this portfolio.");
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
			ShowAlert("danger", $"Settlement record '{rid}' not found.");
		}
	}

	private void BtnClickUpdateRecordClose()
	{
		ModalDialogUpdateRecord.Close();
		ModalDialogUpdateRecord.CloseAlert();
	}

	private async void BtnClickUpdateRecordSave()
	{
		ModalDialogUpdateRecord.ShowAlert("info", "Saving Settlement record...");
		if (!ValidateTx(ModalDialogAddRecord))
		{
			return;
		}

		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.UpdateMyPortfolioTxSettlementAsync(Tx, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogUpdateRecord.ShowAlert("danger", resp.Message ?? $"Error updating Settlement record '{TxId}'.");
			return;
		}
		ModalDialogUpdateRecord.ShowAlert("success", "Settlement record updated successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Settlement record '{TxId}' updated successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogUpdateRecord.Close();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio!.Id, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
