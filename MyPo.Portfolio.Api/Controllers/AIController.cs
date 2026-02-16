using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Controller;
using OpenAI.Chat;


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

	private async ValueTask<SymbolAnalysisEntity?> GetOrCreateAsync(string ownerId, string marketId, string itemType, string itemCode)
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

	private async Task UpdateAnalysisRecordAsync(SymbolAnalysisEntity existingAnalysis, string aiVendor, string aiTier, string aiModel, string prompt, string completion, int totalTimeMs, int promptTokens, int thoughtTokens, int completionTokens)
	{
		existingAnalysis.Metadata ??= new SymbolAnalysisMetadata
		{
			AIVendor = aiVendor,
			AITier = aiTier,
			AIModel = aiModel,
		};
		existingAnalysis.Metadata.TotalTimeMs = totalTimeMs;
		existingAnalysis.Metadata.PromptTokens = promptTokens;
		existingAnalysis.Metadata.ThoughtTokens = thoughtTokens;
		existingAnalysis.Metadata.CompletionTokens = completionTokens;
		existingAnalysis.AnalysisPrompt = prompt;
		existingAnalysis.AnalysisResult = completion;
		existingAnalysis.AnalysisTime = DateTimeOffset.UtcNow;
		// using var scope = Services.CreateScope();
		// var portfolioRepository = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
		await PortfolioRepository.UpdateSymbolAnalysisAsync(existingAnalysis);
	}

	private async Task<ActionResult<ApiResp<SymbolAnalysisResp>>> AnalyzeSymbolWithGemini(SymbolAnalysisReq req, AIVendor aiVendor, string prompt, SymbolAnalysisEntity existingAnalysis, DateTimeOffset timestampStart)
	{
		var geminiClient = Services.GetRequiredKeyedService<Client>($"{req.AIVendor}:{req.AITier}");
		var model = string.IsNullOrEmpty(req.AIModel) ? aiVendor.TieredModels[req.AITier][0] : req.AIModel;
		var aiResponse = await geminiClient.Models.GenerateContentAsync(
			model: model,
			contents:
			[
				new() {
					Role = "user",
					Parts =
					[
						new() { Text = prompt },
					]
				},
			],
			config: new GenerateContentConfig
			{
				Temperature = 0.0f,
				MaxOutputTokens = req.MaxOutputTokens > 0 ? req.MaxOutputTokens : null,
				ThinkingConfig = new ThinkingConfig
				{
					ThinkingLevel = ThinkingLevel.HIGH,
				}
			}
		);
		var timestampEnd = DateTimeOffset.UtcNow;
		var completion = aiResponse.Candidates?[0].Content?.Parts?[0].Text ?? "No analysis result";
		var result = new SymbolAnalysisResp
		{
			TotalTimeMs = (int)(timestampEnd - timestampStart).TotalMilliseconds,
			NumTokensPrompt = aiResponse.UsageMetadata?.PromptTokenCount ?? 0,
			NumTokensThought = aiResponse.UsageMetadata?.ThoughtsTokenCount ?? 0,
			NumTokensResponse = aiResponse.UsageMetadata?.CandidatesTokenCount ?? 0,
			Response = completion,
		};
		if (aiResponse.Candidates != null && aiResponse.Candidates.Count > 0)
		{
			await UpdateAnalysisRecordAsync(existingAnalysis, req.AIVendor, req.AITier, model, prompt, completion, result.TotalTimeMs, result.NumTokensPrompt, result.NumTokensThought, result.NumTokensResponse);
			// await Task.Run(() => UpdateAnalysisRecord(existingAnalysis, req.AIVendor, req.AITier, model, prompt, completion, result.TotalTimeMs, result.NumTokensPrompt, result.NumTokensThought, result.NumTokensResponse));
		}
		return ResponseOk(result);
	}

	private async Task<ActionResult<ApiResp<SymbolAnalysisResp>>> AnalyzeSymbolWithOpenAI(SymbolAnalysisReq req, AIVendor aiVendor, string prompt, SymbolAnalysisEntity existingAnalysis, DateTimeOffset timestampStart)
	{
		var chatClientFactory = Services.GetRequiredKeyedService<OpenAIChatClientFactory>($"{req.AIVendor}:{req.AITier}");
		var model = string.IsNullOrEmpty(req.AIModel) ? aiVendor.TieredModels[req.AITier][0] : req.AIModel;
		var chatClient = chatClientFactory.Create(model);
		ChatCompletion aiResponse = await chatClient.CompleteChatAsync(
			[
				new UserChatMessage(prompt),
			]);
		var timestampEnd = DateTimeOffset.UtcNow;
		var completion = aiResponse.Content.Count > 0 ? aiResponse.Content.Last().Text : "No analysis result";
		var result = new SymbolAnalysisResp
		{
			TotalTimeMs = (int)(timestampEnd - timestampStart).TotalMilliseconds,
			NumTokensPrompt = aiResponse.Usage.InputTokenCount,
			NumTokensThought = 0,
			NumTokensResponse = aiResponse.Usage.OutputTokenCount,
			Response = completion,
		};
		if (aiResponse.FinishReason == ChatFinishReason.Stop)
		{
			await UpdateAnalysisRecordAsync(existingAnalysis, req.AIVendor, req.AITier, model, prompt, completion, result.TotalTimeMs, result.NumTokensPrompt, result.NumTokensThought, result.NumTokensResponse);
			// await Task.Run(() => UpdateAnalysisRecord(existingAnalysis, req.AIVendor, req.AITier, model, prompt, completion, result.TotalTimeMs, result.NumTokensPrompt, result.NumTokensThought, result.NumTokensResponse));
		}
		return ResponseOk(result);
	}

	private async Task<ActionResult<ApiResp<SymbolAnalysisResp>>> AnalyzeSymbolWithAzureOpenAI(SymbolAnalysisReq req, AIVendor aiVendor, string prompt, SymbolAnalysisEntity existingAnalysis, DateTimeOffset timestampStart)
	{
		return await AnalyzeSymbolWithOpenAI(req, aiVendor, prompt, existingAnalysis, timestampStart);
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
		var existingAnalysis = await GetOrCreateAsync(
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
			// if existing analysis is not too old, return it directly without calling AI again
			var result = new SymbolAnalysisResp
			{
				IsCached = true,
				TotalTimeMs = (int)(now - timestampStart).TotalMilliseconds,
				NumTokensPrompt = existingAnalysis.Metadata?.PromptTokens ?? 0,
				NumTokensThought = existingAnalysis.Metadata?.ThoughtTokens ?? 0,
				NumTokensResponse = existingAnalysis.Metadata?.CompletionTokens ?? 0,
				Response = existingAnalysis.AnalysisResult ?? "No analysis result"
			};
			return ResponseOk(result);
		}

		var prompt = $"Context and Task: I am playing a stock trading game. There is a hypothesis stock {symbolCode} with the information provided in the next section. Help me analyze it.\n\n"
			+ $"Inputs:\n{req.Inputs}\n\n"
			+ $"Expected Outputs:\n{req.ExpectedOutputs}\n\n"
			+ $"Provide Analysis for two scenarios: short-term ranking competition and long-term portfolio scoring.\n"
			+ $"Output should be in {req.OutputFormat} format with clear sections and bullet points for easy reading. Enclose response between ```markdown and ```, there must be no additonal next after the ending ```.\n"
			+ $"Highlight key insights and actionable recommendations using CSS class 'text-danger' (we all know about CSS, do not output instructions on how to use CSS/HTML/Styling)."
			;

		return req.AIVendor switch
		{
			AIVendor.VENDOR_GEMINI => await AnalyzeSymbolWithGemini(req, aiVendor, prompt, existingAnalysis, timestampStart),
			AIVendor.VENDOR_OPENAI => await AnalyzeSymbolWithOpenAI(req, aiVendor, prompt, existingAnalysis, timestampStart),
			AIVendor.VENDOR_AZURE_OPENAI => await AnalyzeSymbolWithAzureOpenAI(req, aiVendor, prompt, existingAnalysis, timestampStart),
			_ => ResponseNoData(400, $"AI vendor '{req.AIVendor}' not supported."),// should not happen
		};
	}
}
