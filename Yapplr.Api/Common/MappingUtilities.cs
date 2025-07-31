using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;
using Yapplr.Shared.Models;

namespace Yapplr.Api.Common;

/// <summary>
/// Centralized mapping utilities for converting between models and DTOs
/// </summary>
public static class MappingUtilities
{
    private static readonly string? BaseUrl;
    static MappingUtilities()
    {
        BaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
        Console.WriteLine($"🔧 MappingUtilities: API_BASE_URL environment variable = '{BaseUrl}'");
        if (string.IsNullOrEmpty(BaseUrl))
        {
            Console.WriteLine("⚠️ MappingUtilities: API_BASE_URL environment variable is not set! Video and image URLs will be null.");
        }
    }
    /// <summary>
    /// Generate image URL from filename
    /// </summary>
    public static string? GenerateImageUrl(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || BaseUrl == null)
            return null;

        return $"{BaseUrl}/api/images/{fileName}";
    }

    public static string? GetFileNameFromUrl(string? url)
    {
        if (url == null) return null;
        var parts = url.Split('/');
        return parts.Length < 2 ? null : parts[^1];
    }
    /// <summary>
    /// Generate video URL from filename
    /// </summary>
    public static string? GenerateVideoUrl(string? fileName)
    {
        Console.WriteLine($"🎥 GenerateVideoUrl called with fileName: '{fileName}', BaseUrl: '{BaseUrl}'");

        if (string.IsNullOrEmpty(fileName))
        {
            Console.WriteLine("🎥 GenerateVideoUrl: fileName is null or empty, returning null");
            return null;
        }

        if (BaseUrl == null)
        {
            Console.WriteLine("🎥 GenerateVideoUrl: BaseUrl is null, returning null");
            return null;
        }

        var url = $"{BaseUrl}/api/videos/processed/{fileName}";
        Console.WriteLine($"🎥 GenerateVideoUrl: Generated URL: '{url}'");
        return url;
    }

    /// <summary>
    /// Generate video thumbnail URL from filename
    /// </summary>
    public static string? GenerateVideoThumbnailUrl(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName) || BaseUrl == null)
            return null;

        return $"{BaseUrl}/api/videos/thumbnails/{fileName}";
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
            GenerateImageUrl(user.ProfileImageFileName),
            user.CreatedAt,
            user.FcmToken,
            user.ExpoPushToken,
            user.EmailVerified,
            user.Role,
            user.Status,
            user.SuspendedUntil,
            user.SuspensionReason,
            user.SubscriptionTier?.ToDto()
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
            GenerateImageUrl(user.ProfileImageFileName),
            user.CreatedAt,
            postCount,
            followerCount,
            followingCount,
            isFollowedByCurrentUser,
            hasPendingFollowRequest,
            requiresFollowApproval,
            user.SubscriptionTier?.ToDto()
        );
    }

    public static AdminUserDetailsDto MapToAdminUserDetailsDto(this User user)
    {
        return new AdminUserDetailsDto
            {
                // Basic Profile Information
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                Birthday = user.Birthday,
                Pronouns = user.Pronouns,
                Tagline = user.Tagline,
                ProfileImageUrl = GenerateImageUrl(user.ProfileImageFileName),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastSeenAt = user.LastSeenAt,
                EmailVerified = user.EmailVerified,
                TermsAcceptedAt = user.TermsAcceptedAt,

                // Admin/Moderation Information
                Role = user.Role,
                Status = user.Status,
                SuspendedUntil = user.SuspendedUntil,
                SuspensionReason = user.SuspensionReason,
                SuspendedByUsername = user.SuspendedByUser?.Username,
                LastLoginAt = user.LastLoginAt,
                LastLoginIp = user.LastLoginIp,

                // Trust Score Information
                TrustScore = user.TrustScore ?? 1.0f,
                // TrustScoreFactors = trustScoreFactors,
                // RecentTrustScoreHistory = trustScoreHistory.ToList(),

                // Rate Limiting Settings
                RateLimitingEnabled = user.RateLimitingEnabled,
                TrustBasedRateLimitingEnabled = user.TrustBasedRateLimitingEnabled,
                //IsCurrentlyRateLimited = isCurrentlyRateLimited,
                RateLimitedUntil = null, // Will be set below if blocked
                //RecentRateLimitViolations = recentViolations.Count,

                // Activity Statistics
                PostCount = user.Posts.Count,
                CommentCount = user.Posts.Count(p => p.PostType == PostType.Comment),
                LikeCount = user.Likes.Count,
                FollowerCount = user.Followers.Count,
                FollowingCount = user.Following.Count,
                //ReportCount = reportCount,
                //ModerationActionCount = moderationActionCount,

                // Recent Moderation Actions
                //RecentModerationActions = recentModerationActions
            };
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
    /// Map Post (with PostType.Comment) to CommentDto (without like information)
    /// </summary>
    public static CommentDto MapToCommentDto(this Post comment)
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
    /// Map Post (with PostType.Comment) to CommentDto with like information
    /// </summary>
    public static CommentDto MapToCommentDto(this Post comment, int? currentUserId, int likeCount, bool isLikedByCurrentUser)
    {
        var userDto = comment.User.MapToUserDto();
        var isEdited = IsEdited(comment.CreatedAt, comment.UpdatedAt);

        return new CommentDto(
            comment.Id,
            comment.Content,
            comment.CreatedAt,
            comment.UpdatedAt,
            userDto,
            isEdited,
            likeCount,
            isLikedByCurrentUser
        );
    }

    /// <summary>
    /// Map Post (with PostType.Comment) to CommentDto with reaction information
    /// </summary>
    public static CommentDto MapToCommentDtoWithReactions(this Post comment, int? currentUserId)
    {
        var userDto = comment.User.MapToUserDto();
        var isEdited = IsEdited(comment.CreatedAt, comment.UpdatedAt);

        // Get reaction data
        var reactionCounts = comment.Reactions
            .GroupBy(r => r.ReactionType)
            .Select(g => new ReactionCountDto(
                g.Key,
                g.Key.GetEmoji(),
                g.Key.GetDisplayName(),
                g.Count()
            ))
            .ToList();

        var currentUserReaction = currentUserId.HasValue
            ? comment.Reactions.FirstOrDefault(r => r.UserId == currentUserId.Value)?.ReactionType
            : null;

        var totalReactionCount = comment.Reactions.Count;

        return new CommentDto(
            comment.Id,
            comment.Content,
            comment.CreatedAt,
            comment.UpdatedAt,
            userDto,
            isEdited,
            comment.Likes.Count, // Legacy like count - now using unified Likes
            currentUserId.HasValue && comment.Likes.Any(l => l.UserId == currentUserId.Value), // Legacy is liked
            reactionCounts,
            currentUserReaction,
            totalReactionCount
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
            linkPreview.YouTubeVideoId,
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
            null,
            null // No subscription tier for system user
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
        bool includeModeration = false)
    {
        var userDto = post.User.MapToUserDto();
        var isLiked = currentUserId.HasValue && post.Likes.Any(l => l.UserId == currentUserId.Value);
        var isReposted = currentUserId.HasValue && post.Reposts.Any(r => r.UserId == currentUserId.Value);

        // Generate legacy imageUrl - use Post.ImageFileName if available, otherwise use first image from PostMedia
        var imageUrl = GenerateImageUrl(post.ImageFileName);
        if (string.IsNullOrEmpty(imageUrl) && post.PostMedia.Any(pm => pm.MediaType == MediaType.Image && !string.IsNullOrEmpty(pm.ImageFileName)))
        {
            var firstImage = post.PostMedia.First(pm => pm.MediaType == MediaType.Image && !string.IsNullOrEmpty(pm.ImageFileName));
            imageUrl = GenerateImageUrl(firstImage.ImageFileName);
        }

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
                post.HiddenReason ?? post.HiddenReasonType.ToString(), // Use admin's text reason or enum as fallback
                post.HiddenAt,
                post.HiddenByUser?.MapToUserDto(),
                systemTags,
                null, // Risk score
                null, // Risk level
                appealInfo
            );
        }

        // Generate video URLs
        var videoUrl = GenerateVideoUrl(post.ProcessedVideoFileName);
        var videoThumbnailUrl = GenerateVideoThumbnailUrl(post.VideoThumbnailFileName);

        // Map video metadata if available
        VideoMetadata? videoMetadata = null;
        var videoMedia = post.VideoMedia;
        if (videoMedia != null && videoMedia.VideoWidth.HasValue && videoMedia.VideoHeight.HasValue)
        {
            videoMetadata = new VideoMetadata
            {
                ProcessedWidth = videoMedia.VideoWidth.Value,
                ProcessedHeight = videoMedia.VideoHeight.Value,
                ProcessedDuration = videoMedia.VideoDuration ?? TimeSpan.Zero,
                ProcessedFileSizeBytes = videoMedia.VideoFileSizeBytes ?? 0,
                ProcessedFormat = videoMedia.VideoFormat ?? string.Empty,
                ProcessedBitrate = videoMedia.VideoBitrate ?? 0,
                CompressionRatio = videoMedia.VideoCompressionRatio ?? 0,
                // Original metadata from PostMedia
                OriginalWidth = videoMedia.OriginalVideoWidth ?? videoMedia.VideoWidth.Value,
                OriginalHeight = videoMedia.OriginalVideoHeight ?? videoMedia.VideoHeight.Value,
                OriginalDuration = videoMedia.OriginalVideoDuration ?? videoMedia.VideoDuration ?? TimeSpan.Zero,
                OriginalFileSizeBytes = videoMedia.OriginalVideoFileSizeBytes ?? videoMedia.VideoFileSizeBytes ?? 0,
                OriginalFormat = videoMedia.OriginalVideoFormat ?? videoMedia.VideoFormat ?? string.Empty,
                OriginalBitrate = videoMedia.OriginalVideoBitrate ?? videoMedia.VideoBitrate ?? 0,
                // Rotation metadata
                OriginalRotation = videoMedia.OriginalVideoRotation ?? 0,
                ProcessedRotation = videoMedia.ProcessedVideoRotation ?? 0,
                DisplayWidth = videoMedia.DisplayVideoWidth ?? videoMedia.VideoWidth.Value,
                DisplayHeight = videoMedia.DisplayVideoHeight ?? videoMedia.VideoHeight.Value
            };
        }

        // Map media items
        var mediaItems = post.PostMedia.Select(media => media.MapToPostMediaDto()).ToList();
        
        // Get reaction data
        var reactionCounts = post.Reactions
            .GroupBy(r => r.ReactionType)
            .Select(g => new ReactionCountDto(
                g.Key,
                g.Key.GetEmoji(),
                g.Key.GetDisplayName(),
                g.Count()
            ))
            .ToList();

        var currentUserReaction = currentUserId.HasValue
            ? post.Reactions.FirstOrDefault(r => r.UserId == currentUserId.Value)?.ReactionType
            : null;

        var totalReactionCount = post.Reactions.Count;

        // Map reposted post if this is a repost
        PostDto? repostedPostDto = null;
        if (post.PostType == PostType.Repost && post.RepostedPost != null)
        {
            // Recursively map the reposted post (but don't include moderation info to avoid deep nesting)
            repostedPostDto = post.RepostedPost.MapToPostDto(currentUserId, includeModeration: false);
        }

        return new PostDto(
            post.Id,
            post.Content,
            imageUrl,
            videoUrl,
            videoThumbnailUrl,
            post.VideoFileName != null ? post.VideoProcessingStatus : null,
            post.Privacy,
            post.CreatedAt,
            post.UpdatedAt,
            userDto,
            post.Group.MapToGroupDto(currentUserId),
            post.Likes.Count,
            post.Children.Count(c => c.PostType == PostType.Comment),
            post.Reposts.Count, // Count from new unified system
            tags,
            linkPreviews,
            isLiked,
            isReposted,
            isEdited,
            moderationInfo,
            videoMetadata,
            mediaItems,
            reactionCounts,
            currentUserReaction,
            totalReactionCount,
            post.PostType,
            repostedPostDto
        );
    }

    /// <summary>
    /// Map Post to PostDto with cached counts (for performance when collections aren't loaded)
    /// </summary>
    public static PostDto MapToPostDtoWithCachedCounts(
        this Post post,
        int? currentUserId,
        int likeCount,
        int commentCount,
        int repostCount,
        IEnumerable<ReactionCountDto>? reactionCounts = null,
        ReactionType? currentUserReaction = null,
        int totalReactionCount = 0,
        bool includeModeration = false)
    {
        var userDto = post.User.MapToUserDto();
        var isLiked = currentUserId.HasValue && post.Likes.Any(l => l.UserId == currentUserId.Value);
        var isReposted = currentUserId.HasValue && post.Reposts.Any(r => r.UserId == currentUserId.Value);

        // Generate legacy imageUrl - use Post.ImageFileName if available, otherwise use first image from PostMedia
        var imageUrl = GenerateImageUrl(post.ImageFileName);
        if (string.IsNullOrEmpty(imageUrl) && post.PostMedia.Any(pm => pm.MediaType == MediaType.Image && !string.IsNullOrEmpty(pm.ImageFileName)))
        {
            var firstImage = post.PostMedia.First(pm => pm.MediaType == MediaType.Image && !string.IsNullOrEmpty(pm.ImageFileName));
            imageUrl = GenerateImageUrl(firstImage.ImageFileName);
        }

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
                post.HiddenReason, // Use the admin's text reason, not the enum
                post.HiddenAt,
                post.HiddenByUser?.MapToUserDto(),
                systemTags,
                null, // Risk score
                null, // Risk level
                appealInfo
            );
        }

        // Generate video URLs
        string? videoUrl = null;
        string? videoThumbnailUrl = null;
        VideoMetadata? videoMetadata = null;

        if (!string.IsNullOrEmpty(post.VideoFileName))
        {
            videoUrl = GenerateVideoUrl(post.ProcessedVideoFileName);
            videoThumbnailUrl = GenerateVideoThumbnailUrl(post.VideoThumbnailFileName);

            // Map video metadata if available
            if (post.PostMedia.Any(pm => pm.MediaType == MediaType.Video))
            {
                var videoMedia = post.PostMedia.First(pm => pm.MediaType == MediaType.Video);
                if (videoMedia.VideoWidth.HasValue && videoMedia.VideoHeight.HasValue)
                {
                    videoMetadata = new VideoMetadata
                    {
                        ProcessedWidth = videoMedia.VideoWidth.Value,
                        ProcessedHeight = videoMedia.VideoHeight.Value,
                        ProcessedDuration = videoMedia.VideoDuration ?? TimeSpan.Zero,
                        ProcessedFileSizeBytes = videoMedia.VideoFileSizeBytes ?? 0,
                        ProcessedFormat = videoMedia.VideoFormat ?? string.Empty,
                        ProcessedBitrate = videoMedia.VideoBitrate ?? 0,
                        CompressionRatio = videoMedia.VideoCompressionRatio ?? 0,
                        // Original metadata from PostMedia
                        OriginalWidth = videoMedia.OriginalVideoWidth ?? videoMedia.VideoWidth.Value,
                        OriginalHeight = videoMedia.OriginalVideoHeight ?? videoMedia.VideoHeight.Value,
                        OriginalDuration = videoMedia.OriginalVideoDuration ?? videoMedia.VideoDuration ?? TimeSpan.Zero,
                        OriginalFileSizeBytes = videoMedia.OriginalVideoFileSizeBytes ?? videoMedia.VideoFileSizeBytes ?? 0,
                        OriginalFormat = videoMedia.OriginalVideoFormat ?? videoMedia.VideoFormat ?? string.Empty,
                        OriginalBitrate = videoMedia.OriginalVideoBitrate ?? videoMedia.VideoBitrate ?? 0,
                        // Rotation metadata
                        OriginalRotation = videoMedia.OriginalVideoRotation ?? 0,
                        ProcessedRotation = videoMedia.ProcessedVideoRotation ?? 0,
                        DisplayWidth = videoMedia.DisplayVideoWidth ?? videoMedia.VideoWidth.Value,
                        DisplayHeight = videoMedia.DisplayVideoHeight ?? videoMedia.VideoHeight.Value
                    };
                }
            }
        }

        // Map media items
        var mediaItems = post.PostMedia.Select(media => media.MapToPostMediaDto()).ToList();

        // Map reposted post if this is a repost
        PostDto? repostedPostDto = null;
        if (post.PostType == PostType.Repost && post.RepostedPost != null)
        {
            // Recursively map the reposted post (but don't include moderation info to avoid deep nesting)
            repostedPostDto = post.RepostedPost.MapToPostDto(currentUserId, includeModeration: false);
        }

        return new PostDto(
            post.Id,
            post.Content,
            imageUrl,
            videoUrl,
            videoThumbnailUrl,
            post.VideoFileName != null ? post.VideoProcessingStatus : null,
            post.Privacy,
            post.CreatedAt,
            post.UpdatedAt,
            userDto,
            post.Group.MapToGroupDto(currentUserId),
            likeCount,
            commentCount,
            repostCount,
            tags,
            linkPreviews,
            isLiked,
            isReposted,
            isEdited,
            moderationInfo,
            videoMetadata,
            mediaItems,
            reactionCounts,
            currentUserReaction,
            totalReactionCount,
            post.PostType,
            repostedPostDto
        );
    }

    /// <summary>
    /// Map Message to MessageDto
    /// </summary>
    public static MessageDto MapToMessageDto(
        this Message message,
        MessageStatusType? status = null,
        UserDto? sender = null)
    {
        var senderDto = sender ?? message.Sender?.MapToUserDto() ?? CreateSystemUserDto();

        return new MessageDto(
            message.Id,
            message.Content,
            GenerateImageUrl(message.ImageFileName),
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
        int unreadCount = 0)
    {
        var participants = conversation.Participants.Select(p => p.User.MapToUserDto()).ToList();
        var lastMessage = conversation.Messages.FirstOrDefault();
        var lastMessageDto = lastMessage?.MapToMessageDto();

        return new ConversationDto(
            conversation.Id,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            participants,
            lastMessageDto,
            unreadCount
        );
    }

    /// <summary>
    /// Map PostMedia to PostMediaDto
    /// </summary>
    public static PostMediaDto MapToPostMediaDto(this PostMedia media)
    {


        string? imageUrl = null;
        string? videoUrl = null;
        string? videoThumbnailUrl = null;
        VideoProcessingStatus? videoProcessingStatus = null;
        string? gifUrl = null;
        string? gifPreviewUrl = null;
        int? width = null;
        int? height = null;
        TimeSpan? duration = null;
        long? fileSizeBytes = null;
        string? format = null;
        VideoMetadata? videoMetadata = null;

        if (media.MediaType == MediaType.Image)
        {
            imageUrl = GenerateImageUrl(media.ImageFileName);
            width = media.ImageWidth;
            height = media.ImageHeight;
            fileSizeBytes = media.ImageFileSizeBytes;
            format = media.ImageFormat;
        }
        else if (media.MediaType == MediaType.Gif)
        {
            Console.WriteLine($"🎭 Mapping GIF media: ID={media.Id}, GifUrl='{media.GifUrl}', GifPreviewUrl='{media.GifPreviewUrl}'");
            gifUrl = media.GifUrl;
            gifPreviewUrl = media.GifPreviewUrl;
            width = media.ImageWidth;
            height = media.ImageHeight;

            // Validate GIF URLs
            if (string.IsNullOrEmpty(gifUrl))
            {
                Console.WriteLine($"⚠️ Warning: GIF media {media.Id} has null or empty GifUrl");
            }
            if (string.IsNullOrEmpty(gifPreviewUrl))
            {
                Console.WriteLine($"⚠️ Warning: GIF media {media.Id} has null or empty GifPreviewUrl");
            }
        }
        else if (media.MediaType == MediaType.Video)
        {
            videoUrl = GenerateVideoUrl(media.ProcessedVideoFileName);
            videoThumbnailUrl = GenerateVideoThumbnailUrl(media.VideoThumbnailFileName);
            videoProcessingStatus = media.VideoProcessingStatus;
            width = media.VideoWidth ?? media.OriginalVideoWidth;
            height = media.VideoHeight ?? media.OriginalVideoHeight;
            duration = media.VideoDuration ?? media.OriginalVideoDuration;
            fileSizeBytes = media.VideoFileSizeBytes ?? media.OriginalVideoFileSizeBytes;
            format = media.VideoFormat ?? media.OriginalVideoFormat;

            // Create video metadata if available
            if (media.VideoWidth.HasValue && media.VideoHeight.HasValue)
            {
                videoMetadata = new VideoMetadata
                {
                    ProcessedWidth = media.VideoWidth.Value,
                    ProcessedHeight = media.VideoHeight.Value,
                    ProcessedDuration = media.VideoDuration ?? TimeSpan.Zero,
                    ProcessedFileSizeBytes = media.VideoFileSizeBytes ?? 0,
                    ProcessedFormat = media.VideoFormat ?? string.Empty,
                    ProcessedBitrate = media.VideoBitrate ?? 0,
                    CompressionRatio = media.VideoCompressionRatio ?? 0,
                    OriginalWidth = media.OriginalVideoWidth ?? media.VideoWidth.Value,
                    OriginalHeight = media.OriginalVideoHeight ?? media.VideoHeight.Value,
                    OriginalDuration = media.OriginalVideoDuration ?? media.VideoDuration ?? TimeSpan.Zero,
                    OriginalFileSizeBytes = media.OriginalVideoFileSizeBytes ?? media.VideoFileSizeBytes ?? 0,
                    OriginalFormat = media.OriginalVideoFormat ?? media.VideoFormat ?? string.Empty,
                    OriginalBitrate = media.OriginalVideoBitrate ?? media.VideoBitrate ?? 0,
                    // Rotation metadata
                    OriginalRotation = media.OriginalVideoRotation ?? 0,
                    ProcessedRotation = media.ProcessedVideoRotation ?? 0,
                    DisplayWidth = media.DisplayVideoWidth ?? media.VideoWidth.Value,
                    DisplayHeight = media.DisplayVideoHeight ?? media.VideoHeight.Value
                };
            }
        }

        return new PostMediaDto(
            media.Id,
            media.MediaType,
            imageUrl,
            videoUrl,
            videoThumbnailUrl,
            videoProcessingStatus,
            gifUrl,
            gifPreviewUrl,
            width,
            height,
            duration,
            fileSizeBytes,
            format,
            media.CreatedAt,
            videoMetadata
        );
    }
    
    public static GroupDto? MapToGroupDto(this Group? group, int? currentUserId)
    {
        if (group == null) return null;
        var isCurrentUserMember = currentUserId.HasValue &&
                                  group.Members.Any(m => m.UserId == currentUserId.Value);

        // Calculate visible post count (same filtering as GetGroupPostsAsync)
        var visiblePostCount = group.Posts.Count(p =>
            p.PostType == PostType.Post && !p.IsHidden && !p.IsDeletedByUser);

        return new GroupDto(
            group.Id,
            group.Name,
            group.Description,
            GenerateImageUrl(group.ImageFileName),
            group.CreatedAt,
            group.UpdatedAt,
            group.IsOpen,
            group.User.MapToUserDto(),
            group.Members.Count,
            visiblePostCount,
            isCurrentUserMember
        );
    }
    
    public static GroupListDto MapToGroupListDto(this Group group, int? currentUserId)
    {
        var isCurrentUserMember = currentUserId.HasValue &&
                                  group.Members.Any(m => m.UserId == currentUserId.Value);

        // Calculate visible post count (same filtering as GetGroupPostsAsync)
        var visiblePostCount = group.Posts.Count(p =>
            p.PostType == PostType.Post && !p.IsHidden && !p.IsDeletedByUser);

        return new GroupListDto(
            group.Id,
            group.Name,
            group.Description,
            GenerateImageUrl(group.ImageFileName),
            group.CreatedAt,
            group.User.Username,
            group.Members.Count,
            visiblePostCount,
            isCurrentUserMember
        );
    }
    
    public static SystemTagDto MapToSystemTagDto(this SystemTag systemTag)
    {
        return new SystemTagDto
        {
            Id = systemTag.Id,
            Name = systemTag.Name,
            Description = systemTag.Description,
            Category = systemTag.Category,
            IsVisibleToUsers = systemTag.IsVisibleToUsers,
            IsActive = systemTag.IsActive,
            Color = systemTag.Color,
            Icon = systemTag.Icon,
            SortOrder = systemTag.SortOrder,
            CreatedAt = systemTag.CreatedAt,
            UpdatedAt = systemTag.UpdatedAt
        };
    }
    
    public static AdminPostDto MapToAdminPostDto(this Post post)
    {
        // Map group information if post is in a group
        GroupDto? groupDto = null;
        if (post.Group != null)
        {
            groupDto = new GroupDto(
                post.Group.Id,
                post.Group.Name,
                post.Group.Description,
                post.Group.ImageFileName,
                post.Group.CreatedAt,
                post.Group.UpdatedAt,
                post.Group.IsOpen,
                post.Group.User.MapToUserDto(),
                post.Group.Members?.Count ?? 0,
                post.Group.Posts?.Count ?? 0,
                false // IsCurrentUserMember - we don't have this info in admin context
            );
        }

        return new AdminPostDto
        {
            Id = post.Id,
            Content = post.Content,
            ImageUrl = GenerateImageUrl(post.ImageFileName),
            Privacy = post.Privacy,
            IsHidden = post.IsHidden,
            HiddenReason = post.HiddenReason ?? post.HiddenReasonType.ToString(),
            HiddenAt = post.HiddenAt,
            HiddenByUsername = post.HiddenByUser?.Username,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            User = post.User.MapToUserDto(),
            Group = groupDto,
            LikeCount = post.Likes?.Count ?? 0,
            CommentCount = post.Children?.Count(c => c.PostType == PostType.Comment) ?? 0,
            RepostCount = post.Reposts?.Count ?? 0,
            SystemTags = post.PostSystemTags?.Select(pst => pst.SystemTag.MapToSystemTagDto()).ToList() ?? new List<SystemTagDto>()
        };
    }

    public static AdminPostDto MapToAdminPostDtoWithTags(this Post post, List<AiSuggestedTag> aiSuggestedTags)
    {
        var adminPostDto = MapToAdminPostDto(post);
        adminPostDto.AiSuggestedTags = aiSuggestedTags.Select(MapToAiSuggestedTagDto).ToList();
        return adminPostDto;
    }



    public static AiSuggestedTagDto MapToAiSuggestedTagDto(this AiSuggestedTag aiSuggestedTag)
    {
        return new AiSuggestedTagDto
        {
            Id = aiSuggestedTag.Id,
            TagName = aiSuggestedTag.TagName,
            Category = aiSuggestedTag.Category,
            Confidence = aiSuggestedTag.Confidence,
            RiskLevel = aiSuggestedTag.RiskLevel,
            RequiresReview = aiSuggestedTag.RequiresReview,
            SuggestedAt = aiSuggestedTag.SuggestedAt,
            IsApproved = aiSuggestedTag.IsApproved,
            IsRejected = aiSuggestedTag.IsRejected,
            ApprovedByUserId = aiSuggestedTag.ApprovedByUserId,
            ApprovedByUsername = aiSuggestedTag.ApprovedByUser?.Username,
            ApprovedAt = aiSuggestedTag.ApprovedAt,
            ApprovalReason = aiSuggestedTag.ApprovalReason
        };
    }
    
    public static AdminCommentDto MapPostToAdminCommentDto(this Post post)
    {
        if (post.PostType != PostType.Comment)
            throw new ArgumentException("Post must be of type Comment", nameof(post));

        // Map group information if comment is in a group
        GroupDto? groupDto = null;
        if (post.Group != null)
        {
            groupDto = new GroupDto(
                post.Group.Id,
                post.Group.Name,
                post.Group.Description,
                GenerateImageUrl(post.Group.ImageFileName),
                post.Group.CreatedAt,
                post.Group.UpdatedAt,
                post.Group.IsOpen,
                post.Group.User.MapToUserDto(),
                post.Group.Members?.Count ?? 0,
                post.Group.Posts?.Count ?? 0,
                false // IsCurrentUserMember - we don't have this info in admin context
            );
        }

        return new AdminCommentDto
        {
            Id = post.Id,
            Content = post.Content,
            IsHidden = post.IsHidden,
            HiddenReason = post.HiddenReason,
            HiddenAt = post.HiddenAt,
            HiddenByUsername = post.HiddenByUser?.Username,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            User = post.User.MapToUserDto(),
            Group = groupDto,
            PostId = post.ParentId ?? 0, // ParentId is the original PostId for comments
            SystemTags = post.PostSystemTags?.Select(pst => pst.SystemTag.MapToSystemTagDto()).ToList() ?? new List<SystemTagDto>()
        };
    }

    public static UserAppealDto MapToUserAppealDto(this UserAppeal appeal)
    {
        return new UserAppealDto
        {
            Id = appeal.Id,
            Username = appeal.User.Username,
            Type = appeal.Type,
            Status = appeal.Status,
            Reason = appeal.Reason,
            AdditionalInfo = appeal.AdditionalInfo,
            TargetPostId = appeal.TargetPostId,
            TargetCommentId = appeal.TargetCommentId,
            ReviewedByUsername = appeal.ReviewedByUser?.Username,
            ReviewNotes = appeal.ReviewNotes,
            ReviewedAt = appeal.ReviewedAt,
            CreatedAt = appeal.CreatedAt
        };
    }

    public static UserReportDto MapToUserReportDto(this UserReport report)
    {
        return new UserReportDto
        {
            Id = report.Id,
            ReportedByUsername = report.ReportedByUser.Username,
            Status = report.Status,
            Reason = report.Reason,
            CreatedAt = report.CreatedAt,
            ReviewedAt = report.ReviewedAt,
            ReviewedByUsername = report.ReviewedByUser?.Username,
            ReviewNotes = report.ReviewNotes,
            Post = report.Post != null ? MapToAdminPostDto(report.Post) : null,
            Comment = report.Comment != null ? MapPostToAdminCommentDto(report.Comment) : null,
            SystemTags = report.UserReportSystemTags?.Select(urst => urst.SystemTag.MapToSystemTagDto()).ToList() ?? new List<SystemTagDto>()
        };
    }

    public static NotificationDto MapToNotificationDto(this Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Message = notification.Message,
            IsRead = notification.IsRead,
            IsSeen = notification.IsSeen,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt,
            SeenAt = notification.SeenAt,
            ActorUser = notification.ActorUser?.MapToUserDto(),
            Post = notification.Post
                ?.MapToPostDto(null), // Pass null for currentUserId since we don't need reaction info here
            Comment = notification.Comment?.MapToCommentDto() // Use the basic mapping without like info
        };
    }
}
