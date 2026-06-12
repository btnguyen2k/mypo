using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MyPo.Portfolio.Shared.Utils;

public static partial class FormatUtils
{
    public const string DEFAULT_DATETIME_PICKER_FORMAT = "dd-MMM-yyyy HH:mm";
    public static readonly List<string> DATETIME_PICKER_FORMATS =
    [
        DEFAULT_DATETIME_PICKER_FORMAT,
        "dd-MM-yyyy HH:mm",
        "dd-MMMM-yyyy HH:mm",
        "dd/MM/yyyy HH:mm",
        "dd/MMM/yyyy HH:mm",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd, HH:mm",
    ];

    [GeneratedRegex(@"(?<=^|-)(\d)(?=-)", RegexOptions.Compiled)]
    private static partial Regex MyRegexPaddingDayAndMonth();

    /// <summary>
    /// Parse DateTime from datetime picker string, trying multiple formats.
    /// </summary>
    /// <param name="dateTimeStr"></param>
    /// <returns>return null if parsing error</returns>
    public static DateTime? ParseDateTimeFromDateTimePicker(string dateTimeStr)
    {
        dateTimeStr = dateTimeStr.Replace("Sept", "Sep", StringComparison.OrdinalIgnoreCase); // handle Sept to Sep
        dateTimeStr = MyRegexPaddingDayAndMonth().Replace(dateTimeStr, "0$1");

        return DATETIME_PICKER_FORMATS
            .Select(format => DateTime.TryParseExact(dateTimeStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : (DateTime?)null)
            .FirstOrDefault(dt => dt.HasValue);
    }

    /// <summary>
    /// Parse DateTimeOffset from datetime picker string, trying multiple formats.
    /// </summary>
    /// <param name="dateTimeStr"></param>
    /// <returns>return null if parsing error</returns>
    public static DateTimeOffset? ParseDateTimeOffsetFromDateTimePicker(string dateTimeStr)
    {
        var dt = ParseDateTimeFromDateTimePicker(dateTimeStr);
        return dt != null ? new DateTimeOffset(dt.Value) : null;
    }

    public static string FormatValueMaxDecimals(double value, int maxDecimals = 4)
    {
        return FormatValueMaxDecimals((decimal)value, maxDecimals);
    }

    public static string FormatValueMaxDecimals(decimal value, int maxDecimals = 4)
    {
        if (maxDecimals < 0) maxDecimals = 0;
        for (var numDecimals = 0; numDecimals <= maxDecimals; numDecimals++)
        {
            if (Math.Round(value, numDecimals) == value)
            {
                maxDecimals = numDecimals;
                break;
            }
        }
        var format = "N" + maxDecimals;
        return value.ToString(format, CultureInfo.CurrentCulture);
    }

    public static string FormatValueWithScale(double value, decimal scale = 1, string? format = null)
    {
        return FormatValueWithScale((decimal)value, scale, format);
    }

    public static string FormatValueWithScale(decimal value, decimal scale = 1, string? format = null)
    {
        if (scale <= 0) scale = 1;
        return FormatRawValueWithScale(value * scale, scale, format);
    }

    public static string FormatRawValueWithScale(double value, decimal scale = 1, string? format = null)
    {
        return FormatRawValueWithScale((decimal)value, scale, format);
    }

    public static string FormatRawValueWithScale(decimal value, decimal scale = 1, string? format = null)
    {
        if (scale <= 0) scale = 1;
        if (scale >= 1000)
        {
            return value.ToString(!string.IsNullOrEmpty(format) ? format : "N1", CultureInfo.CurrentCulture);
        }
        if (scale >= 100)
        {
            return value.ToString(!string.IsNullOrEmpty(format) ? format : "N2", CultureInfo.CurrentCulture);
        }
        if (scale > 1)
        {
            return value.ToString(!string.IsNullOrEmpty(format) ? format : "N3", CultureInfo.CurrentCulture);
        }
        return value.ToString(!string.IsNullOrEmpty(format) ? format : "N4", CultureInfo.CurrentCulture);
    }

    public static string FormatVolume(int volume)
    {
        return FormatVolume((long)volume);
    }

    public static string FormatVolume(long volume)
    {
        if (volume >= 1_000_000_000_000)
        {
            return $"{volume / 1_000_000_000_000m:N2}T";
        }
        if (volume >= 1_000_000_000)
        {
            return $"{volume / 1_000_000_000m:N2}B";
        }
        if (volume >= 1_000_000)
        {
            return $"{volume / 1_000_000m:N2}M";
        }
        if (volume >= 1_000)
        {
            return $"{volume / 1_000m:N2}K";
        }
        return volume.ToString("N0", CultureInfo.CurrentCulture);
    }

    public static decimal CalculatePercentageChange(decimal oldValue, decimal newValue)
    {
        if (oldValue == 0)
        {
            return newValue == 0 ? 0 : 100;
        }
        return (newValue - oldValue) / Math.Abs(oldValue) * 100;
    }

    public static decimal CalculatePnL(AssetResp asset, MarketDefResp? market, StockQuote quote)
    {
        return CalculatePnL(asset.ToModel(), market?.ToModel(), quote);
    }

    public static decimal CalculatePnL(AssetEntity asset, MarketDef? market, StockQuote quote)
    {
        if (market == null)
        {
            return 0;
        }
        var currentPrice = quote.MarketPrice;
        var pnl = (currentPrice - asset.AveragePrice * market!.PriceScale) * asset.Quantity;
        return pnl;
    }

    public static string ExtractFirstCharIfEmoji(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var enumerator = StringInfo.GetTextElementEnumerator(input);
        if (!enumerator.MoveNext()) return string.Empty;
        var firstElement = enumerator.GetTextElement();
        return IsEmoji(firstElement) ? firstElement : string.Empty;
    }

    public static bool IsEmoji(string textElement)
    {
        var codePoint = char.ConvertToUtf32(textElement, 0);
        return codePoint == 0x200D ||         // Zero Width Joiner (used in sequences)
            codePoint == 0x20E3 ||            // Combining Enclosing Keycap
            codePoint is
                >= 0x1F600 and <= 0x1F64F or  // Emoticons
                >= 0x1F300 and <= 0x1F5FF or  // Misc Symbols & Pictographs
                >= 0x1F680 and <= 0x1F6FF or  // Transport & Map
                >= 0x1F700 and <= 0x1F77F or  // Alchemical Symbols
                >= 0x1F780 and <= 0x1F7FF or  // Geometric Shapes Extended
                >= 0x1F800 and <= 0x1F8FF or  // Supplemental Arrows-C
                >= 0x1F900 and <= 0x1F9FF or  // Supplemental Symbols & Pictographs
                >= 0x1FA00 and <= 0x1FA6F or  // Chess Symbols
                >= 0x1FA70 and <= 0x1FAFF or  // Symbols & Pictographs Extended-A
                >= 0x2600 and <= 0x26FF or    // Misc Symbols (☀, ☁, ❤, etc.)
                >= 0x2700 and <= 0x27BF or    // Dingbats
                >= 0x231A and <= 0x231B or    // Watch, Hourglass
                >= 0x23E9 and <= 0x23F3 or    // Various clock/timer symbols
                >= 0x23F8 and <= 0x23FA or    // Pause, stop, record
                >= 0x25AA and <= 0x25AB or    // Small squares
                >= 0x25B6 and <= 0x25C0 or    // Triangles
                >= 0x25FB and <= 0x25FE or    // Medium squares
                >= 0xFE00 and <= 0xFE0F       // Variation Selectors
            ;
    }

    public static string EmojiForAssetType(string assetType)
    {
        return assetType.ToUpper() switch
        {
            "ETF" => "📦",
            "MUTUAL FUND" => "🧺",
            "CRYPTO" => "🪙",
            "REIT" => "🏢",
            "LIC" => "🏛️",
            "HYBRID" => "⚖️",
            "STANDARD" => "📈",
            "GOLD" => "🥇",
            "SILVER" => "🥈",
            "OTHER COMMODITY" => "🧱",
            _ => "🗂️",
        };
    }
}
