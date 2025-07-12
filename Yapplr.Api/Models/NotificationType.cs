namespace Yapplr.Api.Models;

public enum NotificationType
{
    Mention = 1,
    Like = 2,
    Repost = 3,
    Follow = 4,
    Comment = 5,
    FollowRequest = 6,

    // Moderation notifications
    UserSuspended = 100,
    UserBanned = 101,
    UserUnsuspended = 102,
    UserUnbanned = 103,
    ContentHidden = 104,
    ContentDeleted = 105,
    ContentRestored = 106,
    AppealApproved = 107,
    AppealDenied = 108,
    SystemMessage = 109,

    // Video processing notifications
    VideoProcessingCompleted = 110
}
