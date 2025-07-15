using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Common;

/// <summary>
/// Common database query utilities and extensions
/// </summary>
public static class QueryUtilities
{
    /// <summary>
    /// Get posts query with all standard includes
    /// </summary>
    public static IQueryable<Post> GetPostsWithIncludes(this YapplrDbContext context)
    {
        return context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeletedByUser && !c.IsHidden))
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .Include(p => p.PostMedia)
            .Include(p => p.HiddenByUser)
            .Include(p => p.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .Include(p => p.PostSystemTags)
                .ThenInclude(pst => pst.AppliedByUser)
            .AsSplitQuery();
    }

    /// <summary>
    /// Get posts query with optimized includes for feed scenarios
    /// </summary>
    public static IQueryable<Post> GetPostsForFeed(this YapplrDbContext context)
    {
        return context.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeletedByUser && !c.IsHidden).Take(3)) // Limit comments for performance
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .Include(p => p.PostMedia)
            .AsSplitQuery();
    }

    /// <summary>
    /// Apply hybrid post visibility filters (post-level permanent hiding + real-time user checks)
    /// </summary>
    public static IQueryable<Post> ApplyVisibilityFilters(this IQueryable<Post> query,
        int? currentUserId,
        HashSet<int> blockedUserIds,
        HashSet<int> followingIds)
    {
        return query.Where(p =>
            // PERMANENT POST-LEVEL HIDING (single optimized check)
            (!p.IsHidden ||
             (p.HiddenReasonType == PostHiddenReasonType.VideoProcessing &&
              currentUserId.HasValue && p.UserId == currentUserId.Value)) && // Video processing posts visible to author

            // REAL-TIME USER STATUS CHECKS (no post updates needed when user status changes)
            p.User.Status == UserStatus.Active && // Hide posts from suspended/banned users
            (p.User.TrustScore >= 0.1f ||
             (currentUserId.HasValue && p.UserId == currentUserId.Value)) && // Hide low trust posts except from author

            // USER-SPECIFIC FILTERING
            !blockedUserIds.Contains(p.UserId) && // Filter out blocked users
            (p.Privacy == PostPrivacy.Public || // Public posts are visible to everyone
             (currentUserId.HasValue && p.UserId == currentUserId.Value) || // User's own posts are always visible
             (p.Privacy == PostPrivacy.Followers && currentUserId.HasValue && followingIds.Contains(p.UserId)))); // Followers-only posts visible if following the author
    }

    /// <summary>
    /// Apply hybrid post visibility filters for public-only content
    /// </summary>
    public static IQueryable<Post> ApplyPublicVisibilityFilters(this IQueryable<Post> query,
        HashSet<int> blockedUserIds)
    {
        return query.Where(p =>
            // PERMANENT POST-LEVEL HIDING (no video processing exception for public timeline)
            !p.IsHidden &&

            // REAL-TIME USER STATUS CHECKS
            p.User.Status == UserStatus.Active && // Hide posts from suspended/banned users
            p.User.TrustScore >= 0.1f && // Hide low trust posts from public timeline

            // PUBLIC TIMELINE SPECIFIC FILTERING
            p.Privacy == PostPrivacy.Public && // Only public posts
            !blockedUserIds.Contains(p.UserId)); // Filter out blocked users
    }



    /// <summary>
    /// Get reposts query with standard includes
    /// </summary>
    public static IQueryable<Repost> GetRepostsWithIncludes(this YapplrDbContext context)
    {
        return context.Reposts
            .Include(r => r.User)
            .Include(r => r.Post)
                .ThenInclude(p => p.User)
            .Include(r => r.Post)
                .ThenInclude(p => p.Likes)
            .Include(r => r.Post)
                .ThenInclude(p => p.Comments.Where(c => !c.IsDeletedByUser && !c.IsHidden))
            .Include(r => r.Post)
                .ThenInclude(p => p.Reposts)
            .Include(r => r.Post)
                .ThenInclude(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
            .Include(r => r.Post)
                .ThenInclude(p => p.PostLinkPreviews)
                    .ThenInclude(plp => plp.LinkPreview)
            .Include(r => r.Post)
                .ThenInclude(p => p.PostMedia)
            .AsSplitQuery();
    }

    /// <summary>
    /// Apply repost visibility filters
    /// </summary>
    public static IQueryable<Repost> ApplyRepostVisibilityFilters(this IQueryable<Repost> query,
        HashSet<int> blockedUserIds)
    {
        return query.Where(r =>
            !r.Post.IsHidden && // Filter out reposts of hidden posts (includes DeletedByUser via hybrid system)
            r.Post.User.Status == UserStatus.Active && // Hide reposts of posts from suspended/banned users
            r.Post.User.TrustScore >= 0.1f && // Hide reposts of low trust posts from public timeline
            r.Post.Privacy == PostPrivacy.Public && // Only reposts of public posts
            !blockedUserIds.Contains(r.UserId) && // Filter out reposts from blocked users
            !blockedUserIds.Contains(r.Post.UserId)); // Filter out reposts of posts from blocked users
    }



    /// <summary>
    /// Get blocked user IDs for a user efficiently
    /// </summary>
    public static async Task<HashSet<int>> GetBlockedUserIdsAsync(this YapplrDbContext context, int userId)
    {
        var blockedIds = await context.Blocks
            .Where(b => b.BlockerId == userId)
            .Select(b => b.BlockedId)
            .ToListAsync();

        return new HashSet<int>(blockedIds);
    }

    /// <summary>
    /// Get following user IDs for a user efficiently
    /// </summary>
    public static async Task<HashSet<int>> GetFollowingUserIdsAsync(this YapplrDbContext context, int userId)
    {
        var followingIds = await context.Follows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToListAsync();

        return new HashSet<int>(followingIds);
    }

    /// <summary>
    /// Apply standard ordering and pagination
    /// </summary>
    public static IQueryable<T> ApplyPaginationAndOrdering<T>(this IQueryable<T> query,
        int page,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null) where T : class
    {
        // Ensure we always have some ordering to avoid EF warning
        if (orderBy != null)
        {
            query = orderBy(query);
        }
        else if (typeof(T).GetInterfaces().Contains(typeof(IEntity)))
        {
            // If T implements IEntity, order by Id as fallback
            query = query.OrderBy(e => ((IEntity)e).Id);
        }
        else
        {
            // For other types, we need to ensure ordering exists
            // This is a fallback that should ideally not be reached
            // Log a warning that explicit ordering should be provided
            throw new InvalidOperationException($"No ordering specified for query of type {typeof(T).Name}. " +
                "Please provide an orderBy parameter or ensure the entity implements IEntity.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Filter posts for visibility (used in timeline methods)
    /// </summary>
    public static IQueryable<Post> FilterForVisibility(this IQueryable<Post> query,
        int userId,
        IEnumerable<int> blockedUserIds,
        IEnumerable<int> followingIds)
    {
        var blockedSet = blockedUserIds.ToHashSet();
        var followingSet = followingIds.ToHashSet();

        return query.Where(p =>
            // PERMANENT POST-LEVEL HIDING
            (!p.IsHidden ||
             (p.HiddenReasonType == PostHiddenReasonType.VideoProcessing && p.UserId == userId)) &&

            // REAL-TIME USER STATUS CHECKS
            p.User.Status == UserStatus.Active &&
            (p.User.TrustScore >= 0.1f || p.UserId == userId) &&

            // USER-SPECIFIC FILTERING
            !blockedSet.Contains(p.UserId) &&
            (p.Privacy == PostPrivacy.Public ||
             p.UserId == userId ||
             (p.Privacy == PostPrivacy.Followers && followingSet.Contains(p.UserId))));
    }



    /// <summary>
    /// Order posts by oldest first
    /// </summary>
    public static IOrderedQueryable<Post> OrderByOldest(this IQueryable<Post> query)
    {
        return query.OrderBy(p => p.CreatedAt);
    }

    /// <summary>
    /// Filter posts for visibility based on user and privacy settings (hybrid approach)
    /// </summary>
    public static IQueryable<Post> FilterForVisibility(
        this IQueryable<Post> query,
        int? currentUserId,
        List<int>? blockedUserIds = null,
        List<int>? followingUserIds = null,
        bool includeHidden = false)
    {
        // PERMANENT POST-LEVEL HIDING
        if (!includeHidden)
        {
            query = query.Where(p =>
                !p.IsHidden ||
                (p.HiddenReasonType == PostHiddenReasonType.VideoProcessing &&
                 currentUserId.HasValue && p.UserId == currentUserId.Value));
        }

        // REAL-TIME USER STATUS CHECKS
        query = query.Where(p =>
            p.User.Status == UserStatus.Active &&
            (p.User.TrustScore >= 0.1f ||
             (currentUserId.HasValue && p.UserId == currentUserId.Value)));

        // Filter out blocked users
        if (blockedUserIds?.Any() == true)
        {
            query = query.Where(p => !blockedUserIds.Contains(p.UserId));
        }

        // Apply privacy filtering
        if (currentUserId.HasValue)
        {
            query = query.Where(p =>
                p.Privacy == PostPrivacy.Public || // Public posts
                p.UserId == currentUserId.Value || // User's own posts
                (p.Privacy == PostPrivacy.Followers &&
                 followingUserIds != null && followingUserIds.Contains(p.UserId))); // Followers-only posts if following
        }
        else
        {
            // Non-authenticated users can only see public posts
            query = query.Where(p => p.Privacy == PostPrivacy.Public);
        }

        return query;
    }

    /// <summary>
    /// Get messages query with standard includes
    /// </summary>
    public static IQueryable<Message> GetMessagesWithIncludes(this YapplrDbContext context)
    {
        return context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Participants)
                    .ThenInclude(p => p.User);
    }

    /// <summary>
    /// Get conversations query with standard includes
    /// </summary>
    public static IQueryable<Conversation> GetConversationsWithIncludes(this YapplrDbContext context)
    {
        return context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .ThenInclude(m => m.Sender);
    }

    /// <summary>
    /// Get users query with standard includes for admin operations
    /// </summary>
    public static IQueryable<User> GetUsersWithAdminIncludes(this YapplrDbContext context)
    {
        return context.Users
            .Include(u => u.SuspendedByUser)
            .Include(u => u.Posts)
            .Include(u => u.Followers)
            .Include(u => u.Following);
    }

    /// <summary>
    /// Filter users by status and role
    /// </summary>
    public static IQueryable<User> FilterByStatusAndRole(
        this IQueryable<User> query,
        UserStatus? status = null,
        UserRole? role = null)
    {
        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        return query;
    }

    /// <summary>
    /// Apply standard ordering to posts (newest first)
    /// </summary>
    public static IQueryable<Post> OrderByNewest(this IQueryable<Post> query)
    {
        return query.OrderByDescending(p => p.CreatedAt);
    }

    /// <summary>
    /// Apply standard ordering to messages (newest first)
    /// </summary>
    public static IQueryable<Message> OrderByNewest(this IQueryable<Message> query)
    {
        return query.OrderByDescending(m => m.CreatedAt);
    }

    /// <summary>
    /// Apply standard ordering to conversations (by last message)
    /// </summary>
    public static IQueryable<Conversation> OrderByLastMessage(this IQueryable<Conversation> query)
    {
        return query.OrderByDescending(c => c.UpdatedAt);
    }

    /// <summary>
    /// Apply pagination with validation
    /// WARNING: This method should only be used on queries that already have an OrderBy clause.
    /// Use ApplyPaginationWithOrdering for queries without explicit ordering.
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Apply pagination with default ordering by Id to ensure deterministic results
    /// </summary>
    public static IQueryable<T> ApplyPaginationWithOrdering<T>(this IQueryable<T> query, int page, int pageSize)
        where T : class, IEntity
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return query
            .OrderBy(e => e.Id) // Ensure deterministic ordering
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Get total count for pagination
    /// </summary>
    public static async Task<int> GetTotalCountAsync<T>(this IQueryable<T> query)
    {
        return await query.CountAsync();
    }

    /// <summary>
    /// Check if user can view hidden content (admin, moderator, or content owner)
    /// DEPRECATED: Use the ClaimsPrincipal overload instead to avoid database queries
    /// </summary>
    [Obsolete("Use the ClaimsPrincipal overload instead to avoid database queries")]
    public static async Task<bool> CanViewHiddenContentAsync(
        this YapplrDbContext context,
        int? currentUserId,
        int contentOwnerId)
    {
        if (!currentUserId.HasValue)
            return false;

        if (currentUserId.Value == contentOwnerId)
            return true;

        var user = await context.Users.FindAsync(currentUserId.Value);
        return user != null && (user.Role == UserRole.Admin || user.Role == UserRole.Moderator);
    }

    /// <summary>
    /// Check if user can view hidden content using JWT claims (preferred method)
    /// </summary>
    public static bool CanViewHiddenContent(ClaimsPrincipal user, int contentOwnerId)
    {
        var currentUserId = user.GetUserIdOrNull();
        if (!currentUserId.HasValue)
            return false;

        if (currentUserId.Value == contentOwnerId)
            return true;

        return user.IsAdminOrModerator();
    }
}
