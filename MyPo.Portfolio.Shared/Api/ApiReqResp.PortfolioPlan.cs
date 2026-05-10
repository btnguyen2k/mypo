using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Utils;

namespace MyPo.Portfolio.Shared.Api;

public struct CreateOrUpdatePortfolioPlanReq
{
	public static CreateOrUpdatePortfolioPlanReq NewRequestFrom(PortfolioPlanEntity plan)
	{
		return NewRequestFrom(PortfolioPlanResp.BuildFrom(plan));
	}

	public static CreateOrUpdatePortfolioPlanReq NewRequestFrom(PortfolioPlanResp plan)
	{
		return new CreateOrUpdatePortfolioPlanReq
		{
			Id = plan.Id,
			Type = plan.Type,
			PortfolioId = plan.PortfolioId,
			Name = plan.Name,
			Metadata = plan.Metadata,
		};
	}

	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; set; }

	[JsonPropertyName("type")]
	public string Type { get; set; }

	[JsonPropertyName("portfolio_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PortfolioId { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("metadata"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PortfolioPlanMetadata? Metadata { get; set; }
}

public sealed class PortfolioPlanResp
{
	public static PortfolioPlanResp BuildFrom(PortfolioPlanEntity pr)
	{
		return new PortfolioPlanResp
		{
			Id = pr.Id,
			Type = pr.Type,
			OwnerUserId = pr.OwnerUserId,
			PortfolioId = pr.PortfolioId,
			Name = pr.Name,
			Metadata = pr.Metadata,
		};
	}

	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("type")]
	public string Type { get; set; } = default!;

	[JsonPropertyName("owner_id")]
	public string OwnerUserId { get; set; } = default!;

	[JsonPropertyName("portfolio_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PortfolioId { get; set; }

	[JsonPropertyName("portfolio"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PortfolioResp? Portfolio { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = default!;

	[JsonPropertyName("metadata"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PortfolioPlanMetadata? Metadata { get; set; }

	[JsonIgnore]
	public MarketDefResp? Market { get; set; }

	[JsonIgnore]
	public string HoldingSymbols => Metadata?.HoldingTickers.Select(ht => ht.Ticker.Split(":")[1]).Aggregate((a, b) => $"{a}, {b}") ?? string.Empty;

	[JsonIgnore]
	public decimal TotalMarketValue => Metadata?.HoldingTickers.Sum(ht => ht.Shares * ht.MarketPrice) ?? 0;

	public string TotalMarketValueStr(MarketDefResp? market = null)
	{
		var m = market ?? Market;
		return $"{m?.CurrencySymbol??""} {FormatUtils.FormatValueWithScale(TotalMarketValue, m?.PriceScale??1, m?.ValueFormat??"")}";
	}

	[JsonIgnore]
	public decimal TotalCostBasic => Metadata?.HoldingTickers.Sum(ht => ht.Shares * ht.AveragePrice) ?? 0;

	public string TotalCostBasicStr(MarketDefResp? market = null)
	{
		var m = market ?? Market;
		return $"{m?.CurrencySymbol??""} {FormatUtils.FormatValueWithScale(TotalCostBasic, m?.PriceScale??1, m?.ValueFormat??"")}";
	}

	[JsonIgnore]
	public decimal TotalUnsettledPnL => Metadata?.HoldingTickers.Sum(ht => ht.Shares * (ht.MarketPrice - ht.AveragePrice)) ?? 0;

	public string TotalUnsettledPnLStr(MarketDefResp? market = null)
	{
		var m = market ?? Market;
		return $"{m?.CurrencySymbol??""} {FormatUtils.FormatValueWithScale(TotalUnsettledPnL, m?.PriceScale??1, m?.ValueFormat??"")}";
	}

	[JsonIgnore]
	public decimal TotalUnsettledPnLYield => TotalCostBasic > 0 ? TotalUnsettledPnL / TotalCostBasic * 100 : 0;

	public string TotalUnsettledPnLYieldStr()
	{
		var yield = TotalUnsettledPnLYield;
		return $"{(yield>=0?"+":"")}{yield:N1}%";
	}

	public string NumSharesStr(HoldingTicker ticker) => FormatUtils.FormatValueMaxDecimals(ticker.Shares, 4);

	public decimal AveragePrice(HoldingTicker ticker) => ticker.AveragePrice;

	public string AveragePriceStr(HoldingTicker ticker, MarketDefResp? market = null)
	{
		var m = market ?? Market;
		return $"{m?.CurrencySymbol ?? ""} {FormatUtils.FormatValueWithScale(ticker.AveragePrice, m?.PriceScale ?? 1, m?.ValueFormat ?? "")}";
	}

	public decimal CostBasic(HoldingTicker ticker) => ticker.Shares * ticker.AveragePrice;

	public string CostBasicStr(HoldingTicker ticker, MarketDefResp? market = null)
	{
		var m = market ?? Market;
		var costBasic = CostBasic(ticker);
		return $"{m?.CurrencySymbol ?? ""} {FormatUtils.FormatValueWithScale(costBasic, m?.PriceScale ?? 1, m?.ValueFormat ?? "")}";
	}

	public decimal UnsettledPnL(HoldingTicker ticker) => ticker.Shares * (ticker.MarketPrice - ticker.AveragePrice);

	public string UnsettledPnLStr(HoldingTicker ticker, MarketDefResp? market = null)
	{
		var m = market ?? Market;
		var pnl = UnsettledPnL(ticker);
		return $"{m?.CurrencySymbol ?? ""} {FormatUtils.FormatValueWithScale(pnl, m?.PriceScale ?? 1, m?.ValueFormat ?? "")}";
	}

	public decimal UnsettledPnLYield(HoldingTicker ticker) => CostBasic(ticker) > 0 ? UnsettledPnL(ticker) / CostBasic(ticker) * 100 : 0;

	public string UnsettledPnLYieldStr(HoldingTicker ticker)
	{
		var yield = UnsettledPnLYield(ticker);
		return $"{(yield>=0?"+":"")}{yield:N1}%";
	}

	public string MarketPriceStr(HoldingTicker ticker, MarketDefResp? market = null)
	{
		var m = market ?? Market;
		return $"{m?.CurrencySymbol ?? ""} {FormatUtils.FormatValueWithScale(ticker.MarketPrice, m?.PriceScale ?? 1, m?.ValueFormat ?? "")}";
	}

	public string MarketValueStr(HoldingTicker ticker, MarketDefResp? market = null)
	{
		var m = market ?? Market;
		var marketValue = ticker.Shares * ticker.MarketPrice;
		return $"{m?.CurrencySymbol ?? ""} {FormatUtils.FormatValueWithScale(marketValue, m?.PriceScale ?? 1, m?.ValueFormat ?? "")}";
	}

	public decimal EstYearlyDividend(HoldingTicker ticker) => ticker.EstYearlyDividend;

	public string EstYearlyDividendStr(HoldingTicker ticker, MarketDefResp? market = null)
	{
		var m = market ?? Market;
		var yearlyDividend = ticker.EstYearlyDividend;
		return $"{m?.CurrencySymbol ?? ""} {FormatUtils.FormatValueWithScale(yearlyDividend, m?.PriceScale ?? 1, m?.ValueFormat ?? "")}";
	}

	[JsonIgnore]
	public decimal EstTotalYearlyDividend => Metadata?.HoldingTickers.Sum(ht => ht.EstYearlyDividend) ?? 0;

	public string EstTotalYearlyDividendStr(MarketDefResp? market = null)
	{
		var m = market ?? Market;
		return $"{m?.CurrencySymbol ?? ""} {FormatUtils.FormatValueWithScale(EstTotalYearlyDividend, m?.PriceScale ?? 1, m?.ValueFormat ?? "")}";
	}

	public string EstTotalYearlyDividendYieldStr()
	{
		var yield = TotalMarketValue > 0 ? EstTotalYearlyDividend / TotalMarketValue * 100 : 0;
		return $"{yield:N1}%";
	}

	public decimal CurrentAllocationPct(HoldingTicker ticker) => TotalMarketValue > 0 ? ticker.Shares * ticker.MarketPrice / TotalMarketValue * 100 : 0;
	public decimal TargetAllocationPct(HoldingTicker ticker) => ticker.TargetAllocation;
	public decimal AllocationDiffPct(HoldingTicker ticker) => CurrentAllocationPct(ticker) - TargetAllocationPct(ticker);

	public decimal AmoutNeededToFillDeviation(HoldingTicker ticker)
	{
		var a = ticker.MarketPrice * ticker.Shares;
		var b = TotalMarketValue - a;
		var c = ticker.TargetAllocation / 100m;
		return 1 - c != 0 ? b*c / (1 - c) - a : 0;
	}

	public string AmoutNeededToFillDeviationStr(HoldingTicker ticker, MarketDefResp? market = null)
	{
		var m = market ?? Market;
		var amount = AmoutNeededToFillDeviation(ticker);
		return $"{m?.CurrencySymbol ?? ""} {FormatUtils.FormatValueWithScale(amount, m?.PriceScale ?? 1, m?.ValueFormat ?? "")}";
	}

	public decimal SharesNeededToFillDeviation(HoldingTicker ticker)
	{
		return ticker.MarketPrice > 0 ? AmoutNeededToFillDeviation(ticker) / ticker.MarketPrice : 0;
	}

	public string SharesNeededToFillDeviationStr(HoldingTicker ticker)
	{
		return SharesNeededToFillDeviation(ticker).ToString("N2");
	}

	[JsonIgnore]
	public decimal MaxAllocationDiffPct => Metadata?.HoldingTickers.Max(AllocationDiffPct) ?? 0;

	public string MaxAllocationDiffPctStr()
	{
		var diffPct = MaxAllocationDiffPct;
		return $"{(diffPct>=0?"+":"")}{diffPct:N1}%";
	}

	[JsonIgnore]
	public decimal MinAllocationDiffPct => Metadata?.HoldingTickers.Min(AllocationDiffPct) ?? 0;

	public string MinAllocationDiffPctStr()
	{
		var diffPct = MinAllocationDiffPct;
		return $"{(diffPct>=0?"+":"")}{diffPct:N1}%";
	}
}
