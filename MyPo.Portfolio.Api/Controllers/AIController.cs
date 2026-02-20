using Google.GenAI;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Controller;
using MyPortfolio.Api.Utils;

namespace MyPo.Portfolio.Api.Controllers;

// [Authorize]
public partial class AIController : ApiBaseController
{
	private readonly IServiceProvider Services;
	private readonly IdentityOptions IdentityOptions;
	private readonly IPortfolioRepository PortfolioRepository;

	public AIController(IServiceProvider services, IOptions<IdentityOptions> identityOptions, IPortfolioRepository portfolioRepository)
	{
		ArgumentNullException.ThrowIfNull(services, nameof(services));
		ArgumentNullException.ThrowIfNull(identityOptions, nameof(identityOptions));
		ArgumentNullException.ThrowIfNull(portfolioRepository, nameof(portfolioRepository));

		Services = services;
		IdentityOptions = identityOptions.Value;
		PortfolioRepository = portfolioRepository;
	}

	/// <summary>
	/// Get the list of available AI vendors and their available models.
	/// </summary>
	/// <returns></returns>
	[HttpGet(IPortfolioApiClient.API_AI_VENDORS)]
	public ActionResult<ApiResp<IList<AIVendor>>> GetAIVendors()
	{
		var result = new List<AIVendor>(Globals.AIVendors);
		return ResponseOk(result);
	}

	private async ValueTask<SymbolAnalysisEntity?> GetOrCreateAnalysisRecordAsync(string ownerId, string marketId, string itemType, string itemCode)
	{
		var existingAnalysis = await PortfolioRepository.GetSymbolAnalysisAsync(ownerId, marketId, itemType, itemCode);
		existingAnalysis ??= await PortfolioRepository.CreateSymbolAnalysisAsync(new SymbolAnalysisEntity
			{
				Id = Guid.NewGuid().ToString(),
				OwnerId = ownerId,
				MarketId = marketId.ToUpper(),
				ItemType = itemType.ToUpper(),
				ItemCode = itemCode.ToUpper(),
				AnalysisTime = DateTimeOffset.MinValue,
			});
		return existingAnalysis;
	}

	private async Task UpdateAnalysisRecordAsync(SymbolAnalysisEntity existingAnalysis, string aiVendor, string aiTier, string aiModel, string prompt, SymbolAnalysisResp analysisResp)
	{
		existingAnalysis.Metadata = new()
		{
			AIVendor = aiVendor,
			AITier = aiTier,
			AIModel = aiModel,
			TotalTimeMs = analysisResp.TotalTimeMs,
			PromptTokens = analysisResp.NumTokensPrompt,
			ThoughtTokens = analysisResp.NumTokensThought,
			CompletionTokens = analysisResp.NumTokensResponse
		};
		existingAnalysis.AnalysisPrompt = prompt;
		existingAnalysis.AnalysisResult = analysisResp.Response;
		existingAnalysis.AnalysisTime = DateTimeOffset.UtcNow;
		var dbResult = await PortfolioRepository.UpdateSymbolAnalysisAsync(existingAnalysis);
		if (dbResult == null)
		{
			var logger = Services.GetService<ILogger<AIController>>();
			logger?.LogError("Failed to update analysis record for {ItemCode}", existingAnalysis.ItemCode);
		}
	}

	/// <summary>
	/// Asks AI to analyze a stock symbol based on the provided information and expected outputs.
	/// </summary>
	/// <param name="req"></param>
	/// <returns></returns>
	[HttpPost(IPortfolioApiClient.API_AI_SYMBOL_ANALYSIS)]
	public async Task<ActionResult<ApiResp<SymbolAnalysisResp>>> AnalyzeSymbol([FromBody] SymbolAnalysisReq req)
	{
		var timestampStart = DateTimeOffset.UtcNow;

		Globals.AIVendorsMap.TryGetValue(req.AIVendor.ToUpper(), out var aiVendor);
		if (aiVendor == null || aiVendor.TieredModels == null || !aiVendor.TieredModels.TryGetValue(req.AITier, out _))
		{
			return ResponseNoData(400, $"AI vendor '{req.AIVendor}' not configured properly.");
		}

		var symbolCode = (req.Symbol.Split(':').FirstOrDefault() ?? string.Empty).ToUpper().Trim();
		var marketId = (req.Symbol.Split(':').LastOrDefault() ?? string.Empty).Trim();
		Globals.MarketsMap.TryGetValue(marketId.ToUpper(), out var market);
		if (market!=null)
		{
			symbolCode = $"{market.Code}:{symbolCode}";
		}

		var currentUserId = GetUserID(IdentityOptions);
		var existingAnalysis = await GetOrCreateAnalysisRecordAsync(
			ownerId: currentUserId ?? string.Empty,
			marketId: marketId,
			itemType: AssetEntity.ASSET_TYPE_STOCK,
			itemCode: symbolCode
		);
		if (existingAnalysis == null)
		{
			return ResponseNoData(500, "Failed to create or retrieve symbol analysis record.");
		}

		var now = DateTimeOffset.Now;
		if (req.AIModel.Equals(existingAnalysis.Metadata?.AIModel, StringComparison.OrdinalIgnoreCase) && existingAnalysis.AnalysisTime != DateTimeOffset.MinValue && (now - existingAnalysis.AnalysisTime).TotalHours < 8)
		{
			// if existing analysis is not too old, return it without calling AI again
			var result = new SymbolAnalysisResp
			{
				IsError = false,
				IsCached = true,
				TotalTimeMs = (int)(now - timestampStart).TotalMilliseconds,
				NumTokensPrompt = existingAnalysis.Metadata?.PromptTokens ?? 0,
				NumTokensThought = existingAnalysis.Metadata?.ThoughtTokens ?? 0,
				NumTokensResponse = existingAnalysis.Metadata?.CompletionTokens ?? 0,
				Response = existingAnalysis.AnalysisResult ?? "<No analysis result>"
			};
			return ResponseOk(result);
		}

		var prompt = $"""
		Context and Task: I am competing in a high-performance trading game using real stocks.
		Primary objective: MAXIMIZE 3-month expected return while controlling probability of >25% drawdown.
		Date: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC.

		Analyze {symbolCode} using the provided data.

		If critical macro, peer, contract backlog, or sector multiple data is missing:
		- Retrieve latest available web data.
		- If unavailable, explicitly state assumptions.
		- Quantify sensitivity impact of those assumptions.

		You must think like a professional portfolio manager allocating real capital under uncertainty.

		All conclusions must:
		- Reference specific numerical metrics
		- Quantify magnitude and directional impact
		- Assign probability weighting
		- Provide confidence level (%)
		- Be justified using expected value math

		Avoid:
		- Generic commentary
		- Mechanical metric listing without interpretation
		- Anchoring bias
		- Unquantified statements

		Neutral stance only allowed if Expected Value ≈ 0.

		---

		# RAW INPUT DATA (DO NOT INTERPRET)

		{req.Inputs}

		---

		# Analytical Framework Rules

		1. Growth Normalization Rule:
		If earnings growth >200%, assess base-effect distortion.
		Use normalized growth assumption for PEG and forward modeling.

		2. Capital Structure Rule:
		If Net Debt / EBITDA > 2.5x → prioritize EV-based valuation.
		If < 1.5x → equity valuation acceptable.

		3. Valuation Hierarchy:
		Primary: Forward normalized P/E
		Secondary: Sector-adjusted EV/EBITDA
		Optional: 3-stage DCF-lite

		4. Peer Selection Rule:
		Select minimum 2 comparable companies in the same industry/sector:
		- Similar market cap range
		- Similar margin structure
		- Similar geographic exposure
		- Check global companies if domestic peers unavailable

		5. Statistical Edge Rule:
		A decision is actionable only if:
		Expected Value > +8% over timeframe
		OR downside asymmetry > 1.8x upside risk

		---

		# REQUIRED CALCULATIONS

		Before conclusions, compute:

		- Net debt
		- Enterprise value
		- EV/EBITDA
		- Revenue per share
		- Earnings yield
		- Implied growth from P/E (Gordon-derived)
		- PEG (normalized growth)
		- % from 52W high/low
		- MA slope bias (short vs long trend)
		- Volume deviation vs 30D avg
		- Net Debt / EBITDA

		---

		# OUTPUT STRUCTURE (STRICT)

		## 1. Executive Summary
		- Conviction Score (0-100)
		- Risk Rating (Low / Moderate / High / Extreme)
		- Buy/Sell/Hold (1-3M, 3-6M, 12M+)
		- 1-line quantified thesis
		- Primary edge: Fundamental or Technical
		- Expected 3M return (%)
		- Probability of >25% drawdown (%)

		---

		## 2. Quantitative Snapshot
		Table:
		- Raw metrics
		- Derived metrics
		- Valuation positioning vs sector
		- Capital efficiency indicators

		---

		## 3. Forward Earnings Model
		Project:
		- Revenue (base, bull, bear)
		- EPS trajectory
		- Margin expansion/compression
		- Sensitivity table (growth vs margin impact)

		---

		## 4. Fundamental Analysis
		- Revenue durability (contract-based? cyclicality?)
		- Margin sustainability (operating leverage quantified)
		- Balance sheet stress test (Net Debt / EBITDA scenarios)
		- Return profile inference
		- % Valuation mispricing vs peers (quantified)

		---

		## 5. Technical Structure
		- Trend regime (short / medium / long)
		- Breakdown or accumulation?
		- RSI regime shift
		- Price/Volume anomaly detection
		- Volatility-adjusted downside risk (Beta applied)

		---

		## 6. Strengths (Ranked by Impact)

		## 7. Weaknesses (Ranked by Severity)

		## 8. Critical Risks (Probability-Weighted)

		---

		## 9. Price Scenario Matrix

		For each timeframe (1M, 3M, 6M):

		Table:
		- Bull (price, probability, catalyst)
		- Base (price, probability, reasoning)
		- Bear (price, probability, risk driver)
		- Expected Price

		---

		## 10. Fair Value Estimation

		Use minimum TWO methods:
		1. Forward normalized P/E
		2. EV/EBITDA sector-adjusted
		(Optional 3rd: DCF-lite with 3-stage growth)

		Provide:
		- Conservative FV
		- Base FV
		- Aggressive FV
		- Margin of safety %

		---

		## 11. Optimal Trading Zones

		Define:
		- High conviction accumulation
		- Tactical rebound
		- Distribution zone
		- Invalidation level (technical + fundamental breach)

		---

		## 12. Trading Game Strategy

		{(req.OwningAmount!=null && req.OwningAveragePrice!=null ? ("Current position: Own " + req.OwningAmount + " shares at average " + req.OwningAveragePrice + ".") : "")}

		Evaluate hold vs sell using FORWARD EXPECTED VALUE (ignore anchoring bias).

		A. Aggressive leaderboard strategy
		- Position sizing %
		- Entry logic
		- Exit logic
		- Risk of ruin %

		B. Risk-adjusted portfolio strategy
		- Allocation %
		- Hedging logic (if applicable)
		- Capital efficiency comparison vs alternatives

		---

		# Conflict Resolution Rule

		If fundamentals and technicals disagree:
		- Quantify statistical edge
		- State which dominates decision
		- Justify using expected value math

		---

		# Abnormality Rule

		If abnormal event detected:
		- Identify anomaly
		- Quantify deviation from normal
		- Hypothesize cause
		- Retrieve web evidence if possible
		- Estimate price impact

		---

		Formatting Rules:
		- Output MUST be Markdown format and enclosed in ```markdown and ```
		- No text outside block
		- Use tables where helpful
		- Highlight decisive actions:
			- 🟢 Buy
			- 🔴 Sell
			- 🟡 Hold
		- No neutral stance unless mathematically unavoidable
		""";

		Console.WriteLine(prompt);

		var analysisResult = req.AIVendor switch
		{
			AIVendor.VENDOR_GEMINI => await AIHelper.AnalyzeSymbolWithGemini(Services.GetRequiredKeyedService<Client>($"{req.AIVendor}:{req.AITier}"), req, prompt),
			AIVendor.VENDOR_OPENAI or AIVendor.VENDOR_AZURE_OPENAI => await AIHelper.AnalyzeSymbolWithOpenAI(Services.GetRequiredKeyedService<OpenAIClientFactory>($"{req.AIVendor}:{req.AITier}"), req, prompt),
			_ => SymbolAnalysisResp.Error,
		};
		if (!analysisResult.IsError)
		{
			await UpdateAnalysisRecordAsync(existingAnalysis, req.AIVendor, req.AITier, req.AIModel, prompt, analysisResult);
		}
		return ResponseOk(analysisResult);
	}
}
