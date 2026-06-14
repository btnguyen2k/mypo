namespace MyPo.Libs;
public static class StringExtensions
{
	public static string SingleLine(this string str)
	{
		return str.Replace("\r\n", " ", StringComparison.Ordinal)
			.Replace("\r", " ", StringComparison.Ordinal)
			.Replace("\n", " ", StringComparison.Ordinal);
	}

	public static string Excerpt(this string str, int maxLength)
	{
		if (str.Length <= maxLength || maxLength < 1) return str;
		return string.Concat(str.AsSpan(0, maxLength), "...");
	}
}
