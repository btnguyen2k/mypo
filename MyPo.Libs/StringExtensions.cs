namespace MyPo.Libs;
public static class StringExtensions
{
	public static string SingleLine(this string str)
	{
		return str.Replace("\r\n", " ", StringComparison.Ordinal)
			.Replace("\r", " ", StringComparison.Ordinal)
			.Replace("\n", " ", StringComparison.Ordinal);
	}
}
