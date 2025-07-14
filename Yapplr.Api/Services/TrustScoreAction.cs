namespace Yapplr.Api.Services;

/// <summary>
/// Actions that can affect trust scores
/// </summary>
public enum TrustScoreAction
{
    // Positive actions
    PostCreated,
    CommentCreated,
    LikeGiven,
    QualityContentCreated,
    HelpfulReport,
    EmailVerified,
    ProfileCompleted,
    ConsistentActivity,
    
    // Negative actions
    ContentReported,
    ContentHidden,
    ContentDeleted,
    SpamDetected,
    UserSuspended,
    UserBanned,
    FalseReport,
    ExcessiveReporting,
    
    // Neutral/Administrative
    AdminAdjustment,
    AppealApproved,
    AppealDenied,
    InactivityDecay
}