using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Blazor.App.Shared;
using MyPo.Blazor.Portfolio.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioTransactions
{
	private CModal ModalDialogAddTx { get; set; } = default!;

	private void PrepareAddTx()
	{
		Tx = new()
		{
			PortfolioId = PortfolioId,
			Type = string.Empty,
			// MarketId = string.Empty,
			Time = DateTimeOffset.Now,
			ItemType = string.Empty,
			ItemCode = string.Empty,
			Price = 0.00m,
			Quantity = 0,
			FeeTx = 0.00m,
			FeeTax = 0.00m,
			FeeOther = 0.00m,
			// Notes = string.Empty
			IsSettled = false,
		};
		TxTime = Tx.Time.ToString(TX_DATETIME_FORMAT);
		TxId = string.Empty;
		CloseAlert();
	}

	private static CreateOrUpdateTransactionRecReq NewTxReqFrom(TransactionRecResp tx)
	{
		return new CreateOrUpdateTransactionRecReq()
		{
			Id = tx.Id,
			PortfolioId = tx.PortfolioId,
			Type = tx.Type,
			MarketId = tx.MarketId,
			Time = tx.Time,
			ItemType = tx.ItemType,
			ItemCode = tx.ItemCode,
			Price = tx.Price,
			Quantity = tx.Quantity,
			FeeTx = tx.FeeTx,
			FeeTax = tx.FeeTax,
			FeeOther = tx.FeeOther,
			Notes = tx.Notes,
			IsSettled = tx.IsSettled,
		};
	}

	private void BtnClickAddTx()
	{
		PrepareAddTx();
		ModalDialogAddTx.Open();
	}

	private void BtnClickAddTxClose()
	{
		ModalDialogAddTx.Close();
		CloseAlert();
	}

	private async void BtnClickAddTxSave()
	{
		ModalDialogAddTx.ShowAlert("info", "Adding transaction...");
		if (!ValidateTx())
		{
			return;
		}

		Tx.PortfolioId = PortfolioId;
		var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
		var resp = await apiClient.CreateMyPortfolioTxAsync(Tx, await GetAuthTokenAsync(), ApiBaseUrl);
		if (resp.Status != 200)
		{
			ModalDialogAddTx.ShowAlert("danger", resp.Message ?? "Failed to add transaction.");
			return;
		}
		ModalDialogAddTx.ShowAlert("success", "Transaction added successfully. Navigating to portfolio page...");
		var passAlertMessage = $"Transaction '{resp.Data.Id}' added successfully.";
		var passAlertType = "success";
		await Task.Delay(PortfolioUIGlobals.AFTER_ACTION_DELAY_MS);
		ModalDialogAddTx.Close();
		var nextUrl = $"{PortfolioUIGlobals.ROUTE_PORTFOLIO_MY_PORTFOLIO_DETAILS.Replace("{PortfolioId}", PortfolioId, StringComparison.OrdinalIgnoreCase)}"
			+ $"?{BasePage.QUERY_PARM_REFRESH}=true"
			+ $"&{BasePage.QUERY_PARM_ALERT_MESSAGE}={passAlertMessage}"
			+ $"&{BasePage.QUERY_PARM_ALERT_TYPE}={passAlertType}";
		NavigationManager.NavigateTo(nextUrl);
	}
}
