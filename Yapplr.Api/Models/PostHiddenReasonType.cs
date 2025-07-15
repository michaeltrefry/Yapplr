namespace Yapplr.Api.Models;

/// <summary>
/// Reasons why a post is permanently hidden at the post level.
/// These require manual action to change and don't automatically update when user status changes.
/// For dynamic user-based hiding (suspension, trust score), use real-time user checks instead.
/// </summary>
public enum PostHiddenReasonType
{
    /// <summary>
    /// Post is not hidden
    /// </summary>
    None = 0,
    
    /// <summary>
    /// User deleted their own post (soft delete)
    /// Permanent until user or admin restores it
    /// </summary>
    DeletedByUser = 1,
    
    /// <summary>
    /// Moderator/admin manually hid the post
    /// Permanent until moderator unhides it
    /// </summary>
    ModeratorHidden = 2,
    
    /// <summary>
    /// Temporarily hidden during video processing
    /// Automatically cleared when processing completes
    /// Exception: visible to post author during processing
    /// </summary>
    VideoProcessing = 3,
    
    /// <summary>
    /// AI content moderation flagged as high risk and auto-hidden
    /// Permanent until manual review and approval
    /// </summary>
    ContentModerationHidden = 4,
    
    /// <summary>
    /// Content flagged by spam detection system
    /// Permanent until manual review and approval
    /// </summary>
    SpamDetection = 5,
    
    /// <summary>
    /// Content contains malicious links or harmful content
    /// Permanent until manual review and approval
    /// </summary>
    MaliciousContent = 6
    
    // NOTE: User status-based hiding (UserSuspended, UserBanned, UserShadowBanned, LowTrustScore)
    // is now handled via real-time user checks in queries, not post-level flags.
    // This avoids the need for bulk post updates when user status changes.
}
