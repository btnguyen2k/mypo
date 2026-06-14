using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct CreateOrUpdateTxSettlementReq
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

public struct TxSettlementResp
{
    public static TxSettlementResp BuildFrom(TxSettlementEntity r, MarketDef? market = null)
    {
        var roiResp = new TxSettlementResp()
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
        if (market != null)
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
            TotalDistributions = pnl.TotalDistributions,
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

    [JsonPropertyName("total_distributions")]
    public decimal TotalDistributions { get; set; }

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

    public readonly decimal TotalMoneyIn => TotalSellValue + TotalCashIn + TotalDividends + TotalDistributions + TotalInterest;
    public readonly decimal TotalMoneyOut => TotalBuyValue + TotalCashOut + TotalTax + TotalFees;

    public readonly decimal NetCapitalContributed => TotalCashIn - TotalCashOut;
    public readonly decimal TotalIncome => TotalDividends + TotalDistributions + TotalInterest;
    public readonly decimal TotalCosts => TotalTax + TotalFees;
    public readonly decimal RealizedCapitalGains => TotalSellValue - TotalBuyValue;
    public readonly decimal GrossReturns => TotalIncome + RealizedCapitalGains;
    public readonly decimal NetReturns => GrossReturns - TotalCosts;
    public readonly decimal NetPnL => NetReturns;
    public readonly decimal UnSetledPnL(decimal marketValue) => NetPnL + marketValue;
    public readonly decimal ROIvsTotalBuy => TotalBuyValue > 0 ? (NetPnL / TotalBuyValue * 100) : 0;
    public readonly decimal UnsettledROIvsTotalBuy(decimal unsettledPnL)
    {
        return TotalBuyValue > 0 ? (unsettledPnL / TotalBuyValue * 100) : 0;
    }

    public readonly decimal UnsettledROIvsPeakCapital(decimal unsettledPnL, IEnumerable<TxSettlementResp> records)
    {
        var peakCapital = PeakCapital(records);
        return peakCapital > 0 ? (unsettledPnL / peakCapital * 100) : 0;
    }

    public readonly decimal UnsettledROIvsAverageCapital(decimal unsettledPnL, IEnumerable<TxSettlementResp> records)
    {
        var averageCapital = AverageCapital(records);
        return averageCapital > 0 ? (unsettledPnL / averageCapital * 100) : 0;
    }

    public readonly decimal ROIvsPeakCapital(IEnumerable<TxSettlementResp> records)
    {
        var peakCapital = PeakCapital(records);
        return peakCapital > 0 ? (NetPnL / peakCapital * 100) : 0;
    }

    public readonly decimal PeakCapital(IEnumerable<TxSettlementResp> records)
    {
        decimal peakCapital = 0;
        decimal cumulativeCapital = 0;

        foreach (var record in records.OrderBy(r => r.TxTime))
        {
            if (record.TxType == TxSettlementEntity.TX_TYPE_BUY)
            {
                cumulativeCapital += record.TxValue;
            }
            else if (record.TxType == TxSettlementEntity.TX_TYPE_SELL)
            {
                cumulativeCapital -= record.TxValue;
            }
            cumulativeCapital = cumulativeCapital > 0 ? cumulativeCapital : 0;
            if (cumulativeCapital > peakCapital)
            {
                peakCapital = cumulativeCapital;
            }
        }
        return peakCapital;
    }

    public readonly decimal ROIvsAverageCapital(IEnumerable<TxSettlementResp> records)
    {
        var averageCapital = AverageCapital(records);
        return averageCapital > 0 ? (NetPnL / averageCapital * 100) : 0;
    }

    public readonly decimal AverageCapital(IEnumerable<TxSettlementResp> records)
    {
        var startDate = "0000-00-00";
        var totalDays = 0;
        decimal totalCapital = 0;
        decimal cumulativeCapital = 0;
        var recordsList = records
            .Where(r => r.TxType == TxSettlementEntity.TX_TYPE_BUY || r.TxType == TxSettlementEntity.TX_TYPE_SELL)
            .OrderBy(r => r.TxTime)
            .ToList();
        var filteredList = recordsList.Append(new TxSettlementResp
        {
            TxTime = recordsList.Count > 0 ? recordsList.Max(r => r.TxTime).AddDays(1) : DateTimeOffset.UtcNow,
            TxType = "END",
            TxValue = 0
        });
        foreach (var tx in filteredList)
        {
            var recDate = tx.TxTime.ToUniversalTime().ToString("yyyy-MM-dd");
            if (startDate == "0000-00-00")
            {
                startDate = recDate;
            }
            else if (recDate != startDate)
            {
                var daysDiff = (DateTime.Parse(recDate) - DateTime.Parse(startDate)).Days;

                totalCapital += cumulativeCapital * daysDiff;
                totalDays += daysDiff;
                startDate = recDate;
            }
            if (tx.TxType == TxSettlementEntity.TX_TYPE_BUY)
            {
                cumulativeCapital += tx.TxValue;
            }
            else if (tx.TxType == TxSettlementEntity.TX_TYPE_SELL)
            {
                cumulativeCapital -= tx.TxValue;
            }
            cumulativeCapital = cumulativeCapital > 0 ? cumulativeCapital : 0;
        }
        return totalDays > 0 ? (totalCapital / totalDays) : 0;
    }

    public readonly PnlSummary ToModel()
    {
        return new PnlSummary()
        {
            PortfolioId = this.PortfolioId,
            TotalBuyValue = this.TotalBuyValue,
            TotalSellValue = this.TotalSellValue,
            TotalDividends = this.TotalDividends,
            TotalDistributions = this.TotalDistributions,
            TotalTax = this.TotalTax,
            TotalFees = this.TotalFees,
            TotalCashIn = this.TotalCashIn,
            TotalCashOut = this.TotalCashOut,
            TotalInterest = this.TotalInterest,
        };
    }
}
