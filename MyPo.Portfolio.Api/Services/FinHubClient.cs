using Finhub.Client;
using MyPo.Shared.Api;

namespace MyPo.Portfolio.Api.Services;

public partial class FinHubClient : BaseApiClient, IFinHubClient
{
    public FinHubClient(
        HttpClient httpClient,
        string baseUrl = "",
        IDictionary<string, string>? attachedHeaders = null,
        ILogger<FinHubClient>? logger = null) : base(httpClient, baseUrl, attachedHeaders, logger)
    {
        var myAttachedHeaders = new Dictionary<string, string>();

        var apiKey = Environment.GetEnvironmentVariable("FINHUB_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            myAttachedHeaders["X-Api-Key"] = apiKey;
        }

        AddAttachedHeaders(myAttachedHeaders);
    }

    private readonly TimeSpan MIN_TIMEOUT = TimeSpan.FromSeconds(10 * 60);
    /// <inheritdoc/>
    protected override void SetupDefaultHttpClient(HttpClient defaultHttpClient)
    {
        base.SetupDefaultHttpClient(defaultHttpClient);
        defaultHttpClient.Timeout = defaultHttpClient.Timeout >= MIN_TIMEOUT ? defaultHttpClient.Timeout : MIN_TIMEOUT;
    }
}
