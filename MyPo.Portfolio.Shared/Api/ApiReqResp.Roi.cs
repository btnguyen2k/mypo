using System.Text.Json.Serialization;
using Microsoft.Identity.Client;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct CreateOrUpdateRoiRecReq
{
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; set; }

	[JsonPropertyName("status"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Status { get; set; }

	[JsonPropertyName("portfolio_id")]
	public string PortfolioId { get; set; }

	[JsonPropertyName("tx_type")]
	public string TxType { get; set; }

	[JsonPropertyName("tx_time")]
	public DateTimeOffset TxTime { get; set; }

	[JsonPropertyName("tx_value")]
	public decimal TxValue { get; set; }

	[JsonPropertyName("tx_desc"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TxDesc { get; set; }

	[JsonPropertyName("ref_tx_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefTxId { get; set; }

	[JsonPropertyName("ref_item_code"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefItemType { get; set; }

	[JsonPropertyName("ref_item_type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefItemCode { get; set; }

	[JsonPropertyName("ref_market_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefMarketId { get; set; }
}

public struct RoiRecResp
{
	public static RoiRecResp BuildFrom(RoiRec r, MarketDef? market = null)
	{
		var roiResp = new RoiRecResp()
		{
			Id = r.Id,
			Status = r.Status,
			PortfolioId = r.PortfolioId,
			TxType = r.TxType,
			TxTime = r.TxTime,
			TxValue = r.TxValue,
			TxDesc = r.TxDesc,
			RefTxId = r.RefTxId,
			RefItemType = r.RefItemType,
			RefItemCode = r.RefItemCode,
			RefMarketId = r.RefMarketId,
			Market = market != null ? MarketDefResp.BuildFrom(market) : null
		};
		if (market!=null)
		{
			roiResp.TxTime = TimeZoneInfo.ConvertTime(roiResp.TxTime, market.TZ);
		}
		return roiResp;
	}

	[JsonPropertyName("id")]
	public string Id { get; set; }
	[JsonPropertyName("status")]
	public string Status { get; set; }

	[JsonPropertyName("portfolio_id")]
	public string PortfolioId { get; set; }

	[JsonPropertyName("tx_type")]
	public string TxType { get; set; }
	[JsonPropertyName("tx_time")]
	public DateTimeOffset TxTime { get; set; }
	[JsonPropertyName("tx_value")]
	public decimal TxValue { get; set; }
	[JsonPropertyName("tx_desc"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TxDesc { get; set; }

	[JsonPropertyName("ref_tx_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefTxId { get; set; }
	[JsonPropertyName("ref_item_type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefItemType { get; set; }
	[JsonPropertyName("ref_item_code"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefItemCode { get; set; }
	[JsonPropertyName("ref_market_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? RefMarketId { get; set; }
	[JsonPropertyName("market"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public MarketDefResp? Market { get; set; }
}

public struct PnlSummaryResp
{
	public static PnlSummaryResp BuildFrom(PnlSummary pnl)
	{
		return new PnlSummaryResp()
		{
			PortfolioId = pnl.PortfolioId,
			TotalBuyValue = pnl.TotalBuyValue,
			TotalSellValue = pnl.TotalSellValue,
			TotalDividends = pnl.TotalDividends,
			TotalTax = pnl.TotalTax,
			TotalFees = pnl.TotalFees,
			TotalCashIn = pnl.TotalCashIn,
			TotalCashOut = pnl.TotalCashOut,
			TotalInterest = pnl.TotalInterest,
		};
	}

	[JsonPropertyName("portfolio_id")]
	public string PortfolioId { get; set; }

	[JsonPropertyName("total_buy_value")]
	public decimal TotalBuyValue { get; set; }

	[JsonPropertyName("total_sell_value")]
	public decimal TotalSellValue { get; set; }

	[JsonPropertyName("total_dividends")]
	public decimal TotalDividends { get; set; }

	[JsonPropertyName("total_tax")]
	public decimal TotalTax { get; set; }

	[JsonPropertyName("total_fees")]
	public decimal TotalFees { get; set; }

	[JsonPropertyName("total_cash_in")]
	public decimal TotalCashIn { get; set; }

	[JsonPropertyName("total_cash_out")]
	public decimal TotalCashOut { get; set; }

	[JsonPropertyName("total_interest")]
	public decimal TotalInterest { get; set; }

	public readonly decimal TotalMoneyIn => TotalSellValue + TotalCashIn + TotalDividends + TotalInterest;
	public readonly decimal TotalMoneyOut => TotalBuyValue + TotalCashOut + TotalTax + TotalFees;

	public readonly decimal NetCapitalContributed => TotalCashIn - TotalCashOut;
	public readonly decimal TotalIncome => TotalDividends + TotalInterest;
	public readonly decimal TotalCosts => TotalTax + TotalFees;
	public readonly decimal RealizedCapitalGains => TotalSellValue - TotalBuyValue;
	public readonly decimal GrossReturns => TotalIncome + RealizedCapitalGains;
	public readonly decimal NetReturns => GrossReturns - TotalCosts;
	public readonly decimal NetPnL => NetReturns;
	public readonly decimal ROI => NetCapitalContributed != 0 ? (NetPnL / NetCapitalContributed * 100) : 0;

	public readonly PnlSummary ToModel()
	{
		return new PnlSummary()
		{
			PortfolioId = this.PortfolioId,
			TotalBuyValue = this.TotalBuyValue,
			TotalSellValue = this.TotalSellValue,
			TotalDividends = this.TotalDividends,
			TotalTax = this.TotalTax,
			TotalFees = this.TotalFees,
			TotalCashIn = this.TotalCashIn,
			TotalCashOut = this.TotalCashOut,
			TotalInterest = this.TotalInterest,
		};
	}
}
