using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using MyPo.Libs.Clavis;

namespace MyPo.Shared.Identity;

public sealed class MyPoUser : IdentityUser
{
	public IEnumerable<MyPoRole>? Roles { get; set; } = default!;
	public IEnumerable<IdentityUserClaim<string>>? Claims { get; set; } = default!;

	public string? GivenName { get; set; } = default!;
	public string? FamilyName { get; set; } = default!;

	public MyPoUserMetadata? Metadata { get; set; } = default!;

	/// <summary>
	/// Touches the entity, updating the <see cref="ConcurrencyStamp"/> property.
	/// </summary>
	public void Touch() => ConcurrencyStamp = Guid.NewGuid().ToString();

	public override bool Equals(object? obj) => obj is MyPoUser other
		&& (ReferenceEquals(this, other) || Id.Equals(other.Id, Globals.StringComparison));

	public override int GetHashCode() => Id.GetHashCode(Globals.StringComparison);
}

public sealed partial class MyPoUserMetadata
{
	// =====================================================================================
	// Generic, domain-agnostic preference-group store.
	// Each group owns its public settings and its own isolated (encrypted) PrivateData.
	// =====================================================================================

	[JsonPropertyName("pref_groups")]
	public Dictionary<string, PreferenceGroupData> PreferenceGroups { get; set; } = [];

	[JsonIgnore]
	private Clavis? _clavis;

	[JsonIgnore]
	private readonly object _lock = new();

	/// <summary>
	/// Propagates the encryption key to every preference group's private data, and remembers it
	/// so groups created later also get the key.
	/// </summary>
	public void ApplyClavis(Clavis clavis)
	{
		lock (_lock)
		{
			_clavis = clavis;
			foreach (var group in PreferenceGroups.Values)
			{
				group.ApplyClavis(clavis);
			}
		}
	}

	/// <summary>
	/// Removes all secrets (every group's private data) before exposing metadata outside the
	/// trust boundary (e.g. API responses).
	/// </summary>
	public void StripSecrets()
	{
		lock (_lock)
		{
			foreach (var group in PreferenceGroups.Values)
			{
				group.PrivateData = null;
			}
		}
	}

	/// <summary>
	/// Gets the preference group with the given id, or <c>null</c> if it does not exist.
	/// </summary>
	public PreferenceGroupData? GetPreferenceGroup(string groupId)
	{
		lock (_lock)
		{
			return PreferenceGroups.TryGetValue(groupId, out var group) ? group : null;
		}
	}

	/// <summary>
	/// Gets the preference group with the given id, creating (and key-initializing) it if absent.
	/// </summary>
	public PreferenceGroupData GetOrCreatePreferenceGroup(string groupId)
	{
		lock (_lock)
		{
			if (!PreferenceGroups.TryGetValue(groupId, out var group))
			{
				group = new PreferenceGroupData();
				if (_clavis != null)
				{
					group.ApplyClavis(_clavis);
				}
				PreferenceGroups[groupId] = group;
			}
			return group;
		}
	}

	public MyPoUserMetadata Clone()
	{
		lock (_lock)
		{
			return JsonSerializer.Deserialize<MyPoUserMetadata>(JsonSerializer.Serialize(this))!;
		}
	}
}

/// <summary>
/// A single preference group's persisted state: public settings plus an isolated, encrypted
/// secret bag. The concrete shape of <see cref="Settings"/> is owned by the domain layer.
/// </summary>
public sealed class PreferenceGroupData
{
	/// <summary>
	/// Non-secret settings for this group, stored as a JSON object so the (lower) shared layer
	/// stays agnostic of the concrete settings type defined in the domain layer.
	/// </summary>
	[JsonPropertyName("settings")]
	public JsonObject Settings { get; set; } = new();

	/// <summary>
	/// This group's own encrypted secret bag (isolated from other groups).
	/// </summary>
	[JsonPropertyName("private_data"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public PrivateData? PrivateData { get; set; }

	[JsonIgnore]
	private Clavis? _clavis;

	[JsonIgnore]
	private readonly object _lock = new();

	public void ApplyClavis(Clavis clavis)
	{
		lock (_lock)
		{
			_clavis = clavis;
			PrivateData?.SetClavis(clavis);
		}
	}

	/// <summary>
	/// Deserializes the strongly-typed settings, or <c>null</c> if none have been stored.
	/// </summary>
	public T? GetSettings<T>() where T : class
		=> Settings.Count == 0 ? null : Settings.Deserialize<T>();

	/// <summary>
	/// Serializes and stores the strongly-typed settings.
	/// </summary>
	public void SetSettings<T>(T value) where T : class
		=> Settings = JsonSerializer.SerializeToNode(value)?.AsObject() ?? new();

	public string? GetSecret(string key)
	{
		lock (_lock)
		{
			return PrivateData?.Get(key);
		}
	}

	public void SetSecret(string key, string value)
	{
		lock (_lock)
		{
			GetOrCreatePrivateData().Add(key, value);
		}
	}

	private PrivateData GetOrCreatePrivateData()
	{
		if (PrivateData == null)
		{
			PrivateData = new();
			if (_clavis != null)
			{
				PrivateData.SetClavis(_clavis);
			}
		}
		return PrivateData;
	}
}
