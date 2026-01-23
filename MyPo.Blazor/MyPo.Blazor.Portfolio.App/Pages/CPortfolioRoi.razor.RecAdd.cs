using MyPo.Blazor.App.Shared;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Pages;

public partial class CPortfolioRoi
{
	private CModal ModalDialogAddRecord { get; set; } = default!;

	private void PrepareAddRecord()
	{
		Rec = new CreateOrUpdateRoiRecReq()
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
		PrepareAddRecord();
		ModalDialogAddRecord.Open();
	}

	private void BtnClickAddRecordClose()
	{
		ModalDialogAddRecord.Close();
		CloseAlert();
	}
}
