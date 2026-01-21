using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct CreateOrUpdatePortfolioRecReq
{
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
}

public sealed class PortfolioRecResp
{
	public static PortfolioRecResp BuildFrom(PortfolioRec pr)
	{
		return new PortfolioRecResp
		{
			Id = pr.Id,
			ParentId = pr.ParentId,
			Name = pr.Name,
			Description = pr.Description,
			Currency = pr.Currency,
			OwnerUserId = pr.OwnerUserId,
			IsActive = pr.IsActive,
			CreatedAt = pr.CreatedAt,
			UpdatedAt = pr.UpdatedAt,
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

	[JsonPropertyName("created_at")]
	public DateTimeOffset CreatedAt { get; set; }

	[JsonPropertyName("updated_at")]
	public DateTimeOffset UpdatedAt { get; set; }

	[JsonIgnore]
	public SortedSet<PortfolioRecResp>? Children { get; set; }
}
