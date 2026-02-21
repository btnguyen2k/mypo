using System.ClientModel;
using Microsoft.VisualBasic;
using MyPo.Portfolio.Shared.Models.FinHub;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

namespace MyPo.Portfolio.Api.Services;

public class OpenAIClientFactory
{
	private readonly OpenAIClient Client;
	private readonly AICapacity? Capacity;

	public OpenAIClientFactory(string endpoint, string apiKey, AICapacity? capacity = null)
	{
		if (string.IsNullOrWhiteSpace(endpoint))
		{
			throw new ArgumentException("API endpoint must be specified.", nameof(endpoint));
		}

		if (string.IsNullOrWhiteSpace(apiKey))
		{
			throw new ArgumentException("API key must be specified.", nameof(apiKey));
		}

		this.Capacity = capacity;
		this.Client = new(
			credential: new ApiKeyCredential(apiKey),
			options: new OpenAIClientOptions()
			{
				Endpoint = new($"{endpoint}"),
			});
	}

	public string GetApiTypeForModel(string modelOrDeployment)
	{
		return Capacity?.GetApiTypeForModel(modelOrDeployment) ?? AICapacity.API_TYPE_CHAT;
	}

	public ChatClient CreateChatClient(string modelOrDeployment)
	{
		return Client.GetChatClient(modelOrDeployment);
	}

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
	public ResponsesClient CreateResponseClient(string modelOrDeployment)
	{
		return Client.GetResponsesClient(model: modelOrDeployment);
	}
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
	public IList<ResponseTool> BuildReponseToolChain(string modelOrDeployment)
	{
		var toolChain = new List<ResponseTool>();
		var toolsList = Capacity?.GetToolChainForModel(modelOrDeployment) ?? [];
		foreach (var tool in toolsList)
		{
			if (tool.TryGetValue("type", out var toolType))
			{
				switch (toolType)
				{
					case "web_search":
					case "web-search":
						toolChain.Add(ResponseTool.CreateWebSearchTool());
						break;
					case "web-search-preview":
					case "web_search_preview":
						toolChain.Add(ResponseTool.CreateWebSearchPreviewTool());
						break;
					default:
						break;
				}
			}
		}
		return toolChain;
	}
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
