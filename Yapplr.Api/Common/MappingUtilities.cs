using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Common;

/// <summary>
/// Centralized mapping utilities for converting between models and DTOs
/// </summary>
public static class MappingUtilities
{
    /// <summary>
    /// Generate image URL from filename
    /// </summary>
    public static string? GenerateImageUrl(string? fileName, HttpContext? httpContext)
    {
        if (string.IsNullOrEmpty(fileName) || httpContext?.Request == null)
            return null;

        var request = httpContext.Request;
        return $"{request.Scheme}://{request.Host}/api/images/{fileName}";
    }

    /// <summary>
    /// Check if entity is edited (updated more than 1 minute after creation)
    /// </summary>
    public static bool IsEdited(DateTime createdAt, DateTime updatedAt)
    {
        return updatedAt > createdAt.AddMinutes(1);
    }

    /// <summary>
    /// Map User to UserDto
    /// </summary>
    public static UserDto MapToUserDto(this User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.Username,
            user.Bio,
            user.Birthday,
            user.Pronouns,
            user.Tagline,
            user.ProfileImageFileName,
            user.CreatedAt,
            user.FcmToken,
            user.ExpoPushToken,
            user.EmailVerified,
            user.Role,
            user.Status,
            user.SuspendedUntil,
            user.SuspensionReason
        );
    }

    /// <summary>
    /// Map User to UserProfileDto with additional data
    /// </summary>
    public static UserProfileDto MapToUserProfileDto(
        this User user, 
        int postCount, 
        int followerCount, 
        int followingCount,
        bool isFollowedByCurrentUser = false,
        bool hasPendingFollowRequest = false,
        bool requiresFollowApproval = false)
    {
        return new UserProfileDto(
            user.Id,
            user.Username,
            user.Bio,
            user.Birthday,
            user.Pronouns,
            user.Tagline,
            user.ProfileImageFileName,
            user.CreatedAt,
            postCount,
            followerCount,
            followingCount,
            isFollowedByCurrentUser,
            hasPendingFollowRequest,
            requiresFollowApproval
        );
    }

    /// <summary>
    /// Map Tag to TagDto
    /// </summary>
    public static TagDto MapToTagDto(this Tag tag)
    {
        return new TagDto(
            tag.Id,
            tag.Name,
            tag.PostCount
        );
    }

    /// <summary>
    /// Map Comment to CommentDto
    /// </summary>
    public static CommentDto MapToCommentDto(this Comment comment, HttpContext? httpContext = null)
    {
        var userDto = comment.User.MapToUserDto();
        var isEdited = IsEdited(comment.CreatedAt, comment.UpdatedAt);

        return new CommentDto(
            comment.Id,
            comment.Content,
            comment.CreatedAt,
            comment.UpdatedAt,
            userDto,
            isEdited
        );
    }

    /// <summary>
    /// Map LinkPreview to LinkPreviewDto
    /// </summary>
    public static LinkPreviewDto MapToLinkPreviewDto(this LinkPreview linkPreview)
    {
        return new LinkPreviewDto(
            linkPreview.Id,
            linkPreview.Url,
            linkPreview.Title,
            linkPreview.Description,
            linkPreview.ImageUrl,
            linkPreview.SiteName,
            linkPreview.Status,
            linkPreview.ErrorMessage,
            linkPreview.CreatedAt
        );
    }

    /// <summary>
    /// Create system user DTO for fallback scenarios
    /// </summary>
    public static UserDto CreateSystemUserDto()
    {
        return new UserDto(
            0,
            "system@yapplr.com",
            "Yapplr Moderation",
            "Official moderation team",
            null,
            "",
            "Keeping Yapplr safe and friendly",
            "",
            DateTime.UtcNow,
            null,
            null,
            true,
            UserRole.System,
            UserStatus.Active,
            null,
            null
        );
    }

    /// <summary>
    /// Map FollowRequest to FollowRequestDto
    /// </summary>
    public static FollowRequestDto MapToFollowRequestDto(this FollowRequest followRequest)
    {
        return new FollowRequestDto
        {
            Id = followRequest.Id,
            CreatedAt = followRequest.CreatedAt,
            Requester = followRequest.Requester.MapToUserDto(),
            Requested = followRequest.Requested.MapToUserDto()
        };
    }

    /// <summary>
    /// Create FollowResponseDto
    /// </summary>
    public static FollowResponseDto CreateFollowResponseDto(bool isFollowing, int followerCount, bool hasPendingRequest = false)
    {
        return new FollowResponseDto(isFollowing, followerCount, hasPendingRequest);
    }

    /// <summary>
    /// Create AuthResponseDto
    /// </summary>
    public static AuthResponseDto CreateAuthResponseDto(string token, User user)
    {
        return new AuthResponseDto(token, user.MapToUserDto());
    }

    /// <summary>
    /// Map Post to PostDto with all related data
    /// </summary>
    public static PostDto MapToPostDto(
        this Post post,
        int? currentUserId,
        HttpContext? httpContext = null,
        bool includeModeration = false)
    {
        var userDto = post.User.MapToUserDto();
        var isLiked = currentUserId.HasValue && post.Likes.Any(l => l.UserId == currentUserId.Value);
        var isReposted = currentUserId.HasValue && post.Reposts.Any(r => r.UserId == currentUserId.Value);
        var imageUrl = GenerateImageUrl(post.ImageFileName, httpContext);
        var isEdited = IsEdited(post.CreatedAt, post.UpdatedAt);

        // Map tags
        var tags = post.PostTags.Select(pt => pt.Tag.MapToTagDto()).ToList();

        // Map link previews
        var linkPreviews = post.PostLinkPreviews.Select(plp => plp.LinkPreview.MapToLinkPreviewDto()).ToList();

        // Map moderation info if requested and available
        PostModerationInfoDto? moderationInfo = null;
        if (includeModeration && post.IsHidden)
        {
            var systemTags = post.PostSystemTags.Select(pst => new PostSystemTagDto(
                pst.SystemTag.Id,
                pst.SystemTag.Name,
                pst.SystemTag.Description,
                pst.SystemTag.Category.ToString(),
                pst.SystemTag.IsVisibleToUsers,
                pst.SystemTag.Color,
                pst.SystemTag.Icon,
                pst.Reason,
                pst.AppliedAt,
                pst.AppliedByUser?.MapToUserDto() ?? CreateSystemUserDto()
            )).ToList();

            PostAppealInfoDto? appealInfo = null;
            // Add appeal info mapping if needed

            moderationInfo = new PostModerationInfoDto(
                post.IsHidden,
                post.HiddenReason,
                post.HiddenAt,
                post.HiddenByUser?.MapToUserDto(),
                systemTags,
                null, // Risk score
                null, // Risk level
                appealInfo
            );
        }

        return new PostDto(
            post.Id,
            post.Content,
            imageUrl,
            post.Privacy,
            post.CreatedAt,
            post.UpdatedAt,
            userDto,
            post.Likes.Count,
            post.Comments.Count,
            post.Reposts.Count,
            tags,
            linkPreviews,
            isLiked,
            isReposted,
            isEdited,
            moderationInfo
        );
    }

    /// <summary>
    /// Map Message to MessageDto
    /// </summary>
    public static MessageDto MapToMessageDto(
        this Message message,
        int currentUserId,
        HttpContext? httpContext = null,
        MessageStatusType? status = null)
    {
        var imageUrl = GenerateImageUrl(message.ImageFileName, httpContext);
        var senderDto = message.Sender?.MapToUserDto() ?? CreateSystemUserDto();

        return new MessageDto(
            message.Id,
            message.Content,
            imageUrl,
            message.CreatedAt,
            message.UpdatedAt,
            message.IsEdited,
            message.IsDeleted,
            message.ConversationId,
            senderDto,
            status
        );
    }

    /// <summary>
    /// Map Conversation to ConversationDto
    /// </summary>
    public static ConversationDto MapToConversationDto(
        this Conversation conversation,
        int currentUserId,
        HttpContext? httpContext = null,
        int unreadCount = 0)
    {
        var participants = conversation.Participants.Select(p => p.User.MapToUserDto()).ToList();
        var lastMessage = conversation.Messages.FirstOrDefault();
        var lastMessageDto = lastMessage?.MapToMessageDto(currentUserId, httpContext);

        return new ConversationDto(
            conversation.Id,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            participants,
            lastMessageDto,
            unreadCount
        );
    }
}
