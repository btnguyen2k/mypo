using System.Text.Json.Serialization;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Portfolio.Shared.Api;

public struct ReportResp
{
    public static ReportResp BuildFrom(ReportEntity e)
    {
        var resp = new ReportResp()
        {
            Id = e.Id,
            Type = e.Type,
            PeriodStart = e.PeriodStart,
            PeriodLabel = e.PeriodLabel,
            PortfolioId = e.PortfolioId,
            ItemCode = e.ItemCode,
            TxType = e.TxType,
            IsFinal = e.IsFinal,
            Metadata = e.Metadata
        };
        return resp;
    }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public ReportType Type { get; set; }

    [JsonPropertyName("period_start")]
    public string PeriodStart { get; set; }

    [JsonPropertyName("period_label")]
    public string PeriodLabel { get; set; }

    [JsonPropertyName("portfolio_id")]
    public string PortfolioId { get; set; }

    [JsonPropertyName("item_code")]
    public string ItemCode { get; set; }

    [JsonPropertyName("tx_type")]
    public string TxType { get; set; }

    [JsonPropertyName("is_final")]
    public bool IsFinal { get; set; }

    [JsonPropertyName("metadata"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReportEntityMetadata? Metadata { get; set; }
}
