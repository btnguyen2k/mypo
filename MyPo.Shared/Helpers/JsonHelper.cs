using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyPo.Shared.Helpers;

public static class JsonHelper
{
	public static T? SafeDeserialize<T>(string v) where T : class
	{
		try
		{
			return JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions?)null);
		}
		catch (JsonException)
		{
			return null;
		}
	}
}

public sealed class DefaultDecimalConverter : JsonConverter<decimal>
{
	public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null) return 0;
        return reader.GetDecimal();
	}
	public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value);
	}
}

public sealed class DefaultIntConverter : JsonConverter<int>
{
	public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null) return 0;
        return reader.GetInt32();
	}
	public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value);
	}
}

public sealed class DefaultLongConverter : JsonConverter<long>
{
	public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null) return 0;
        return reader.GetInt64();
	}
	public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value);
	}
}
