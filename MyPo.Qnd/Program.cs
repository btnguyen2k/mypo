using MyPo.Shared.Api;
using MyPo.Portfolio.Shared.Models.FinHub;
using System.Text.Json;

var jsonstr = """
{
  "status": 200,
  "message": "ok",
  "data": [
    {
      "symbol": "ASX:BSR",
      "exchange": "ASX",
      "company_name": "Bison Resources Limited",
      "timestamp": 1776261600,
      "date": "2026-04-16 00:00:00+10:00",
      "event_category": "listing",
      "source_name": "ASX",
      "link": "https://www.asx.com.au/listings/upcoming-floats-and-listings",
      "sector": "BASIC MATERIALS",
      "principal_activities": "Exploration and mining",
      "price": 0.2,
      "currency": "AUD",
      "capital": 5500000,
      "analysis": {
        "status": "upcoming",
        "data_quality": "high",
        "search_findings": "Prospectus lodged 20 Feb 2026 to raise ~$5.5m at $0.20/share for Nevada-focused gold exploration across four Carlin Trend projects, with funds primarily allocated to drilling and working capital. Management includes executives with prior ASX junior exploration experience, and no revenue or resources are yet defined. ([bisonresources.com.au](https://www.bisonresources.com.au/prospectus))",
        "stance": "Neutral",
        "catalyst": "ASX listing on 16 April 2026 and initial drill program commencement",
        "risks": [
          "Exploration risk with no defined resources",
          "Limited cash runway post-IPO",
          "Gold price volatility"
        ],
        "outlook": {
          "w2": {
            "direction": "→",
            "reason": "Typical post-IPO price discovery for micro-cap explorer with no drilling results yet.",
            "confidence": 45
          },
          "m1": {
            "direction": "↑",
            "reason": "Speculative interest possible ahead of first exploration updates (inferred).",
            "confidence": 40
          },
          "m3": {
            "direction": "→",
            "reason": "Sustained performance dependent on drill results and funding runway.",
            "confidence": 35
          }
        }
      }
    }
  ]
}
""";

var obj = JsonSerializer.Deserialize<ApiResp<IEnumerable<ListingEvent>>>(jsonstr);
var e = obj!.Data!.First();
Console.WriteLine(JsonSerializer.Serialize(e));
