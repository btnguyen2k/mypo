using FinHub.Client.Schemas.Stocks;

namespace Finhub.Client;

public partial interface IFinHubClient
{
    public const string API_FINHUB_ENDPOINT_TOZ_GOLD_QUOTE = "/toz/gold/quote";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_TOZ_GOLD_QUOTE"/> to get the latest gold price quote.
    /// </summary>
    /// <param name="currency">The currency to get the gold price in (e.g., USD, EUR, etc), optional, default is USD.</param>
    /// <param name="baseUrl"></param>
    /// <param name="httpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GetStockQuoteResponse> GetGoldQuoteAsync(string? currency = "USD", string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_ENDPOINT_TOZ_GOLD_HISTORY = "/toz/gold/history";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_TOZ_GOLD_HISTORY"/> to get historical gold price data for the past number of days.
    /// </summary>
    /// <param name="currency">The currency to get the gold price in (e.g., USD, EUR, etc), optional, default is USD.</param>
    /// <param name="days">The number of past days to get historical data for, optional, default is 30.</param>
    /// <param name="baseUrl"></param>
    /// param name="httpClient"></param>
    /// param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GetStockHistoryResponse> GetGoldPriceHistoryAsync(string? currency = "USD", int? days = 30, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_ENDPOINT_TOZ_SILVER_QUOTE = "/toz/silver/quote";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_TOZ_SILVER_QUOTE"/> to get the latest silver price quote.
    /// </summary>
    /// <param name="currency">The currency to get the silver price in (e.g., USD, EUR, etc), optional, default is USD.</param>
    /// <param name="baseUrl"></param>
    /// <param name="httpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GetStockQuoteResponse> GetSilverQuoteAsync(string? currency = "USD", string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);

    public const string API_FINHUB_ENDPOINT_TOZ_SILVER_HISTORY = "/toz/silver/history";

    /// <summary>
    /// Calls the API <see cref="API_FINHUB_ENDPOINT_TOZ_SILVER_HISTORY"/> to get historical silver price data for the past number of days.
    /// </summary>
    /// <param name="currency">The currency to get the silver price in (e.g., USD, EUR, etc), optional, default is USD.</param>
    /// <param name="days">The number of past days to get historical data for, optional, default is 30.</param>
    /// <param name="baseUrl"></param>
    /// param name="httpClient"></param>
    /// param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GetStockHistoryResponse> GetSilverPriceHistoryAsync(string? currency = "USD", int? days = 30, string? baseUrl = default, HttpClient? httpClient = default, CancellationToken cancellationToken = default);
}
