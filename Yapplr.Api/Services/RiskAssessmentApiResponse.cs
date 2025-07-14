using System.Text.Json.Serialization;

namespace Yapplr.Api.Services;

public class RiskAssessmentApiResponse
{
    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }
}
