using System.Linq.Expressions;
using Yapplr.Api.Models;

namespace Yapplr.Api.Common;

/// <summary>
/// Common query filters as extension methods
/// </summary>
public static class QueryFilters
{
    /// <summary>
    /// Filter posts for a specific user's visibility
    /// Excludes group posts and comments from general feeds - group posts should only appear in group-specific queries
    /// </summary>
    public static QueryBuilder<Post> ForUser(this QueryBuilder<Post> builder, int? currentUserId,
        List<int>? blockedUserIds = null, List<int>? followingUserIds = null)
    {
        return builder.Where(p =>
            // POST TYPE FILTERING - only top-level posts in feeds, exclude comments
            p.PostType == PostType.Post &&

            // GROUP POST FILTERING - exclude group posts from general feeds
            p.GroupId == null &&

            // PERMANENT POST-LEVEL HIDING
            (!p.IsHidden ||
             (p.HiddenReasonType == PostHiddenReasonType.VideoProcessing &&
              currentUserId.HasValue && p.UserId == currentUserId.Value)) &&

            // REAL-TIME USER STATUS CHECKS
            p.User.Status == UserStatus.Active &&
            (p.User.TrustScore >= 0.1f ||
             (currentUserId.HasValue && p.UserId == currentUserId.Value)) &&

            // USER-SPECIFIC FILTERING
            (blockedUserIds == null || !blockedUserIds.Contains(p.UserId)) &&
            (p.Privacy == PostPrivacy.Public ||
             (currentUserId.HasValue && p.UserId == currentUserId.Value) ||
             (p.Privacy == PostPrivacy.Followers && currentUserId.HasValue &&
              followingUserIds != null && followingUserIds.Contains(p.UserId))));
    }

    /// <summary>
    /// Filter for active users only
    /// </summary>
    public static QueryBuilder<User> ActiveOnly(this QueryBuilder<User> builder)
    {
        return builder.Where(u => u.Status == UserStatus.Active);
    }

    /// <summary>
    /// Filter for verified users only
    /// </summary>
    public static QueryBuilder<User> VerifiedOnly(this QueryBuilder<User> builder)
    {
        return builder.Where(u => u.EmailVerified);
    }

    /// <summary>
    /// Filter messages for a conversation participant
    /// </summary>
    public static QueryBuilder<Message> ForParticipant(this QueryBuilder<Message> builder, int userId)
    {
        return builder.Where(m => 
            m.Conversation.Participants.Any(p => p.UserId == userId) &&
            !m.IsDeleted);
    }

    /// <summary>
    /// Filter for recent items (within specified days)
    /// </summary>
    public static QueryBuilder<T> Recent<T>(this QueryBuilder<T> builder, int days = 30) 
        where T : class
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        
        // Use reflection to find CreatedAt property
        var property = typeof(T).GetProperty("CreatedAt");
        if (property != null && property.PropertyType == typeof(DateTime))
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.Property(parameter, property);
            var constant = Expression.Constant(cutoffDate);
            var comparison = Expression.GreaterThanOrEqual(propertyAccess, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            
            return builder.Where(lambda);
        }
        
        return builder;
    }
}