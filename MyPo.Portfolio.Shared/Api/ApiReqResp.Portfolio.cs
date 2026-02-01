using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct CreateOrUpdatePortfolioReq
{
	[JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; set; }
	[JsonPropertyName("parent_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ParentId { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("desc"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Description { get; set; }

	[JsonPropertyName("currency")]
	public string Currency { get; set; }

	[JsonPropertyName("is_active")]
	public bool IsActive { get; set; }

	[JsonPropertyName("metadata"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PortfolioMetadata? Metadata { get; set; }
}

public sealed class PortfolioResp
{
	public static PortfolioResp BuildFrom(PortfolioEntity pr)
	{
		return new PortfolioResp
		{
			Id = pr.Id,
			ParentId = pr.ParentId,
			Name = pr.Name,
			Description = pr.Description,
			Currency = pr.Currency,
			OwnerUserId = pr.OwnerUserId,
			IsActive = pr.IsActive,
			Metadata = pr.Metadata,
		};
	}

	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonPropertyName("parent_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ParentId { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = default!;

	[JsonPropertyName("desc"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Description { get; set; }

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = default!;

	[JsonPropertyName("owner_uid")]
	public string OwnerUserId { get; set; } = default!;

	[JsonPropertyName("is_active")]
	public bool IsActive { get; set; }

	[JsonPropertyName("metadata"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PortfolioMetadata? Metadata { get; set; }

	[JsonIgnore]
	public SortedSet<PortfolioResp>? Children { get; set; }
}
