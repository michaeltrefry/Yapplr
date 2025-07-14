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
