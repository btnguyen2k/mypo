using System.Text.Json.Serialization;

namespace MyPo.Libs.Clavis;

public sealed class PrivateData
{
	[JsonPropertyName("data")]
	public Dictionary<string, string> Data { get; set; } = [];

	[JsonIgnore]
	private Clavis? _clavis;

	public PrivateData SetSecretKey(string secretKey)
	{
		_clavis = new Clavis(secretKey);
		return this;
	}

	public PrivateData SetClavis(Clavis clavis)
	{
		_clavis = clavis;
		return this;
	}

	public PrivateData Add(string key, string value)
	{
		if (_clavis == null) throw new InvalidOperationException("Clavis is not set. Please set it before adding data.");
		var encryptedValue = _clavis.EncryptAsBase64(value);
		Data[key] = encryptedValue;
		return this;
	}

	public string? Get(string key)
	{
		if (_clavis == null) throw new InvalidOperationException("Clavis is not set. Please set it before getting data.");
		if (!Data.TryGetValue(key, out var encryptedValue)) return null;
		return _clavis.DecryptToStringFromBase64(encryptedValue);
	}
}
