using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Shared.Api;

public partial interface IPortfolioApiClient
{
	public const string API_AI_VENDORS = "/api/ai/vendors";

	public const string API_AI_SYMBOL_ANALYSIS = "/api/ai/symbol_analysis";

	/// <summary>
	/// Calls the API <see cref="API_AI_VENDORS"/> to get the list of available AI vendors.
	/// </summary>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// <param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<IEnumerable<AIVendor>>> GetAIVendorsAsync(string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);

	/// <summary>
	/// Calls the API <see cref="API_AI_SYMBOL_ANALYSIS"/> to analyze a symbol with AI.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="authToken"></param>
	/// <param name="baseUrl"></param>
	/// param name="requestHttpClient"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task<ApiResp<SymbolAnalysisResp>> AnalyzeSymbolAsync(SymbolAnalysisReq req, string authToken, string? baseUrl = default, HttpClient? requestHttpClient = default, CancellationToken cancellationToken = default);
}
