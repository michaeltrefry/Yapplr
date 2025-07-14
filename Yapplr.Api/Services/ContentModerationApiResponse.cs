using System.Text.Json.Serialization;

namespace Yapplr.Api.Services;

// API Response DTOs
public class ContentModerationApiResponse
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("sentiment")]
    public SentimentApiResponse? Sentiment { get; set; }

    [JsonPropertyName("suggested_tags")]
    public Dictionary<string, List<string>>? SuggestedTags { get; set; }

    [JsonPropertyName("risk_assessment")]
    public RiskAssessmentApiResponse? RiskAssessment { get; set; }

    [JsonPropertyName("requires_review")]
    public bool RequiresReview { get; set; }
}
