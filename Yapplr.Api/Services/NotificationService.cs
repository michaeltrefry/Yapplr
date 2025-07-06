using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;
using Yapplr.Api.Utils;

namespace Yapplr.Api.Services;

public class NotificationService : INotificationService
{
    private readonly YapplrDbContext _context;
    private readonly ICompositeNotificationService _notificationService;

    public NotificationService(YapplrDbContext context, ICompositeNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<NotificationDto?> CreateNotificationAsync(CreateNotificationDto createDto)
    {
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

        return await GetNotificationByIdAsync(notification.Id);
    }

    public async Task CreateMentionNotificationsAsync(string content, int mentioningUserId, int? postId = null, int? commentId = null)
    {
        var mentionedUsernames = MentionParser.ExtractMentions(content);
        if (!mentionedUsernames.Any())
            return;

        // Get users that exist and are not the mentioning user
        var mentionedUsers = await _context.Users
            .Where(u => mentionedUsernames.Contains(u.Username) && u.Id != mentioningUserId)
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
                .ThenInclude(c => c!.Post)
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
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
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
        return true;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(int userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (!unreadNotifications.Any())
            return false;

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
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

        var notification = new Notification
        {
            Type = NotificationType.Like,
            Message = $"@{likingUser.Username} liked your post",
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

        var notification = new Notification
        {
            Type = NotificationType.Repost,
            Message = $"@{repostingUser.Username} reposted your post",
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

        var notification = new Notification
        {
            Type = NotificationType.Comment,
            Message = $"@{commentingUser.Username} commented on your post",
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
                .ThenInclude(c => c!.Post)
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
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
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

            dto.Post = new PostDto(
                notification.Post.Id,
                notification.Post.Content,
                imageUrl,
                notification.Post.Privacy,
                notification.Post.CreatedAt,
                notification.Post.UpdatedAt,
                notification.Post.User.ToDto(),
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
                notification.Comment.UpdatedAt > notification.Comment.CreatedAt.AddMinutes(1) // IsEdited
            );

            // If this is a comment mention and we don't have post info yet, get it from the comment
            if (dto.Post == null && notification.Comment.Post != null)
            {
                string? imageUrl = null;
                if (!string.IsNullOrEmpty(notification.Comment.Post.ImageFileName))
                {
                    imageUrl = notification.Comment.Post.ImageFileName;
                }

                dto.Post = new PostDto(
                    notification.Comment.Post.Id,
                    notification.Comment.Post.Content,
                    imageUrl,
                    notification.Comment.Post.Privacy,
                    notification.Comment.Post.CreatedAt,
                    notification.Comment.Post.UpdatedAt,
                    notification.Comment.Post.User.ToDto(),
                    0, // LikeCount - we don't need this for notifications
                    0, // CommentCount - we don't need this for notifications
                    0, // RepostCount - we don't need this for notifications
                    new List<TagDto>(), // Tags - empty for notifications
                    new List<LinkPreviewDto>(), // LinkPreviews - empty for notifications
                    false, // IsLikedByCurrentUser - not relevant for notifications
                    false, // IsRepostedByCurrentUser - not relevant for notifications
                    notification.Comment.Post.UpdatedAt > notification.Comment.Post.CreatedAt.AddMinutes(1) // IsEdited
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
}
