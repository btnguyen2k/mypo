using Finance.Net.Interfaces;
using Finance.Net.Models.Yahoo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Controller;

namespace MyPo.Portfolio.Api.Controllers;

[Authorize]
public partial class MarketsController : ApiBaseController
{
	private readonly IYahooFinanceService YFService = default!;

	public MarketsController(IYahooFinanceService yahooFinanceService)
	{
		ArgumentNullException.ThrowIfNull(yahooFinanceService, nameof(yahooFinanceService));
		this.YFService = yahooFinanceService;
	}

	private static string BuildYFSymbol(string code, MarketDef market)
	{
		return market.Code switch
		{
			"ASX" => $"{code}.AX",
			"HOSE" => $"{code}.VN",
			"HNX" => $"{code}.VN",
			"UPCOM" => $"{code}.VN",
			_ => code,
		};
	}

	/// <summary>
	/// Get stock quotes for the given comma-separated symbols.
	/// </summary>
	/// <param name="symbols">Comma-separated list of symbols. Each symbol is in the following format CODE:market-id</param>
	/// <returns></returns>
	[HttpGet(IPortfolioApiClient.API_STOCKS_GET_QUOTES)]
	public async ValueTask<ActionResult<ApiResp<IEnumerable<Quote>>>> GetStockQuotes([FromQuery] string symbols)
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
		var result = new Dictionary<string, Quote>();
		var quotes = await YFService.GetQuotesAsync([.. yfSymbolMap.Keys]) ?? [];
		foreach (var quote in quotes.Where(q => yfSymbolMap.TryGetValue(q.Symbol??string.Empty, out _)))
		{
			result[yfSymbolMap[quote.Symbol??string.Empty]] = quote;
		}
		// foreach (var quote in quotes)
		// {
		// 	if (yfSymbolMap.TryGetValue(quote.Symbol??string.Empty, out var originalCodeMarketId))
		// 	{
		// 		result[originalCodeMarketId] = quote;
		// 	}
		// }
		return ResponseOk(result);
	}
}
