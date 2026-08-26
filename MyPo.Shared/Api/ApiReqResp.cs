using System.Text.Json;
using System.Text.Json.Serialization;
using MyPo.Shared.Helpers;

namespace MyPo.Shared.Api;

/// <summary>
/// Response to an API call.
/// </summary>
public class ApiResp
{
	/// <summary>
	/// Status of the API call, following HTTP status codes convention.
	/// </summary>
	[JsonPropertyName("status")]
	public int Status { get; set; }

	[JsonIgnore]
	public bool IsSuccess => Status >= 200 && Status < 300;

	/// <summary>
	/// Extra information if any (e.g. the detailed error message).
	/// </summary>
	[JsonPropertyName("message"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Message { get; set; }

	/// <summary>
	/// Extra data if any.
	/// </summary>
	[JsonPropertyName("extra"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public virtual object? Extra { get; set; }

    public virtual T? ExtraAs<T>() where T : class
    {
        return JsonHelper.SafeDeserialize<T>(JsonSerializer.Serialize(Extra, (JsonSerializerOptions?)null));
    }

	/// <summary>
	/// Debug information if any.
	/// </summary>
	[JsonPropertyName("debug_info"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public virtual object? DebugInfo { get; set; }

	public virtual T? DebugInfoAs<T>() where T : class
    {
        return JsonHelper.SafeDeserialize<T>(JsonSerializer.Serialize(DebugInfo, (JsonSerializerOptions?)null));
    }
}

/// <summary>
/// Typed version of <see cref="ApiResp"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiResp<T> : ApiResp
{
	/// <summary>
	/// The data returned by the API call (specific to individual API).
	/// </summary>
	[JsonPropertyName("data"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public virtual T? Data { get; set; }
}

/*----------------------------------------------------------------------*/

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
