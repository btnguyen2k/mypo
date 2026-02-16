using System.ClientModel;
using OpenAI.Chat;

namespace MyPo.Portfolio.Api.Services;

public class OpenAIChatClientFactory
{
	private readonly string Endpoint;
	private readonly string ApiKey;

	public OpenAIChatClientFactory(string endpoint, string apiKey)
	{
		if (string.IsNullOrWhiteSpace(endpoint))
		{
			throw new ArgumentException("API endpoint must be specified.", nameof(endpoint));
		}

		if (string.IsNullOrWhiteSpace(apiKey))
		{
			throw new ArgumentException("API key must be specified.", nameof(apiKey));
		}

		Endpoint = endpoint;
		ApiKey = apiKey;
	}

	public ChatClient Create(string modelOrDeployment)
	{
		return new(
			credential: new ApiKeyCredential(ApiKey),
			model: modelOrDeployment,
			options: new OpenAI.OpenAIClientOptions()
			{
				Endpoint = new($"{Endpoint}"),
			});
	}
}
