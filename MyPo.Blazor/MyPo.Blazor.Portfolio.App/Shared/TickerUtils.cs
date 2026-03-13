using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Blazor.Portfolio.App.Shared;

public static class TickerUtils
{
	/// <summary>
	/// Fetches the latest stock quotes for a list of tickers, return raw response from API.
	/// </summary>
	/// <param name="tickers">List of tickers to fetch quotes for.</param>
	/// <param name="apiClient"></param>
	/// <param name="authToken"></param>
	/// <param name="apiBaseUrl"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static async Task<ApiResp<IDictionary<string, StockQuote>>> FetchQuotesForTickersRaw(
		IEnumerable<string> tickersList,
		IPortfolioApiClient apiClient,
		string authToken,
		string apiBaseUrl,
		CancellationToken cancellationToken = default)
	{
		var symbols = string.Join(",", tickersList);
		return await apiClient.GetStocksQuotesAsync(symbols, authToken, apiBaseUrl, cancellationToken: cancellationToken);
	}

	/// <summary>
	/// Fetches the latest stock quotes for a list of tickers, returns a map of {ticker --> quote}, silently handle any error and return empty map in case of failure.
	/// </summary>
	/// <param name="tickers">List of tickers to fetch quotes for.</param>
	/// <param name="apiClient"></param>
	/// <param name="authToken"></param>
	/// <param name="apiBaseUrl"></param>
	/// <param name="callbackPrefetch">Optional callback function to be called before each API call.</param>
	/// <param name="callbackPostfetch">Optional callback function to be called after each API call.</param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static async Task<IDictionary<string, StockQuote>> FetchQuotesForTickers(
		IEnumerable<string> tickers,
		IPortfolioApiClient apiClient,
		string authToken,
		string apiBaseUrl,
		Action<IEnumerable<string>>? callbackPrefetch = null,
		Action<ApiResp<IDictionary<string, StockQuote>>>? callbackPostfetch = null,
		CancellationToken cancellationToken = default)
	{
		var emptyDict = new Dictionary<string, StockQuote>();
		var clonedTickers = tickers.ToList();
		var quotesMap = new Dictionary<string, StockQuote>();
		while (clonedTickers.Count > 0)
		{
			var currentChunk = clonedTickers.Take(5).ToList();
			clonedTickers = [.. clonedTickers.Skip(5)];
			callbackPrefetch?.Invoke(currentChunk);
			var apiResult = await FetchQuotesForTickersRaw(currentChunk, apiClient, authToken, apiBaseUrl, cancellationToken);
			callbackPostfetch?.Invoke(apiResult);
			foreach (var quote in apiResult.Data ?? emptyDict)
			{
				quotesMap[quote.Key] = quote.Value;
			}
		}
		return quotesMap;
	}
}
