using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Controller;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class MarketsController : ApiBaseController
{
	private readonly IServiceProvider services;

	public MarketsController(IServiceProvider services)
	{
		this.services = services;
	}

	private static string BuildYFSymbol(string code, MarketDef market)
	{
		return market.Code switch
		{
			"*AUSYD" or "ASX" => $"{code}.AX",
			"*VNVN" or "HOSE" or "HNX" or "UPCOM" => $"{code}.VN",
			_ => code,
		};
	}

	/// <summary>
	/// Get stock quotes for the given comma-separated symbols.
	/// </summary>
	/// <param name="symbols">Comma-separated list of symbols. Each symbol is in the following format CODE:market-id</param>
	/// <returns></returns>
	[HttpGet(IPortfolioApiClient.API_STOCKS_GET_QUOTES)]
	public async ValueTask<ActionResult<ApiResp<IDictionary<string, StockQuote>>>> GetStockQuotes([FromQuery] string symbols)
	{
		var yfSymbolMap = new Dictionary<string, string>();
		var pairsCodeMarketId = symbols.ToUpper().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (var codeMarketIdPair in pairsCodeMarketId)
		{
			var (code, marketId) = codeMarketIdPair.Split(':', 2) switch
			{
				var arr when arr.Length == 2 => (arr[0], arr[1]),
				_ => (codeMarketIdPair, string.Empty),
			};
			var market = Globals.MarketsMap.TryGetValue(marketId.ToUpper(), out var mkt) ? mkt : null;
			if (market != null)
			{
				var symbol = BuildYFSymbol(code, market);
				yfSymbolMap[symbol] = codeMarketIdPair;
			}
		}

		using (var scope = services.CreateScope())
		{
			var finhubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
			var finhubResult = await finhubClient.GetStockQuotesAsync(string.Join(',', yfSymbolMap.Keys));
			if (finhubResult.Status != 200)
			{
				return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to fetch stock quotes for '{symbols}' from FinHub", finhubResult.Extras);
			}
			var result = new Dictionary<string, StockQuote>();
			var quotes = finhubResult.Data ?? new Dictionary<string, StockQuote>();
			foreach (var quote in quotes.Where(q => yfSymbolMap.TryGetValue(q.Key, out _)))
			{
				result[yfSymbolMap[quote.Key]] = quote.Value;
			}
			return ResponseOk(result);
		}
	}

	/// <summary>
	/// Get stock symbol info for the given symbol.
	/// </summary>
	/// <param name="symbol">Symbol in the following format CODE:market-id</param>
	/// <returns></returns>
	[HttpGet(IPortfolioApiClient.API_STOCKS_SYMBOL_INFO)]
	public async ValueTask<ActionResult<ApiResp<SymbolInfo>>> GetStockSymbolInfo([FromRoute] string symbol)
	{
		var (code, marketId) = symbol.Split(':', 2) switch
		{
			var arr when arr.Length == 2 => (arr[0], arr[1]),
			_ => (symbol, string.Empty),
		};
		var market = Globals.MarketsMap.TryGetValue(marketId.ToUpper(), out var mkt) ? mkt : null;
		var yfSymbol = market != null ? BuildYFSymbol(code, market) : code;

		using (var scope = services.CreateScope())
		{
			var finhubClient = scope.ServiceProvider.GetRequiredService<IFinHubClient>();
			var finhubResult = await finhubClient.GetStockSymbolInfoAsync(yfSymbol);
			if (finhubResult.Status != 200)
			{
				return ResponseNoData(finhubResult.Status, finhubResult.Message ?? $"Failed to fetch stock symbol info for '{symbol}' from FinHub", finhubResult.Extras);
			}
			return ResponseOk(finhubResult.Data);
		}
	}
}
