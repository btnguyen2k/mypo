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
		if (str.Length <= maxLength || maxLength < 1)
		{
			return str;
		}

		var excerptLength = maxLength;
		if (!char.IsWhiteSpace(str[maxLength]))
		{
			while (excerptLength > 0 && !char.IsWhiteSpace(str[excerptLength - 1]))
			{
				excerptLength--;
			}

			if (excerptLength == 0)
			{
				excerptLength = maxLength;
				while (excerptLength < str.Length && !char.IsWhiteSpace(str[excerptLength]))
				{
					excerptLength++;
				}

				if (excerptLength == str.Length)
				{
					return str;
				}
			}
		}

		return string.Concat(str.AsSpan(0, excerptLength).TrimEnd(), "...");
	}
}
