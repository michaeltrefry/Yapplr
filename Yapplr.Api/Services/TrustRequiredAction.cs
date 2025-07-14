namespace Yapplr.Api.Services;

/// <summary>
/// Actions that require certain trust levels
/// </summary>
public enum TrustRequiredAction
{
    CreatePost,
    CreateComment,
    LikeContent,
    ReportContent,
    SendMessage,
    FollowUsers,
    CreateMultiplePosts,
    MentionUsers
}