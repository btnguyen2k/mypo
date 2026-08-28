using Finhub.Client;
using FinHub.Client.Schemas.Stocks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class FinHubController
{
    /// <summary>
    /// Get stock quotes for the given comma-separated symbols.
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_STOCKS_GET_QUOTES)]
    public async ValueTask<ActionResult<GetStockQuotesResponse>> GetStockQuotes([FromQuery] string symbols, IFinHubClient finHubClient)
    {
        var finhubResult = await finHubClient.GetStockQuotesAsync(symbols);
        if (!finhubResult.IsSuccess)
        {
            Logger?.LogError("Failed to fetch stock quotes for '{symbols}' from FinHub: {message}", symbols, finhubResult.Message);
            return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to fetch stock quotes for '{symbols}' from FinHub", finhubResult.Extra);
        }
        return ResponseOk(finhubResult.Data);

        // var yfSymbolMap = new Dictionary<string, string>();
        // var pairsCodeMarketId = symbols.ToUpper().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // foreach (var codeMarketIdPair in pairsCodeMarketId)
        // {
        //     var (code, marketId) = codeMarketIdPair.Split(':', 2) switch
        //     {
        //         var arr when arr.Length == 2 => (arr[0], arr[1]),
        //         _ => (codeMarketIdPair, string.Empty),
        //     };
        //     var market = Globals.MarketsMap.TryGetValue(marketId.ToUpper(), out var mkt) ? mkt : null;
        //     var symbol = market != null ? YFUtils.BuildYFTicker(code, market) : YFUtils.BuildYFTicker(codeMarketIdPair);
        //     yfSymbolMap[symbol] = codeMarketIdPair;
        // }

        // var finhubResult = await finHubClient.GetStockQuotesAsync(string.Join(',', yfSymbolMap.Keys));
        // if (!finhubResult.IsSuccess)
        // {
        //     return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to fetch stock quotes for '{symbols}' from FinHub", finhubResult.Extra);
        // }
        // var result = new Dictionary<string, StockQuote>();
        // var quotes = finhubResult.Data ?? new Dictionary<string, StockQuote>();
        // foreach (var quote in quotes.Where(q => yfSymbolMap.TryGetValue(q.Key, out _)))
        // {
        //     result[yfSymbolMap[quote.Key]] = quote.Value;
        // }
        // return ResponseOk(result);
    }

    /// <summary>
    /// Get stock symbol overview info for the given symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get information about, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_STOCKS_SYMBOL_OVERVIEW)]
    public async ValueTask<ActionResult<GetSymbolOverviewResponse>> GetStockSymbolOverview([FromRoute] string symbol, IFinHubClient finHubClient)
    {
        var finhubResult = await finHubClient.GetStockQuotesAsync(symbol);
        if (!finhubResult.IsSuccess)
        {
            Logger?.LogError("Failed to fetch stock symbol overview for '{symbol}' from FinHub: {message}", symbol, finhubResult.Message);
            return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to fetch stock symbol overview for '{symbol}' from FinHub", finhubResult.Extra);
        }
        return ResponseOk(finhubResult.Data);

        // var (code, marketId) = symbol.Split(':', 2) switch
        // {
        //     var arr when arr.Length == 2 => (arr[0], arr[1]),
        //     _ => (symbol, string.Empty),
        // };
        // var market = Globals.MarketsMap.TryGetValue(marketId.ToUpper(), out var mkt) ? mkt : null;
        // var yfSymbol = market != null ? YFUtils.BuildYFTicker(code, market) : YFUtils.BuildYFTicker(symbol);

        // var finhubResult = await finHubClient.GetStockSymbolOverviewAsync(yfSymbol);
        // if (finhubResult.Status != 200)
        // {
        //     return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to fetch stock symbol overview for '{symbol}' from FinHub", finhubResult.Extra);
        // }
        // return ResponseOk(finhubResult.Data);
    }

    /// <summary>
    /// Get stock symbol detailed info for the given symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get information about, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    /// <returns></returns>
    [HttpGet(IPortfolioApiClient.API_FINHUB_STOCKS_SYMBOL_INFO)]
    public async ValueTask<ActionResult<GetSymbolInfoResponse>> GetStockSymbolInfo([FromRoute] string symbol, IFinHubClient finHubClient)
    {
        var finhubResult = await finHubClient.GetStockQuotesAsync(symbol);
        if (!finhubResult.IsSuccess)
        {
            Logger?.LogError("Failed to fetch stock symbol info for '{symbol}' from FinHub: {message}", symbol, finhubResult.Message);
            return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to fetch stock symbol info for '{symbol}' from FinHub", finhubResult.Extra);
        }
        return ResponseOk(finhubResult.Data);

        // var (code, marketId) = symbol.Split(':', 2) switch
        // {
        //     var arr when arr.Length == 2 => (arr[0], arr[1]),
        //     _ => (symbol, string.Empty),
        // };
        // var market = Globals.MarketsMap.TryGetValue(marketId.ToUpper(), out var mkt) ? mkt : null;
        // var yfSymbol = market != null ? YFUtils.BuildYFTicker(code, market) : YFUtils.BuildYFTicker(symbol);

        // var finhubResult = await finHubClient.GetStockSymbolInfoAsync(yfSymbol);
        // if (finhubResult.Status != 200)
        // {
        //     return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to fetch stock symbol info for '{symbol}' from FinHub", finhubResult.Extra);
        // }
        // return ResponseOk(finhubResult.Data);
    }

    // /// <summary>
    // /// Get stock quote for the given symbol at the given date.
    // /// </summary>
    // /// <param name="symbol">The symbol to get quote for, accept YF format (e.g. XYZ.AX) or EXCHANGE:CODE format (e.g. ASX:XYZ).</param>
    // /// <param name="date">Date in the format of yyyy-MM-dd</param>
    // /// <returns></returns>
    // [HttpGet(IPortfolioApiClient.API_FINHUB_STOCKS_SYMBOL_QUOTE_AT_DATE)]
    // public async ValueTask<ActionResult<ApiResp<HistoryPoint>>> GetStockSymbolQuoteAtDate([FromRoute] string symbol, [FromRoute] string date, IFinHubClient finHubClient)
    // {
    //     var (code, marketId) = symbol.Split(':', 2) switch
    //     {
    //         var arr when arr.Length == 2 => (arr[0], arr[1]),
    //         _ => (symbol, string.Empty),
    //     };
    //     var market = Globals.MarketsMap.TryGetValue(marketId.ToUpper(), out var mkt) ? mkt : null;
    //     var yfSymbol = market != null ? YFUtils.BuildYFTicker(code, market) : YFUtils.BuildYFTicker(symbol);

    //     var finhubResult = await finHubClient.GetStockQuoteAtDateAsync(yfSymbol, date);
    //     if (finhubResult.Status != 200)
    //     {
    //         return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to fetch stock quote at date for '{symbol}' at '{date}' from FinHub", finhubResult.Extra);
    //     }
    //     return ResponseOk(finhubResult.Data);
    // }
}
