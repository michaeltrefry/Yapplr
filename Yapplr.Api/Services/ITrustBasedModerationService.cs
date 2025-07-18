namespace Yapplr.Api.Services;

/// <summary>
/// Service that uses trust scores to influence various platform behaviors
/// </summary>
public interface ITrustBasedModerationService
{
    /// <summary>
    /// Get rate limit multiplier based on user's trust score
    /// </summary>
    Task<float> GetRateLimitMultiplierAsync(int userId);

    /// <summary>
    /// Determine if content should be auto-hidden based on author's trust score
    /// </summary>
    Task<bool> ShouldAutoHideContentAsync(int authorId, string? contentType);

    /// <summary>
    /// Get moderation priority score for content based on author's trust score
    /// </summary>
    Task<int> GetModerationPriorityAsync(int authorId, string contentType);

    /// <summary>
    /// Check if user should be allowed to perform an action based on trust score
    /// </summary>
    Task<bool> CanPerformActionAsync(int userId, TrustRequiredAction action);

    /// <summary>
    /// Get content visibility level based on author's trust score
    /// </summary>
    Task<ContentVisibilityLevel> GetContentVisibilityLevelAsync(int authorId);

    /// <summary>
    /// Get recommended review threshold for user reports based on reporter's trust score
    /// </summary>
    Task<float> GetReportReviewThresholdAsync(int reporterId);
}