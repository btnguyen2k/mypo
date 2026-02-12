using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.AspNetCore.Mvc;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;
using MyPo.Shared.Api;
using MyPo.Shared.Api.Controller;


namespace MyPo.Portfolio.Api.Controllers;

// [Authorize]
public partial class AIController : ApiBaseController
{
	private readonly IServiceProvider services;

	public AIController(IServiceProvider services)
	{
		this.services = services;
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

	/// <summary>
	/// Asks AI to analyze a stock symbol based on the provided information and expected outputs.
	/// </summary>
	/// <param name="req"></param>
	/// <returns></returns>
	[HttpPost(IPortfolioApiClient.API_AI_SYMBOL_ANALYSIS)]
	public async Task<ActionResult<ApiResp<SymbolAnalysisResp>>> AnalyzeSymbol([FromBody] SymbolAnalysisReq req)
	{
		if (req.AIVendor != AIVendor.VENDOR_GEMINI || req.Tier != AIVendor.TIER_FREE)
		{
			return ResponseNoData(400, $"Unsupported AI vendor '{req.AIVendor}' or tier '{req.Tier}'. Currently only Gemini Free Tier is supported.");
		}

		Globals.AIVendorsMap.TryGetValue(req.AIVendor.ToUpper(), out var aiVendor);
		if (aiVendor == null || aiVendor.TieredModels == null || !aiVendor.TieredModels.ContainsKey(req.Tier))
		{
			return ResponseNoData(400, $"AI vendor '{req.AIVendor}' not configured properly.");
		}

		var prompt = $"Context and Task: I am playing a stock trading game. There is a hypothesis stock {req.Symbol} with the information provided in the next section. Help me analyze it.\n\n"
			+ $"Inputs:\n{req.Inputs}\n\n"
			+ $"Expected Outputs:\n{req.ExpectedOutputs}\n\n"
			+ $"Provide Analysis for two scenarios: short-term ranking competition and long-term portfolio scoring. Output should be in {req.OutputFormat} format with clear sections and bullet points for easy reading. Highlight key insights and actionable recommendations using CSS class 'text-danger'."
			;
		var geminiClient = services.GetRequiredKeyedService<Client>($"{req.AIVendor}:{req.Tier}");
		var model = string.IsNullOrEmpty(req.Model) ? aiVendor.TieredModels[req.Tier][0] : req.Model;
		var response = await geminiClient.Models.GenerateContentAsync(
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
		var result = new SymbolAnalysisResp
		{
			Response = response.Candidates?[0].Content?.Parts?[0].Text??"No response"
		};
		return ResponseOk(result);
	}
}
