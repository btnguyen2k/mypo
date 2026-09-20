using System.Text.Json.Serialization;

namespace MyPo.Shared.Api;

/// <summary>
/// Response to the <<c>/info</c>> API call.
/// </summary>
public sealed class InfoResp
{
	[JsonPropertyName("ready")]
	public bool Ready { get; set; }

	[JsonPropertyName("app")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public AppInfo? App { get; set; }

	[JsonPropertyName("server")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ServerInfo? Server { get; set; }

	[JsonPropertyName("crypto")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public CryptoInfo? Crypto { get; set; }
}

public sealed class CryptoInfo
{
	[JsonPropertyName("pub_key")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PubKey { get; set; }

	[JsonPropertyName("pub_key_type")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PubKeyType { get; set; }
}

public sealed class ServerInfo
{
	[JsonPropertyName("env")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Env { get; set; }

	[JsonPropertyName("time")]
	public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AppInfo
{
	[JsonPropertyName("name")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Name { get; set; }

	[JsonPropertyName("version")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Version { get; set; }

	[JsonPropertyName("description")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Description { get; set; }
}
