using System.Text.Json.Serialization;

namespace MyPo.Libs.Clavis;

public sealed class PrivateData
{
	[JsonIgnore]
	private readonly Dictionary<string, string> _data = [];

	[JsonPropertyName("data")]
	public Dictionary<string, string> Data => new(_data);

	[JsonIgnore]
	private string _secret_key = string.Empty;

	[JsonIgnore]
	private Clavis? _clavis;

	public PrivateData SetSecretKey(string secretKey)
	{
		_secret_key = secretKey;
		_clavis = new Clavis(secretKey);
		return this;
	}

	public PrivateData SetClavis(Clavis clavis)
	{
		_secret_key = string.Empty;
		_clavis = clavis;
		return this;
	}

	public PrivateData Add(string key, string value)
	{
		if (_clavis == null) throw new InvalidOperationException("Clavis is not set. Please set it before adding data.");
		var encryptedValue = _clavis.EncryptAsBase64(value);
		_data[key] = encryptedValue;
		return this;
	}

	public string? Get(string key)
	{
		if (_clavis == null) throw new InvalidOperationException("Clavis is not set. Please set it before getting data.");
		if (!_data.TryGetValue(key, out var encryptedValue)) return null;
		return _clavis.DecryptToStringFromBase64(encryptedValue);
	}
}
