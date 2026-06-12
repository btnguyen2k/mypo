using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Utils;

public static class SymbolUtils
{
    /// <summary>
    /// Converts a stock symbol to format EXCHANGE:SYMBOL.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="market"></param>
    /// <returns></returns>
    public static string NormalizeSymbol(string symbol, MarketDef market)
    {
        symbol = symbol.ToUpper();
        if (symbol.Contains(':', StringComparison.Ordinal))
        {
            return symbol; // already in EXCHANGE:SYMBOL format, just return as is
        }
        symbol = symbol.EndsWith(".AX") || symbol.EndsWith(".VN") ? symbol[..^3] : symbol;
        return $"{market.Code}:{symbol}".ToUpper();
    }
}
