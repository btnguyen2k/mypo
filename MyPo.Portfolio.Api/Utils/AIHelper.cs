using Google.GenAI;
using Google.GenAI.Types;
using MyPo.Portfolio.Api;
using MyPo.Portfolio.Api.Services;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;
using OpenAI.Chat;
using OpenAI.Responses;

namespace MyPortfolio.Api.Utils;

public class AIHelper
{
	public const string NO_ANALYSIS_RESULT = "<No analysis result>";

	public static async Task<SymbolAnalysisResp> AnalyzeSymbolWithGemini(Client geminiClient, SymbolAnalysisReq req, string prompt)
	{
		var timestampStart = DateTimeOffset.UtcNow;

		var aiVendor = Globals.AIVendorsMap[req.AIVendor.ToUpper()];
		var model = string.IsNullOrEmpty(req.AIModel) ? aiVendor.TieredModels[req.AITier][0] : req.AIModel;
		var aiResponse = await geminiClient.Models.GenerateContentAsync(
			model: model,
			contents:
			[
				new() {
					Role = "user",
					Parts = [ new() { Text = prompt } ]
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

		var completion = aiResponse.Candidates?[0].Content?.Parts?[0].Text ?? null;
		return new SymbolAnalysisResp
		{
			TotalTimeMs = (int)(timestampEnd - timestampStart).TotalMilliseconds,
			NumTokensPrompt = aiResponse.UsageMetadata?.PromptTokenCount ?? 0,
			NumTokensThought = aiResponse.UsageMetadata?.ThoughtsTokenCount ?? 0,
			NumTokensResponse = aiResponse.UsageMetadata?.CandidatesTokenCount ?? 0,
			Response = !string.IsNullOrEmpty(completion) ? completion : NO_ANALYSIS_RESULT,
			IsError = aiResponse.PromptFeedback?.BlockReason != null || string.IsNullOrEmpty(completion),
		};
	}

	private static async Task<SymbolAnalysisResp> AnalyzeSymbolWithOpenAIChat(OpenAIClientFactory openaiClientFactory, SymbolAnalysisReq req, string model, string prompt)
	{
		var timestampStart = DateTimeOffset.UtcNow;
		var chatClient = openaiClientFactory.CreateChatClient(model);
		ChatCompletion aiResponse = await chatClient.CompleteChatAsync([new UserChatMessage(prompt)], new ChatCompletionOptions
		{
			Temperature = 0.0f,
			MaxOutputTokenCount = req.MaxOutputTokens > 0 ? req.MaxOutputTokens : null,
		});
		var timestampEnd = DateTimeOffset.UtcNow;
		var completion = aiResponse.Content.Count > 0 ? aiResponse.Content.Last().Text : null;
		return new SymbolAnalysisResp
		{
			TotalTimeMs = (int)(timestampEnd - timestampStart).TotalMilliseconds,
			NumTokensPrompt = aiResponse.Usage.InputTokenCount,
			NumTokensThought = 0,
			NumTokensResponse = aiResponse.Usage.OutputTokenCount,
			Response = !string.IsNullOrEmpty(completion) ? completion : NO_ANALYSIS_RESULT,
			IsError = aiResponse.FinishReason != ChatFinishReason.Stop || string.IsNullOrEmpty(completion),
		};
	}

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
	private static async Task<SymbolAnalysisResp> AnalyzeSymbolWithOpenAIResponse(OpenAIClientFactory openaiClientFactory, SymbolAnalysisReq req, string model, string prompt)
	{
		var timestampStart = DateTimeOffset.UtcNow;
		var responseClient = openaiClientFactory.CreateResponseClient(model);
		var options = new CreateResponseOptions(inputItems: [ ResponseItem.CreateUserMessageItem(prompt) ])
		{
			Temperature = !model.StartsWith("gpt-5") ? 0.0f : null,
			ReasoningOptions = new ResponseReasoningOptions()
			{
				ReasoningEffortLevel = model.StartsWith("gpt-5") ? ResponseReasoningEffortLevel.Medium : null,
			},
			MaxOutputTokenCount = req.MaxOutputTokens > 0 ? req.MaxOutputTokens : null,
		};
		foreach (var tool in openaiClientFactory.BuildReponseToolChain(model))
		{
			options.Tools.Add(tool);
		}
		var aiResponse = await responseClient.CreateResponseAsync(options: options);
		var timestampEnd = DateTimeOffset.UtcNow;
		var completion = aiResponse?.Value.GetOutputText() ?? null;
		Console.WriteLine($"[DEBUG] OpenAI Response: {aiResponse?.Value.Status} / IsError: {(aiResponse?.Value.Status ?? ResponseStatus.Failed) != ResponseStatus.Completed || string.IsNullOrEmpty(completion)}\n{completion}");
		return new SymbolAnalysisResp
		{
			TotalTimeMs = (int)(timestampEnd - timestampStart).TotalMilliseconds,
			NumTokensPrompt = aiResponse?.Value.Usage.InputTokenCount ?? 0,
			NumTokensThought = 0,
			NumTokensResponse = aiResponse?.Value.Usage.OutputTokenCount ?? 0,
			Response = !string.IsNullOrEmpty(completion) ? completion : NO_ANALYSIS_RESULT,
			IsError = (aiResponse?.Value.Status ?? ResponseStatus.Failed) != ResponseStatus.Completed || string.IsNullOrEmpty(completion),
		};
	}
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

	public static async Task<SymbolAnalysisResp> AnalyzeSymbolWithOpenAI(OpenAIClientFactory openaiClientFactory, SymbolAnalysisReq req, string prompt)
	{
		var aiVendor = Globals.AIVendorsMap[req.AIVendor.ToUpper()];
		var model = string.IsNullOrEmpty(req.AIModel) ? aiVendor.TieredModels[req.AITier][0] : req.AIModel;
		var apiType = openaiClientFactory.GetApiTypeForModel(model);
		return apiType switch
		{
			AICapacity.API_TYPE_CHAT => await AnalyzeSymbolWithOpenAIChat(openaiClientFactory, req, model, prompt),
			AICapacity.API_TYPE_RESPONSE => await AnalyzeSymbolWithOpenAIResponse(openaiClientFactory, req, model, prompt),
			_ => SymbolAnalysisResp.Error,
		};
	}
}
