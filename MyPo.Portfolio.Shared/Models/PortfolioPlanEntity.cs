using System.Text.Json.Serialization;
using MyPo.Shared.Models;

namespace MyPo.Portfolio.Shared.Models;

public sealed class PortfolioPlanEntity : Entity<string>
{
	/// <inheritdoc />
	public override string Id { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Id of the portfolio owner, which is the user id.
	/// </summary>
	public string OwnerUserId { get; set; } = default!;

	/// <summary>
	/// Id of the associated portfolio, if any.
	/// </summary>
	public string? PortfolioId { get; set; }

	/// <summary>
	/// Plan's friendly name.
	/// </summary>
	public string Name { get; set; } = default!;

	public PortfolioPlanMetadata? Metadata { get; set; }

	public override string ToString() => Name ?? string.Empty;
}

public sealed class PortfolioPlanMetadata
{
	[JsonPropertyName("refresh_timestamp")]
	public long MetadataRefreshTimestamp { get; set; }

	[JsonIgnore]
	public DateTime MetadataRefreshUTC => DateTimeOffset.FromUnixTimeSeconds(MetadataRefreshTimestamp).UtcDateTime;

	[JsonPropertyName("holdings")]
	public IList<HoldingTicker> HoldingTickers { get; set; } = [];
}

public sealed class HoldingTicker
{
	[JsonPropertyName("id")]
	public string Id = Guid.NewGuid().ToString();

	[JsonPropertyName("ticker")]
	public string Ticker { get; set; } = string.Empty;

	[JsonPropertyName("allocation")]
	public decimal TargetAllocation { get; set; } = 0;

	[JsonPropertyName("tags")]
	public string Tags { get; set; } = string.Empty;

	[JsonPropertyName("shares")]
	public decimal Shares { get; set; }

	[JsonPropertyName("market_price")]
	public decimal MarketPrice { get; set; }

	[JsonPropertyName("div_yield")]
	public decimal DividendYield { get; set; }

	[JsonPropertyName("payout_frequency")]
	public int PayoutFrequency { get; set; }

	[JsonIgnore]
	public decimal EstYearlyDividend => MarketPrice * DividendYield/100m * Shares;
}
