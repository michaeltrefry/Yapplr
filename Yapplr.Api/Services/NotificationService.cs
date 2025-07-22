using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;
using Yapplr.Api.Utils;
using Yapplr.Api.Services.Unified;
using Serilog.Context;

namespace Yapplr.Api.Services;

public class NotificationService : INotificationService
{
    private readonly YapplrDbContext _context;
    private readonly IUnifiedNotificationService _notificationService;
    private readonly ICountCacheService _countCache;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(YapplrDbContext context, IUnifiedNotificationService notificationService, ICountCacheService countCache, ILogger<NotificationService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _countCache = countCache;
        _logger = logger;
    }

    public async Task<NotificationDto?> CreateNotificationAsync(CreateNotificationDto createDto)
    {
        using var operationScope = LogContext.PushProperty("Operation", "CreateNotification");
        using var userScope = LogContext.PushProperty("UserId", createDto.UserId);
        using var typeScope = LogContext.PushProperty("NotificationType", createDto.Type);
        using var actorScope = createDto.ActorUserId.HasValue ? LogContext.PushProperty("ActorUserId", createDto.ActorUserId.Value) : null;
        using var postScope = createDto.PostId.HasValue ? LogContext.PushProperty("PostId", createDto.PostId.Value) : null;
        using var commentScope = createDto.CommentId.HasValue ? LogContext.PushProperty("CommentId", createDto.CommentId.Value) : null;

        _logger.LogInformation("Creating notification of type {NotificationType} for user {UserId}",
            createDto.Type, createDto.UserId);

        var notification = new Notification
        {
            Type = createDto.Type,
            Message = createDto.Message,
            UserId = createDto.UserId,
            ActorUserId = createDto.ActorUserId,
            PostId = createDto.PostId,
            CommentId = createDto.CommentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        using var notificationScope = LogContext.PushProperty("NotificationId", notification.Id);
        _logger.LogInformation("Notification {NotificationId} created successfully for user {UserId}",
            notification.Id, createDto.UserId);

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(createDto.UserId);

        return await GetNotificationByIdAsync(notification.Id);
    }

    public async Task CreateMentionNotificationsAsync(string? content, int mentioningUserId, int? postId = null, int? commentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        using var operationScope = LogContext.PushProperty("Operation", "CreateMentionNotifications");
        using var mentioningUserScope = LogContext.PushProperty("MentioningUserId", mentioningUserId);
        using var postScope = postId.HasValue ? LogContext.PushProperty("PostId", postId.Value) : null;
        using var commentScope = commentId.HasValue ? LogContext.PushProperty("CommentId", commentId.Value) : null;

        var mentionedUsernames = MentionParser.ExtractMentions(content);
        if (!mentionedUsernames.Any())
        {
            _logger.LogDebug("No mentions found in content for user {MentioningUserId}", mentioningUserId);
            return;
        }

        using var mentionCountScope = LogContext.PushProperty("MentionCount", mentionedUsernames.Count);
        _logger.LogInformation("Processing {MentionCount} mentions from user {MentioningUserId}",
            mentionedUsernames.Count, mentioningUserId);

        // Get users that exist and are not the mentioning user
        var mentionedUsers = await _context.Users
            .Where(u => mentionedUsernames.Contains(u.Username.ToLower()) && u.Id != mentioningUserId)
            .ToListAsync();

        var mentioningUser = await _context.Users.FindAsync(mentioningUserId);
        if (mentioningUser == null)
            return;

        foreach (var mentionedUser in mentionedUsers)
        {
            // Check if user has blocked the mentioning user
            var isBlocked = await _context.Blocks
                .AnyAsync(b => b.BlockerId == mentionedUser.Id && b.BlockedId == mentioningUserId);
            
            if (isBlocked)
                continue;

            // Create notification
            var message = postId.HasValue 
                ? $"@{mentioningUser.Username} mentioned you in a post"
                : $"@{mentioningUser.Username} mentioned you in a comment";

            var notification = new Notification
            {
                Type = NotificationType.Mention,
                Message = message,
                UserId = mentionedUser.Id,
                ActorUserId = mentioningUserId,
                PostId = postId,
                CommentId = commentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send real-time notification (Firebase with SignalR fallback)
            await _notificationService.SendMentionNotificationAsync(
                mentionedUser.Id,
                mentioningUser.Username,
                postId ?? 0,
                commentId
            );

            // Create mention record
            var mention = new Mention
            {
                Username = mentionedUser.Username,
                MentionedUserId = mentionedUser.Id,
                MentioningUserId = mentioningUserId,
                PostId = postId,
                CommentId = commentId,
                NotificationId = notification.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Mentions.Add(mention);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<NotificationListDto> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 25)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .Include(n => n.ActorUser)
            .Include(n => n.Post)
                .ThenInclude(p => p!.User)
            .Include(n => n.Comment)
                .ThenInclude(c => c!.User)
            .Include(n => n.Comment)
                .ThenInclude(c => c!.Parent)
                    .ThenInclude(p => p!.User)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var unreadCount = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();

        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var notificationDtos = new List<NotificationDto>();
        foreach (var notification in notifications)
        {
            var dto = await MapToNotificationDto(notification);
            notificationDtos.Add(dto);
        }

        return new NotificationListDto
        {
            Notifications = notificationDtos,
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            HasMore = totalCount > page * pageSize
        };
    }

    public async Task<int> GetUnreadNotificationCountAsync(int userId)
    {
        return await _countCache.GetUnreadNotificationCountAsync(userId);
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null || notification.IsRead)
            return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(userId);

        return true;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(int userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (!unreadNotifications.Any())
            return false;

        var now = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            // Also mark as seen when marking as read
            if (!notification.IsSeen)
            {
                notification.IsSeen = true;
                notification.SeenAt = now;
            }
        }

        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(userId);

        return true;
    }

    public async Task<bool> MarkNotificationAsSeenAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null || notification.IsSeen)
            return false;

        notification.IsSeen = true;
        notification.SeenAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(userId);

        return true;
    }

    public async Task<bool> MarkNotificationsAsSeenAsync(int[] notificationIds, int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => notificationIds.Contains(n.Id) && n.UserId == userId && !n.IsSeen)
            .ToListAsync();

        if (!notifications.Any())
            return false;

        foreach (var notification in notifications)
        {
            notification.IsSeen = true;
            notification.SeenAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(userId);

        return true;
    }

    public async Task<bool> MarkAllNotificationsAsSeenAsync(int userId)
    {
        var unseenNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsSeen)
            .ToListAsync();

        if (!unseenNotifications.Any())
            return false;

        foreach (var notification in unseenNotifications)
        {
            notification.IsSeen = true;
            notification.SeenAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Invalidate notification count cache
        await _countCache.InvalidateNotificationCountsAsync(userId);

        return true;
    }

    public async Task CreateLikeNotificationAsync(int likedUserId, int likingUserId, int postId)
    {
        // Don't notify if user likes their own post
        if (likedUserId == likingUserId)
            return;

        // Check if user has blocked the liking user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == likedUserId && b.BlockedId == likingUserId);

        if (isBlocked)
            return;

        var likingUser = await _context.Users.FindAsync(likingUserId);
        if (likingUser == null)
            return;

        // Get the post to determine media type for notification message
        var post = await _context.Posts
            .Include(p => p.PostMedia)
            .FirstOrDefaultAsync(p => p.Id == postId);

        var mediaTypeText = post != null ? GetMediaTypeText(post) : "post";

        var notification = new Notification
        {
            Type = NotificationType.Like,
            Message = $"@{likingUser.Username} liked your {mediaTypeText}",
            UserId = likedUserId,
            ActorUserId = likingUserId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification (Firebase with SignalR fallback)
        await _notificationService.SendLikeNotificationAsync(likedUserId, likingUser.Username, postId);
    }

    public async Task CreateCommentLikeNotificationAsync(int commentOwnerId, int likingUserId, int postId, int commentId)
    {
        // Don't notify if user likes their own comment
        if (commentOwnerId == likingUserId)
            return;

        // Check if user has blocked the liking user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == commentOwnerId && b.BlockedId == likingUserId);

        if (isBlocked)
            return;

        var likingUser = await _context.Users.FindAsync(likingUserId);
        if (likingUser == null)
            return;

        var notification = new Notification
        {
            Type = NotificationType.Like,
            Message = $"@{likingUser.Username} liked your comment",
            UserId = commentOwnerId,
            ActorUserId = likingUserId,
            PostId = postId,
            CommentId = commentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification (Firebase with SignalR fallback)
        await _notificationService.SendCommentLikeNotificationAsync(commentOwnerId, likingUser.Username, postId, commentId);
    }

    public async Task CreateReactionNotificationAsync(int reactedUserId, int reactingUserId, int postId, ReactionType reactionType)
    {
        // Don't notify if user reacts to their own post
        if (reactedUserId == reactingUserId)
            return;

        // Check if user has blocked the reacting user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == reactedUserId && b.BlockedId == reactingUserId);

        if (isBlocked)
            return;

        var reactingUser = await _context.Users.FindAsync(reactingUserId);
        if (reactingUser == null)
            return;

        // Get the post to determine media type for notification message
        var post = await _context.Posts
            .Include(p => p.PostMedia)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
            return;

        var mediaTypeText = GetMediaTypeText(post);
        var reactionEmoji = reactionType.GetEmoji();
        var notification = new Notification
        {
            Type = NotificationType.Like, // Reuse like type for now, could add new reaction type later
            Message = $"@{reactingUser.Username} reacted {reactionEmoji} to your {mediaTypeText}",
            UserId = reactedUserId,
            ActorUserId = reactingUserId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification (Firebase with SignalR fallback)
        await _notificationService.SendLikeNotificationAsync(reactedUserId, reactingUser.Username, postId);
    }

    public async Task CreateCommentReactionNotificationAsync(int commentOwnerId, int reactingUserId, int postId, int commentId, ReactionType reactionType)
    {
        // Don't notify if user reacts to their own comment
        if (commentOwnerId == reactingUserId)
            return;

        // Check if user has blocked the reacting user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == commentOwnerId && b.BlockedId == reactingUserId);

        if (isBlocked)
            return;

        var reactingUser = await _context.Users.FindAsync(reactingUserId);
        if (reactingUser == null)
            return;

        var reactionEmoji = reactionType.GetEmoji();
        var notification = new Notification
        {
            Type = NotificationType.Like, // Reuse like type for now, could add new reaction type later
            Message = $"@{reactingUser.Username} reacted {reactionEmoji} to your comment",
            UserId = commentOwnerId,
            ActorUserId = reactingUserId,
            PostId = postId,
            CommentId = commentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification (Firebase with SignalR fallback)
        await _notificationService.SendCommentLikeNotificationAsync(commentOwnerId, reactingUser.Username, postId, commentId);
    }

    public async Task CreateRepostNotificationAsync(int originalUserId, int repostingUserId, int postId)
    {
        // Don't notify if user reposts their own post
        if (originalUserId == repostingUserId)
            return;

        // Check if user has blocked the reposting user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == originalUserId && b.BlockedId == repostingUserId);
        
        if (isBlocked)
            return;

        var repostingUser = await _context.Users.FindAsync(repostingUserId);
        if (repostingUser == null)
            return;

        // Get the post to determine media type for notification message
        var post = await _context.Posts
            .Include(p => p.PostMedia)
            .FirstOrDefaultAsync(p => p.Id == postId);

        var mediaTypeText = post != null ? GetMediaTypeText(post) : "post";

        var notification = new Notification
        {
            Type = NotificationType.Repost,
            Message = $"@{repostingUser.Username} reposted your {mediaTypeText}",
            UserId = originalUserId,
            ActorUserId = repostingUserId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification (Firebase with SignalR fallback)
        await _notificationService.SendRepostNotificationAsync(originalUserId, repostingUser.Username, postId);
    }

    public async Task CreateFollowNotificationAsync(int followedUserId, int followingUserId)
    {
        // Check if user has blocked the following user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == followedUserId && b.BlockedId == followingUserId);
        
        if (isBlocked)
            return;

        var followingUser = await _context.Users.FindAsync(followingUserId);
        if (followingUser == null)
            return;

        var notification = new Notification
        {
            Type = NotificationType.Follow,
            Message = $"@{followingUser.Username} started following you",
            UserId = followedUserId,
            ActorUserId = followingUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification (Firebase with SignalR fallback)
        await _notificationService.SendFollowNotificationAsync(followedUserId, followingUser.Username);
    }

    public async Task CreateFollowRequestNotificationAsync(int requestedUserId, int requesterUserId)
    {
        // Check if user has blocked the requesting user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == requestedUserId && b.BlockedId == requesterUserId);

        if (isBlocked)
            return;

        var requesterUser = await _context.Users.FindAsync(requesterUserId);
        if (requesterUser == null)
            return;

        var notification = new Notification
        {
            Type = NotificationType.FollowRequest,
            Message = $"@{requesterUser.Username} wants to follow you",
            UserId = requestedUserId,
            ActorUserId = requesterUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification (Firebase with SignalR fallback)
        await _notificationService.SendFollowRequestNotificationAsync(requestedUserId, requesterUser.Username);
    }

    public async Task CreateCommentNotificationAsync(int postOwnerId, int commentingUserId, int postId, int commentId, string commentContent)
    {
        // Don't notify if user comments on their own post
        if (postOwnerId == commentingUserId)
            return;

        // Check if user has blocked the commenting user
        var isBlocked = await _context.Blocks
            .AnyAsync(b => b.BlockerId == postOwnerId && b.BlockedId == commentingUserId);

        if (isBlocked)
            return;

        // Check if the post owner is mentioned in the comment
        // If so, skip comment notification since they'll get a mention notification
        var mentionedUsernames = MentionParser.ExtractMentions(commentContent);
        var postOwner = await _context.Users.FindAsync(postOwnerId);
        if (postOwner != null && mentionedUsernames.Contains(postOwner.Username))
            return;

        var commentingUser = await _context.Users.FindAsync(commentingUserId);
        if (commentingUser == null)
            return;

        // Get the post to determine media type for notification message
        var post = await _context.Posts
            .Include(p => p.PostMedia)
            .FirstOrDefaultAsync(p => p.Id == postId);

        var mediaTypeText = post != null ? GetMediaTypeText(post) : "post";

        var notification = new Notification
        {
            Type = NotificationType.Comment,
            Message = $"@{commentingUser.Username} commented on your {mediaTypeText}",
            UserId = postOwnerId,
            ActorUserId = commentingUserId,
            PostId = postId,
            CommentId = commentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification (Firebase with SignalR fallback)
        await _notificationService.SendCommentNotificationAsync(postOwnerId, commentingUser.Username, postId, commentId);
    }

    public async Task DeletePostNotificationsAsync(int postId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.PostId == postId)
            .ToListAsync();

        _context.Notifications.RemoveRange(notifications);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSocialNotificationsForPostAsync(int postId)
    {
        // Delete social interaction notifications and post-specific system notifications
        // Preserve user-level system/moderation notifications (suspensions, bans, appeals, etc.)
        var notificationTypesToDelete = new[]
        {
            NotificationType.Mention,                    // 1
            NotificationType.Like,                       // 2
            NotificationType.Repost,                     // 3
            NotificationType.Follow,                     // 4 (shouldn't be post-related but included for completeness)
            NotificationType.Comment,                    // 5
            NotificationType.FollowRequest,              // 6 (shouldn't be post-related but included for completeness)
            NotificationType.VideoProcessingCompleted   // 110 (post-specific, should be deleted with the post)
        };

        var notificationsToDelete = await _context.Notifications
            .Where(n => n.PostId == postId && notificationTypesToDelete.Contains(n.Type))
            .ToListAsync();

        if (notificationsToDelete.Any())
        {
            _context.Notifications.RemoveRange(notificationsToDelete);
            await _context.SaveChangesAsync();

            // Invalidate notification count cache for affected users
            var affectedUserIds = notificationsToDelete.Select(n => n.UserId).Distinct();
            foreach (var userId in affectedUserIds)
            {
                await _countCache.InvalidateNotificationCountsAsync(userId);
            }

            _logger.LogInformation("Deleted {Count} post-related notifications for deleted post {PostId}",
                notificationsToDelete.Count, postId);
        }
    }

    public async Task DeleteCommentNotificationsAsync(int commentId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.CommentId == commentId)
            .ToListAsync();

        _context.Notifications.RemoveRange(notifications);
        await _context.SaveChangesAsync();
    }

    private async Task<NotificationDto?> GetNotificationByIdAsync(int id)
    {
        var notification = await _context.Notifications
            .Include(n => n.ActorUser)
            .Include(n => n.Post)
                .ThenInclude(p => p!.User)
            .Include(n => n.Comment)
                .ThenInclude(c => c!.User)
            .Include(n => n.Comment)
                .ThenInclude(c => c!.Parent)
                    .ThenInclude(p => p!.User)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (notification == null)
            return null;

        return await MapToNotificationDto(notification);
    }

    private async Task<NotificationDto> MapToNotificationDto(Notification notification)
    {
        string? status = notification.Status;

        // For follow request notifications, get status from the FollowRequest
        if (notification.Type == NotificationType.FollowRequest && notification.ActorUserId.HasValue)
        {
            var followRequest = await _context.FollowRequests
                .Where(fr => fr.RequesterId == notification.ActorUserId.Value &&
                            fr.RequestedId == notification.UserId)
                .OrderByDescending(fr => fr.CreatedAt)
                .FirstOrDefaultAsync();

            if (followRequest != null)
            {
                status = followRequest.Status switch
                {
                    FollowRequestStatus.Pending => null,
                    FollowRequestStatus.Approved => "approved",
                    FollowRequestStatus.Denied => "denied",
                    _ => null
                };
            }
        }

        var dto = new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Message = notification.Message,
            IsRead = notification.IsRead,
            IsSeen = notification.IsSeen,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            SeenAt = notification.SeenAt,
            Status = status,
            ActorUser = notification.ActorUser?.ToDto()
        };

        // Add post information if available
        if (notification.Post != null)
        {
            // Generate image URL from filename
            string? imageUrl = null;
            if (!string.IsNullOrEmpty(notification.Post.ImageFileName))
            {
                // Note: We don't have HttpContext here, so we'll just use the filename
                // The frontend can construct the full URL
                imageUrl = notification.Post.ImageFileName;
            }

            // Generate video URLs (without HttpContext, just use filenames)
            var videoUrl = notification.Post.ProcessedVideoFileName;
            var videoThumbnailUrl = notification.Post.VideoThumbnailFileName;

            dto.Post = new PostDto(
                notification.Post.Id,
                notification.Post.Content,
                imageUrl,
                videoUrl,
                videoThumbnailUrl,
                notification.Post.VideoFileName != null ? notification.Post.VideoProcessingStatus : null,
                notification.Post.Privacy,
                notification.Post.CreatedAt,
                notification.Post.UpdatedAt,
                notification.Post.User.ToDto(),
                null, // Group - not loaded for notifications
                0, // LikeCount - we don't need this for notifications
                0, // CommentCount - we don't need this for notifications
                0, // RepostCount - we don't need this for notifications
                new List<TagDto>(), // Tags - empty for notifications
                new List<LinkPreviewDto>(), // LinkPreviews - empty for notifications
                false, // IsLikedByCurrentUser - not relevant for notifications
                false, // IsRepostedByCurrentUser - not relevant for notifications
                notification.Post.UpdatedAt > notification.Post.CreatedAt.AddMinutes(1) // IsEdited
            );
        }

        // Add comment information if available
        if (notification.Comment != null)
        {
            dto.Comment = new CommentDto(
                notification.Comment.Id,
                notification.Comment.Content,
                notification.Comment.CreatedAt,
                notification.Comment.UpdatedAt,
                notification.Comment.User.ToDto(),
                notification.Comment.UpdatedAt > notification.Comment.CreatedAt.AddMinutes(1), // IsEdited
                0, // LikeCount - not needed for notifications
                false // IsLikedByCurrentUser - not needed for notifications
            );

            // If this is a comment mention and we don't have post info yet, get it from the comment
            if (dto.Post == null && notification.Comment.Parent != null)
            {
                string? imageUrl = null;
                if (!string.IsNullOrEmpty(notification.Comment.Parent?.ImageFileName))
                {
                    imageUrl = notification.Comment.Parent.ImageFileName;
                }

                // Generate video URLs (without HttpContext, just use filenames)
                var videoUrl = notification.Comment.Parent?.ProcessedVideoFileName;
                var videoThumbnailUrl = notification.Comment.Parent?.VideoThumbnailFileName;

                dto.Post = new PostDto(
                    notification.Comment.Parent?.Id ?? 0,
                    notification.Comment.Parent?.Content ?? "",
                    imageUrl,
                    videoUrl,
                    videoThumbnailUrl,
                    notification.Comment.Parent?.VideoFileName != null ? notification.Comment.Parent.VideoProcessingStatus : null,
                    notification.Comment.Parent?.Privacy ?? PostPrivacy.Public,
                    notification.Comment.Parent?.CreatedAt ?? DateTime.UtcNow,
                    notification.Comment.Parent?.UpdatedAt ?? DateTime.UtcNow,
                    notification.Comment.Parent?.User.ToDto() ?? new UserDto(0, "", "", "", null, "", "", "", DateTime.UtcNow, null, null, false, UserRole.User, UserStatus.Active, null, null),
                    null, // Group - not loaded for notifications
                    0, // LikeCount - we don't need this for notifications
                    0, // CommentCount - we don't need this for notifications
                    0, // RepostCount - we don't need this for notifications
                    new List<TagDto>(), // Tags - empty for notifications
                    new List<LinkPreviewDto>(), // LinkPreviews - empty for notifications
                    false, // IsLikedByCurrentUser - not relevant for notifications
                    false, // IsRepostedByCurrentUser - not relevant for notifications
                    (notification.Comment.Parent?.UpdatedAt ?? DateTime.UtcNow) > (notification.Comment.Parent?.CreatedAt ?? DateTime.UtcNow).AddMinutes(1) // IsEdited
                );
            }
        }

        // Add mention information if this is a mention notification
        if (notification.Type == NotificationType.Mention)
        {
            var mention = await _context.Mentions
                .FirstOrDefaultAsync(m => m.NotificationId == notification.Id);
            
            if (mention != null)
            {
                dto.Mention = new MentionDto
                {
                    Id = mention.Id,
                    Username = mention.Username,
                    CreatedAt = mention.CreatedAt,
                    MentionedUserId = mention.MentionedUserId,
                    MentioningUserId = mention.MentioningUserId,
                    PostId = mention.PostId,
                    CommentId = mention.CommentId
                };
            }
        }

        return dto;
    }

    // Moderation notification methods
    public async Task CreateUserSuspensionNotificationAsync(int userId, string reason, DateTime? suspendedUntil, string moderatorUsername)
    {
        var message = suspendedUntil.HasValue
            ? $"Your account has been suspended until {suspendedUntil.Value:yyyy-MM-dd} by @{moderatorUsername}. Reason: {reason}"
            : $"Your account has been suspended indefinitely by @{moderatorUsername}. Reason: {reason}";

        var notification = new Notification
        {
            Type = NotificationType.UserSuspended,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.SendSystemMessageAsync(userId, "Account Suspended", message);
    }

    public async Task CreateUserBanNotificationAsync(int userId, string reason, bool isShadowBan, string moderatorUsername)
    {
        var banType = isShadowBan ? "shadow banned" : "banned";
        var message = $"Your account has been {banType} by @{moderatorUsername}. Reason: {reason}";

        var notification = new Notification
        {
            Type = NotificationType.UserBanned,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification (only for regular bans, not shadow bans)
        if (!isShadowBan)
        {
            await _notificationService.SendSystemMessageAsync(userId, "Account Banned", message);
        }
    }

    public async Task CreateUserUnsuspensionNotificationAsync(int userId, string moderatorUsername)
    {
        var message = $"Your account suspension has been lifted by @{moderatorUsername}. Welcome back!";

        var notification = new Notification
        {
            Type = NotificationType.UserUnsuspended,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.SendSystemMessageAsync(userId, "Account Unsuspended", message);
    }

    public async Task CreateUserUnbanNotificationAsync(int userId, string moderatorUsername)
    {
        var message = $"Your account ban has been lifted by @{moderatorUsername}. Welcome back!";

        var notification = new Notification
        {
            Type = NotificationType.UserUnbanned,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.SendSystemMessageAsync(userId, "Account Unbanned", message);
    }

    public async Task CreateContentHiddenNotificationAsync(int userId, string contentType, int contentId, string reason, string moderatorUsername)
    {
        var message = $"Your {contentType} #{contentId} has been hidden by @{moderatorUsername}. Reason: {reason}";

        var notification = new Notification
        {
            Type = NotificationType.ContentHidden,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            PostId = contentType == "post" ? contentId : null,
            CommentId = contentType == "comment" ? contentId : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.SendSystemMessageAsync(userId, "Content Hidden", message);
    }

    public async Task CreateContentDeletedNotificationAsync(int userId, string contentType, int contentId, string reason, string moderatorUsername)
    {
        var message = $"Your {contentType} #{contentId} has been deleted by @{moderatorUsername}. Reason: {reason}";

        var notification = new Notification
        {
            Type = NotificationType.ContentDeleted,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.SendSystemMessageAsync(userId, "Content Deleted", message);
    }

    public async Task CreateContentRestoredNotificationAsync(int userId, string contentType, int contentId, string moderatorUsername)
    {
        var message = $"Your {contentType} #{contentId} has been restored by @{moderatorUsername}.";

        var notification = new Notification
        {
            Type = NotificationType.ContentRestored,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            PostId = contentType == "post" ? contentId : null,
            CommentId = contentType == "comment" ? contentId : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.SendSystemMessageAsync(userId, "Content Restored", message);
    }

    public async Task CreateAppealApprovedNotificationAsync(int userId, int appealId, string reviewNotes, string moderatorUsername)
    {
        var message = $"Your appeal #{appealId} has been approved by @{moderatorUsername}. {reviewNotes}";

        var notification = new Notification
        {
            Type = NotificationType.AppealApproved,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.SendSystemMessageAsync(userId, "Appeal Approved", message);
    }

    public async Task CreateAppealDeniedNotificationAsync(int userId, int appealId, string reviewNotes, string moderatorUsername)
    {
        var message = $"Your appeal #{appealId} has been denied by @{moderatorUsername}. {reviewNotes}";

        var notification = new Notification
        {
            Type = NotificationType.AppealDenied,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.SendSystemMessageAsync(userId, "Appeal Denied", message);
    }

    public async Task CreateSystemMessageNotificationAsync(int userId, string message)
    {
        var notification = new Notification
        {
            Type = NotificationType.SystemMessage,
            Message = message,
            UserId = userId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.SendSystemMessageAsync(userId, "System Message", message);
    }

    public async Task CreateVideoProcessingCompletedNotificationAsync(int userId, int postId)
    {
        var message = "Your video has finished processing and is now available on your feed.";

        var notification = new Notification
        {
            Type = NotificationType.VideoProcessingCompleted,
            Message = message,
            UserId = userId,
            PostId = postId,
            ActorUserId = null, // System notification
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification with post ID
        var notificationData = new Dictionary<string, string>
        {
            ["type"] = "VideoProcessingCompleted",
            ["postId"] = postId.ToString()
        };

        await _notificationService.SendSystemMessageAsync(userId, "Video Ready", "Your video has finished processing and is ready for viewing.", notificationData);
    }

    /// <summary>
    /// Determines the appropriate text to use in notifications based on the post's media content
    /// </summary>
    private string GetMediaTypeText(Post post)
    {
        if (post.PostMedia == null || !post.PostMedia.Any())
        {
            return "post";
        }

        // Check if post has only videos
        var hasVideo = post.PostMedia.Any(m => m.MediaType == MediaType.Video);
        var hasImage = post.PostMedia.Any(m => m.MediaType == MediaType.Image);
        var hasGif = post.PostMedia.Any(m => m.MediaType == MediaType.Gif);

        // If only videos, call it "video"
        if (hasVideo && !hasImage && !hasGif)
        {
            return "video";
        }

        // If only images (including GIFs), call it "photo"
        if ((hasImage || hasGif) && !hasVideo)
        {
            return "photo";
        }

        // If mixed media or unknown, default to "post"
        return "post";
    }
}
