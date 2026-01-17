using Finance.Net.Interfaces;
using Finance.Net.Models.Yahoo;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Models;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Controller;

namespace MyPo.Portfolio.Api.Controllers;

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
	[HttpGet("/stock/quote/{symbols}")]
	public async Task<ActionResult<ApiResp<IEnumerable<Quote>>>> GetStockQuotes([FromRoute] string symbols)
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
			var market = Globals.Markets.FirstOrDefault(m => string.Equals(m.Id, marketId, StringComparison.OrdinalIgnoreCase));
			if (market != null)
			{
				var symbol = BuildYFSymbol(code, market);
				yfSymbolMap[symbol] = codeMarketIdPair;
			}
		}
		var result = new Dictionary<string, Quote>();
		var quotes = await YFService.GetQuotesAsync([.. yfSymbolMap.Keys]) ?? [];
		foreach (var quote in quotes)
		{
			if (yfSymbolMap.TryGetValue(quote.Symbol??string.Empty, out var originalCodeMarketId))
			{
				result[originalCodeMarketId] = quote;
			}
		}
		return ResponseOk(result);
	}
}
