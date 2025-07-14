namespace Yapplr.Api.Services;

public class ContentModerationResult
{
    public string Text { get; set; } = string.Empty;
    public SentimentResult? Sentiment { get; set; }
    public Dictionary<string, List<string>> SuggestedTags { get; set; } = new();
    public RiskAssessment RiskAssessment { get; set; } = new();
    public bool RequiresReview { get; set; }
}
