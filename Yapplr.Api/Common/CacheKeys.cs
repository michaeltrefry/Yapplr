namespace Yapplr.Api.Common;

/// <summary>
/// Cache key constants for consistent cache management
/// </summary>
public static class CacheKeys
{
    public const string USER_PREFIX = "user";
    public const string POST_PREFIX = "post";
    public const string TIMELINE_PREFIX = "timeline";
    public const string CONVERSATION_PREFIX = "conversation";
    public const string NOTIFICATION_PREFIX = "notification";
    public const string FOLLOW_PREFIX = "follow";
    public const string BLOCK_PREFIX = "block";

    public static string UserById(int userId) => $"{USER_PREFIX}:id:{userId}";
    public static string UserByUsername(string username) => $"{USER_PREFIX}:username:{username}";
    public static string PostById(int postId) => $"{POST_PREFIX}:id:{postId}";
    public static string UserTimeline(int userId, int page, int pageSize) => 
        $"{TIMELINE_PREFIX}:user:{userId}:page:{page}:size:{pageSize}";
    public static string PublicTimeline(int page, int pageSize) => 
        $"{TIMELINE_PREFIX}:public:page:{page}:size:{pageSize}";
    public static string UserFollowing(int userId) => $"{FOLLOW_PREFIX}:following:{userId}";
    public static string UserFollowers(int userId) => $"{FOLLOW_PREFIX}:followers:{userId}";
    public static string UserBlocked(int userId) => $"{BLOCK_PREFIX}:blocked:{userId}";
    public static string ConversationList(int userId, int page, int pageSize) => 
        $"{CONVERSATION_PREFIX}:list:{userId}:page:{page}:size:{pageSize}";
    public static string UnreadCount(int userId) => $"{NOTIFICATION_PREFIX}:unread:{userId}";
}