using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTxSettled
{
	private CModal ModalDialogAddRecord { get; set; } = default!;

	private void PrepareAddRecord()
	{
		Rec = new CreateOrUpdateTxSettlementReq()
		{
			PortfolioId = PortfolioId,
			TxType = string.Empty,
			TxTime = DateTimeOffset.Now,
			RefItemType = string.Empty,
			RefItemCode = string.Empty,
			RefMarketId = string.Empty,
		};
		TxTime = Rec.TxTime.ToString(TX_DATETIME_FORMAT);
		RecId = string.Empty;
		CloseAlert();
	}

	private void BtnClickAddRecord()
	{
		if (Portfolio?.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You do not have permission to add ROI records to this portfolio.");
			return;
		}
		PrepareAddRecord();
		ModalDialogAddRecord.Open();
	}

	private void BtnClickAddRecordClose()
	{
		ModalDialogAddRecord.Close();
		ModalDialogAddRecord.CloseAlert();
	}

	private async void BtnClickAddRecordSave()
	{
		ModalDialogAddRecord.ShowAlert("info", "Adding ROI record...");
		if (!ValidateRoiRec(ModalDialogAddRecord))
		{
			return;
		}

		Rec.PortfolioId = PortfolioId;
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.CreateMyPortfolioTxSettlementAsync(Rec, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogAddRecord.ShowAlert("danger", resp.Message ?? "Failed to create ROI record.");
			return;
		}
		ModalDialogAddRecord.ShowAlert("success", "ROI record added successfully. Navigating to portfolio page...");
		var passAlertMessage = $"ROI record '{resp.Data.Id}' added successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogAddRecord.Close();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
