using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioTxSettled
{
	private CModal ModalDialogAddRecord { get; set; } = default!;

	private void PrepareAddRecord()
	{
		Tx = new CreateOrUpdateTxSettlementReq()
		{
			PortfolioId = Portfolio!.Id,
			TxType = string.Empty,
			TxTime = DateTimeOffset.Now,
			RefItemType = string.Empty,
			RefItemCode = string.Empty,
			RefMarketId = string.Empty,
		};
		TxTime = Tx.TxTime.ToString(PortfolioUtils.DEFAULT_DATETIME_PICKER_FORMAT);
		TxId = string.Empty;
		CloseAlert();
	}

	private void BtnClickAddRecord()
	{
		if (Portfolio?.OwnerUserId != CurrentUser?.Id)
		{
			ShowAlert("danger", "You do not have permission to add Settlement records to this portfolio.");
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
		ModalDialogAddRecord.ShowAlert("info", "Adding Settlement record...");
		if (!ValidateTx(ModalDialogAddRecord))
		{
			return;
		}

		Tx.PortfolioId = Portfolio?.Id ?? string.Empty;
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.CreateMyPortfolioTxSettlementAsync(Tx, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogAddRecord.ShowAlert("danger", resp.Message ?? "Failed to create Settlement record.");
			return;
		}
		ModalDialogAddRecord.ShowAlert("success", "Settlement record added successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Settlement record '{resp.Data.Id}' added successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogAddRecord.Close();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", Portfolio!.Id, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
