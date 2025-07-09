using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface IContentModerationService
{
    /// <summary>
    /// Analyze content and get moderation suggestions
    /// </summary>
    Task<ContentModerationResult> AnalyzeContentAsync(string content, bool includeSentiment = true);
    
    /// <summary>
    /// Analyze multiple pieces of content in batch
    /// </summary>
    Task<IEnumerable<ContentModerationResult>> AnalyzeContentBatchAsync(IEnumerable<string> contents, bool includeSentiment = true);
    
    /// <summary>
    /// Check if the content moderation service is available
    /// </summary>
    Task<bool> IsServiceAvailableAsync();
    
    /// <summary>
    /// Apply suggested system tags to a post
    /// </summary>
    Task<bool> ApplySuggestedTagsToPostAsync(int postId, ContentModerationResult moderationResult, int appliedByUserId);
    
    /// <summary>
    /// Apply suggested system tags to a comment
    /// </summary>
    Task<bool> ApplySuggestedTagsToCommentAsync(int commentId, ContentModerationResult moderationResult, int appliedByUserId);
}

public class ContentModerationResult
{
    public string Text { get; set; } = string.Empty;
    public SentimentResult? Sentiment { get; set; }
    public Dictionary<string, List<string>> SuggestedTags { get; set; } = new();
    public RiskAssessment RiskAssessment { get; set; } = new();
    public bool RequiresReview { get; set; }
}

public class SentimentResult
{
    public string Label { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class RiskAssessment
{
    public double Score { get; set; }
    public string Level { get; set; } = string.Empty;
}
