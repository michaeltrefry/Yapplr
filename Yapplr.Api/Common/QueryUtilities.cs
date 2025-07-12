using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

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
            .Include(p => p.HiddenByUser)
            .Include(p => p.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .Include(p => p.PostSystemTags)
                .ThenInclude(pst => pst.AppliedByUser)
            .AsSplitQuery();
    }

    /// <summary>
    /// Filter posts for visibility based on user and privacy settings
    /// </summary>
    public static IQueryable<Post> FilterForVisibility(
        this IQueryable<Post> query, 
        int? currentUserId, 
        List<int>? blockedUserIds = null,
        List<int>? followingUserIds = null,
        bool includeHidden = false)
    {
        // Filter out user-deleted posts
        query = query.Where(p => !p.IsDeletedByUser);

        // Filter out hidden posts unless user is authorized to see them
        if (!includeHidden)
        {
            query = query.Where(p => !p.IsHidden);
        }

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
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        return query.Skip((page - 1) * pageSize).Take(pageSize);
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
    /// </summary>
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
}
