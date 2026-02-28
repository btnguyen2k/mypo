using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct MarketEventResp
{
	public static MarketEventResp BuildFrom(MarketEventEntity entity)
	{
		return new MarketEventResp
		{
			Id = entity.Id,
			OwnerId = entity.OwnerId,
			MarketId = entity.MarketId,
			ItemCode = entity.ItemCode,
			EventType = entity.EventType,
			EventTime = entity.EventTime,
			Metadata = entity.Metadata
		};
	}

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("owner_id")]
	public string OwnerId { get; set; }

	[JsonPropertyName("market_id")]
	public string MarketId { get; set; }

	[JsonPropertyName("item_code")]
	public string ItemCode { get; set; }

	[JsonPropertyName("event_type")]
	public string EventType { get; set; }

	[JsonPropertyName("event_time")]
	public DateTimeOffset EventTime { get; set; }

	[JsonIgnore]
	public readonly DateTimeOffset EventTimeLocalTz => EventTime.ToLocalTime();

	[JsonPropertyName("metadata"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public MarketEventMetadata? Metadata { get; set; }
}
