using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Extensions;
using Yapplr.Api.Utils;
using Yapplr.Api.Common;
using Yapplr.Api.Services.Unified;
using MassTransit;
using Yapplr.Shared.Messages;
using Yapplr.Shared.Models;
using Serilog.Context;

namespace Yapplr.Api.Services;

public class PostService : BaseService, IPostService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IBlockService _blockService;
    private readonly IUnifiedNotificationService _notificationService;
    private readonly ILinkPreviewService _linkPreviewService;
    private readonly IContentModerationService _contentModerationService;
    private readonly IConfiguration _configuration;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICountCacheService _countCache;
    private readonly ITrustScoreService _trustScoreService;
    private readonly ITrustBasedModerationService _trustBasedModerationService;
    private readonly IAnalyticsService _analyticsService;

    public PostService(
        YapplrDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IBlockService blockService,
        IUnifiedNotificationService notificationService,
        ILinkPreviewService linkPreviewService,
        IContentModerationService contentModerationService,
        IConfiguration configuration,
        IPublishEndpoint publishEndpoint,
        ICountCacheService countCache,
        ITrustScoreService trustScoreService,
        ITrustBasedModerationService trustBasedModerationService,
        IAnalyticsService analyticsService,
        ILogger<PostService> logger) : base(context, logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _blockService = blockService;
        _notificationService = notificationService;
        _linkPreviewService = linkPreviewService;
        _contentModerationService = contentModerationService;
        _configuration = configuration;
        _publishEndpoint = publishEndpoint;
        _countCache = countCache;
        _trustScoreService = trustScoreService;
        _trustBasedModerationService = trustBasedModerationService;
        _analyticsService = analyticsService;
    }

    public async Task<PostDto?> CreatePostAsync(int userId, CreatePostDto createDto)
    {
        using var timedOperation = _logger.BeginTimedOperation("CreatePost", new { UserId = userId });
        using var operationScope = Serilog.Context.LogContext.PushProperty("Operation", "CreatePost");
        using var userScope = Serilog.Context.LogContext.PushProperty("UserId", userId);

        var mediaCount = (createDto.MediaFileNames?.Count ?? 0) +
                        (!string.IsNullOrEmpty(createDto.ImageFileName) ? 1 : 0) +
                        (!string.IsNullOrEmpty(createDto.VideoFileName) ? 1 : 0);

        using var mediaScope = Serilog.Context.LogContext.PushProperty("MediaCount", mediaCount);
        using var privacyScope = Serilog.Context.LogContext.PushProperty("Privacy", createDto.Privacy.ToString());

        _logger.LogInformation("Creating post for user {UserId} with {MediaCount} media files and privacy {Privacy}",
            userId, mediaCount, createDto.Privacy);

        // Check trust-based permissions
        if (!await _trustBasedModerationService.CanPerformActionAsync(userId, TrustRequiredAction.CreatePost))
        {
            _logger.LogWarning("Post creation denied for user {UserId}: Insufficient trust score", userId);
            throw new InvalidOperationException("Insufficient trust score to create posts");
        }

        // If posting to a group, validate group membership and override privacy
        PostPrivacy effectivePrivacy = createDto.Privacy;
        if (createDto.GroupId.HasValue)
        {
            // Check if group exists
            var group = await _context.Groups.FindAsync(createDto.GroupId.Value);
            if (group == null)
            {
                throw new InvalidOperationException("Group not found");
            }

            // Check if user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == createDto.GroupId.Value && gm.UserId == userId);

            if (!isMember)
            {
                throw new InvalidOperationException("You must be a member of the group to post in it");
            }

            // Posts in groups are always public
            effectivePrivacy = PostPrivacy.Public;
            _logger.LogInformation("Post will be created in group {GroupId} with public privacy", createDto.GroupId.Value);
        }

        // Check if post contains any videos
        var hasVideos = !string.IsNullOrEmpty(createDto.VideoFileName) ||
                       (createDto.MediaFileNames?.Any(fileName =>
                           !string.IsNullOrEmpty(fileName) &&
                           DetermineMediaTypeFromFileName(fileName) == MediaType.Video) == true);

        using var videoScope = Serilog.Context.LogContext.PushProperty("HasVideos", hasVideos);

        var post = new Post
        {
            Content = createDto.Content,
            Privacy = effectivePrivacy,
            UserId = userId,
            GroupId = createDto.GroupId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            // Hide posts with videos during processing so they're only visible to the author
            IsHiddenDuringVideoProcessing = hasVideos
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        using var postScope = Serilog.Context.LogContext.PushProperty("PostId", post.Id);
        _logger.LogInformation("Post {PostId} created successfully for user {UserId}", post.Id, userId);

        // Create media records if present
        var hasMedia = false;

        // Handle legacy single file properties for backward compatibility
        if (!string.IsNullOrEmpty(createDto.ImageFileName))
        {
            var imageMedia = new PostMedia
            {
                PostId = post.Id,
                MediaType = MediaType.Image,
                ImageFileName = createDto.ImageFileName,
                OriginalFileName = createDto.ImageFileName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.PostMedia.Add(imageMedia);
            hasMedia = true;
        }

        if (!string.IsNullOrEmpty(createDto.VideoFileName))
        {
            var videoMedia = new PostMedia
            {
                PostId = post.Id,
                MediaType = MediaType.Video,
                VideoFileName = createDto.VideoFileName,
                OriginalFileName = createDto.VideoFileName,
                VideoProcessingStatus = VideoProcessingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.PostMedia.Add(videoMedia);
            hasMedia = true;
        }

        // Handle multiple media files
        if (createDto.MediaFileNames != null && createDto.MediaFileNames.Count > 0)
        {
            foreach (var fileName in createDto.MediaFileNames)
            {
                if (string.IsNullOrEmpty(fileName)) continue;

                // Determine media type based on file extension
                var mediaType = DetermineMediaTypeFromFileName(fileName);

                var media = new PostMedia
                {
                    PostId = post.Id,
                    MediaType = mediaType,
                    OriginalFileName = fileName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (mediaType == MediaType.Image)
                {
                    media.ImageFileName = fileName;
                }
                else if (mediaType == MediaType.Video)
                {
                    media.VideoFileName = fileName;
                    media.VideoProcessingStatus = VideoProcessingStatus.Pending;
                }

                _context.PostMedia.Add(media);
                hasMedia = true;
            }
        }

        if (hasMedia)
        {
            await _context.SaveChangesAsync();
        }

        // Process hashtags in the post content
        await ProcessPostTagsAsync(post.Id, createDto.Content);

        // Process link previews in the post content
        await ProcessPostLinkPreviewsAsync(post.Id, createDto.Content);

        // Create mention notifications for users mentioned in the post
        await _notificationService.CreateMentionNotificationsAsync(createDto.Content, userId, post.Id);

        // Perform content moderation analysis
        await ProcessContentModerationAsync(post.Id, createDto.Content, userId);

        // Process videos if present
        var videoFileNames = new List<string>();

        // Add legacy single video file
        if (!string.IsNullOrEmpty(createDto.VideoFileName))
        {
            videoFileNames.Add(createDto.VideoFileName);
        }

        // Add multiple video files
        if (createDto.MediaFileNames != null)
        {
            foreach (var fileName in createDto.MediaFileNames)
            {
                if (!string.IsNullOrEmpty(fileName) && DetermineMediaTypeFromFileName(fileName) == MediaType.Video)
                {
                    videoFileNames.Add(fileName);
                }
            }
        }

        // Process each video
        foreach (var videoFileName in videoFileNames)
        {
            await ProcessVideoAsync(post.Id, userId, videoFileName, createDto.Content);
        }

        // Invalidate user post count cache
        await _countCache.InvalidateUserCountsAsync(userId);

        // Update trust score for post creation
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                userId,
                TrustScoreAction.PostCreated,
                "post",
                post.Id,
                $"Created post with {createDto.Content.Length} characters"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for post creation by user {UserId}", userId);
            // Don't fail the post creation if trust score update fails
        }

        // Check if post should be auto-hidden based on trust score
        try
        {
            if (await _trustBasedModerationService.ShouldAutoHideContentAsync(userId, "post"))
            {
                post.IsHidden = true;
                post.HiddenReasonType = PostHiddenReasonType.ContentModerationHidden;
                post.HiddenReason = "Auto-hidden due to low trust score";
                post.HiddenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Auto-hidden post {PostId} from low trust user {UserId}", post.Id, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check auto-hide for post {PostId} by user {UserId}", post.Id, userId);
            // Don't fail the post creation if auto-hide check fails
        }

        // Load the post with user data
        var createdPost = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Group)
            .Include(p => p.Likes)
            .Include(p => p.Reactions)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment))
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .Include(p => p.PostMedia)
            .AsSplitQuery()
            .FirstAsync(p => p.Id == post.Id);

        return createdPost.MapToPostDto(userId);
    }

    public async Task<PostDto?> CreatePostWithMediaAsync(int userId, CreatePostWithMediaDto createDto)
    {
        _logger.LogInformation("Starting post creation with media for user {UserId}", userId);

        // Check trust-based permissions with timeout and error handling
        try
        {
            _logger.LogDebug("Checking trust-based permissions for user {UserId}", userId);
            var canPerformAction = await _trustBasedModerationService.CanPerformActionAsync(userId, TrustRequiredAction.CreatePost);
            if (!canPerformAction)
            {
                _logger.LogWarning("User {UserId} denied post creation due to insufficient trust score", userId);
                throw new InvalidOperationException("Insufficient trust score to create posts");
            }
            _logger.LogDebug("Trust-based permissions check completed for user {UserId}", userId);
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Error checking trust-based permissions for user {UserId}, allowing action to proceed", userId);
            // Continue with post creation if trust check fails to avoid blocking users
        }

        // If posting to a group, validate group membership and override privacy
        PostPrivacy effectivePrivacy = createDto.Privacy;
        if (createDto.GroupId.HasValue)
        {
            // Check if group exists
            var group = await _context.Groups.FindAsync(createDto.GroupId.Value);
            if (group == null)
            {
                throw new InvalidOperationException("Group not found");
            }

            // Check if user is a member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == createDto.GroupId.Value && gm.UserId == userId);

            if (!isMember)
            {
                throw new InvalidOperationException("You must be a member of the group to post in it");
            }

            // Posts in groups are always public
            effectivePrivacy = PostPrivacy.Public;
            _logger.LogInformation("Post will be created in group {GroupId} with public privacy", createDto.GroupId.Value);
        }

        // Validate that either content or media files are provided
        var hasContent = !string.IsNullOrWhiteSpace(createDto.Content);
        var hasMediaFiles = createDto.MediaFiles != null && createDto.MediaFiles.Count > 0;

        if (!hasContent && !hasMediaFiles)
        {
            throw new ArgumentException("Either content or media files must be provided");
        }

        // Validate media files count
        if (createDto.MediaFiles != null && createDto.MediaFiles.Count > 10)
        {
            throw new ArgumentException("Maximum 10 media files allowed per post");
        }

        // Check if post contains any videos
        var hasVideos = createDto.MediaFiles?.Any(m => m.MediaType == MediaType.Video) == true;

        _logger.LogDebug("Creating post entity for user {UserId}", userId);
        var post = new Post
        {
            Content = string.IsNullOrWhiteSpace(createDto.Content) ? string.Empty : createDto.Content,
            Privacy = effectivePrivacy,
            UserId = userId,
            GroupId = createDto.GroupId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            // Hide posts with videos during processing so they're only visible to the author
            IsHiddenDuringVideoProcessing = hasVideos
        };

        _context.Posts.Add(post);
        _logger.LogDebug("Saving post to database for user {UserId}", userId);
        await _context.SaveChangesAsync();
        _logger.LogDebug("Post {PostId} saved successfully for user {UserId}", post.Id, userId);

        // Create media records
        if (createDto.MediaFiles != null && createDto.MediaFiles.Count > 0)
        {
            _logger.LogDebug("Processing {MediaCount} media files for post {PostId}", createDto.MediaFiles.Count, post.Id);
            foreach (var mediaFile in createDto.MediaFiles)
            {
                // Validate that non-GIF media types have a fileName
                if (mediaFile.MediaType != MediaType.Gif && string.IsNullOrEmpty(mediaFile.FileName))
                {
                    throw new InvalidOperationException($"FileName is required for {mediaFile.MediaType} media type");
                }

                // Validate that GIF media types have URLs
                if (mediaFile.MediaType == MediaType.Gif && (string.IsNullOrEmpty(mediaFile.GifUrl) || string.IsNullOrEmpty(mediaFile.GifPreviewUrl)))
                {
                    throw new InvalidOperationException("GifUrl and GifPreviewUrl are required for GIF media type");
                }
                var media = new PostMedia
                {
                    PostId = post.Id,
                    MediaType = mediaFile.MediaType,
                    OriginalFileName = mediaFile.FileName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (mediaFile.MediaType == MediaType.Image)
                {
                    media.ImageFileName = mediaFile.FileName;
                    media.ImageWidth = mediaFile.Width;
                    media.ImageHeight = mediaFile.Height;
                    media.ImageFileSizeBytes = mediaFile.FileSizeBytes;
                }
                else if (mediaFile.MediaType == MediaType.Video)
                {
                    media.VideoFileName = mediaFile.FileName;
                    media.VideoProcessingStatus = VideoProcessingStatus.Pending;
                    media.OriginalVideoWidth = mediaFile.Width;
                    media.OriginalVideoHeight = mediaFile.Height;
                    media.OriginalVideoDuration = mediaFile.Duration;
                    media.OriginalVideoFileSizeBytes = mediaFile.FileSizeBytes;
                }
                else if (mediaFile.MediaType == MediaType.Gif)
                {
                    media.GifUrl = mediaFile.GifUrl;
                    media.GifPreviewUrl = mediaFile.GifPreviewUrl;
                    media.ImageWidth = mediaFile.Width;
                    media.ImageHeight = mediaFile.Height;
                }

                _context.PostMedia.Add(media);
            }

            await _context.SaveChangesAsync();
        }

        // Process hashtags in the post content
        await ProcessPostTagsAsync(post.Id, createDto.Content);

        // Process link previews in the post content
        await ProcessPostLinkPreviewsAsync(post.Id, createDto.Content);

        // Create mention notifications for users mentioned in the post
        await _notificationService.CreateMentionNotificationsAsync(createDto.Content, userId, post.Id);

        // Perform content moderation analysis
        await ProcessContentModerationAsync(post.Id, createDto.Content, userId);

        // Process videos if present
        if (createDto.MediaFiles != null)
        {
            var videoFiles = createDto.MediaFiles.Where(m => m.MediaType == MediaType.Video);
            foreach (var videoFile in videoFiles)
            {
                await ProcessVideoAsync(post.Id, userId, videoFile.FileName, createDto.Content);
            }
        }

        // Invalidate user post count cache
        await _countCache.InvalidateUserCountsAsync(userId);

        // Update trust score for post creation
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                userId,
                TrustScoreAction.PostCreated,
                "post",
                post.Id,
                $"Created post with {createDto.Content?.Length} characters and {createDto.MediaFiles?.Count ?? 0} media files"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for post creation by user {UserId}", userId);
            // Don't fail the post creation if trust score update fails
        }

        // Check if post should be auto-hidden based on trust score
        try
        {
            var shouldAutoHide = await _trustBasedModerationService.ShouldAutoHideContentAsync(userId, createDto.Content);
            if (shouldAutoHide)
            {
                post.IsHidden = true;
                post.HiddenReasonType = PostHiddenReasonType.ContentModerationHidden;
                post.HiddenReason = "Content automatically hidden due to low trust score";
                post.HiddenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Post {PostId} by user {UserId} auto-hidden due to low trust score", post.Id, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check auto-hide for post {PostId} by user {UserId}", post.Id, userId);
            // Don't fail the post creation if auto-hide check fails
        }

        // Load the post with user data
        var createdPost = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Group)
            .Include(p => p.Likes)
            .Include(p => p.Reactions)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment))
            .Include(p => p.Reposts)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .Include(p => p.PostMedia)
            .AsSplitQuery()
            .FirstAsync(p => p.Id == post.Id);

        return createdPost.MapToPostDto(userId);
    }

    public async Task<PostDto?> GetPostByIdAsync(int postId, int? currentUserId = null)
    {
        var post = await _context.GetPostsWithIncludes()
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null) return null;

        // Use hybrid visibility system to check if user can view this post
        var blockedUserIds = currentUserId.HasValue
            ? await _context.GetBlockedUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();
        var followingIds = currentUserId.HasValue
            ? await _context.GetFollowingUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();

        var visiblePosts = new[] { post }.AsQueryable()
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .ToList();

        if (!visiblePosts.Any())
        {
            return null; // Post is hidden from this user
        }

        return post.MapToPostDto(currentUserId, includeModeration: true);
    }



    public async Task<IEnumerable<TimelineItemDto>> GetTimelineAsync(int userId, int page = 1, int pageSize = 20)
    {
        LogOperation(nameof(GetTimelineAsync), new { userId, page, pageSize });

        // Get user's following list and blocked users for filtering
        var followingIds = await GetFollowingUserIdsAsync(userId);
        var blockedUserIds = await GetBlockedUserIdsAsync(userId);

        // Create a larger page size for fetching since we'll filter and sort in memory
        var fetchSize = pageSize * 3; // Fetch more to account for mixed posts and reposts

        // Get original posts with filtering
        var posts = await _context.GetPostsForFeed()
            .ApplyVisibilityFilters(userId, blockedUserIds.ToHashSet(), followingIds.ToHashSet())
            .OrderByDescending(p => p.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Get reposts from followed users, self, and public reposts from any user (using new unified system)
        var reposts = await _context.GetPostsWithIncludes()
            .Where(p => p.PostType == PostType.Repost &&
                       !p.IsDeletedByUser &&
                       // Apply same visibility filters as posts - use hybrid system
                       (!p.IsHidden ||
                        (p.HiddenReasonType == PostHiddenReasonType.VideoProcessing && p.UserId == userId)) && // Filter out hidden posts except video processing for author
                       p.User.Status == UserStatus.Active && // Hide reposts from suspended/banned users
                       (p.User.TrustScore >= 0.1f || p.UserId == userId) && // Hide reposts from low trust users except from self
                       !blockedUserIds.Contains(p.UserId) && // Filter out reposts from blocked users
                       (p.UserId == userId || // Reposts from self
                        followingIds.Contains(p.UserId) || // Reposts from followed users
                        p.Privacy == PostPrivacy.Public)) // Public reposts from any user
            .Where(p => p.RepostedPost != null && // Ensure reposted post exists
                       !p.RepostedPost.IsDeletedByUser &&
                       (!p.RepostedPost.IsHidden ||
                        (p.RepostedPost.HiddenReasonType == PostHiddenReasonType.VideoProcessing && p.RepostedPost.UserId == userId)) && // Filter out reposts of hidden posts
                       p.RepostedPost.User.Status == UserStatus.Active && // Hide reposts of posts from suspended/banned users
                       (p.RepostedPost.User.TrustScore >= 0.1f || p.RepostedPost.UserId == userId) && // Hide reposts of low trust posts except from self
                       !blockedUserIds.Contains(p.RepostedPost.UserId) && // Filter out reposts of posts from blocked users
                       (p.RepostedPost.Privacy == PostPrivacy.Public || // Public posts can be reposted
                        p.RepostedPost.UserId == userId || // User's own posts
                        (p.RepostedPost.Privacy == PostPrivacy.Followers && followingIds.Contains(p.RepostedPost.UserId)))) // Followers-only posts if following original author
            .OrderByDescending(p => p.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Note: reposts with content are now included in the main reposts query above

        // Create timeline items and apply pagination
        return CreateTimelineItems(posts, reposts, userId, page, pageSize);
    }

    private static List<TimelineItemDto> CreateTimelineItems(
        IEnumerable<Post> posts,
        IEnumerable<Post> reposts,
        int? currentUserId,
        int page,
        int pageSize)
    {
        var timelineItems = new List<TimelineItemDto>();

        // Add original posts
        timelineItems.AddRange(posts.Select(p => new TimelineItemDto(
            "post",
            p.CreatedAt,
            p.MapToPostDto(currentUserId)
        )));

        // Add reposts (both simple reposts and reposts with content)
        timelineItems.AddRange(reposts.Select(r => new TimelineItemDto(
            "repost",
            r.CreatedAt,
            r.MapToPostDto(currentUserId), // Pass the repost itself for correct ID handling
            r.User.MapToUserDto()
        )));

        // Sort by creation date and apply pagination
        return timelineItems
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<IEnumerable<TimelineItemDto>> GetPublicTimelineAsync(int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        // For public timeline, we only show public posts and reposts of public posts
        var fetchSize = pageSize * 2; // Fetch more to account for mixed posts and reposts

        // Get blocked user IDs if user is authenticated
        var blockedUserIds = currentUserId.HasValue
            ? await _context.GetBlockedUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();

        // Get public posts only
        var posts = await _context.GetPostsForFeed()
            .ApplyPublicVisibilityFilters(blockedUserIds)
            .OrderByDescending(p => p.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Get public reposts (using new unified system)
        var reposts = await _context.GetPostsWithIncludes()
            .Where(p => p.PostType == PostType.Repost &&
                       p.Privacy == PostPrivacy.Public &&
                       !p.IsDeletedByUser &&
                       !p.IsHidden &&
                       p.User.Status == UserStatus.Active &&
                       p.User.TrustScore >= 0.1f &&
                       !blockedUserIds.Contains(p.UserId))
            .Where(p => p.RepostedPost != null && // Ensure reposted post exists
                       !p.RepostedPost.IsDeletedByUser &&
                       !p.RepostedPost.IsHidden &&
                       p.RepostedPost.User.Status == UserStatus.Active &&
                       p.RepostedPost.User.TrustScore >= 0.1f &&
                       p.RepostedPost.Privacy == PostPrivacy.Public && // Only reposts of public posts
                       !blockedUserIds.Contains(p.RepostedPost.UserId))
            .OrderByDescending(p => p.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Create timeline items and apply pagination
        return CreateTimelineItems(posts, reposts, currentUserId, page, pageSize);
    }

    public async Task<IEnumerable<PostDto>> GetUserPostsAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        // Check if current user is blocked by the profile owner or has blocked them
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            var isBlocked = await _blockService.IsUserBlockedAsync(currentUserId.Value, userId) ||
                           await _blockService.IsBlockedByUserAsync(currentUserId.Value, userId);
            if (isBlocked)
            {
                return new List<PostDto>(); // Return empty list if blocked
            }
        }

        // Check if current user is following the target user
        var isFollowing = false;
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == userId);
        }

        var blockedUserIds = currentUserId.HasValue
            ? await _context.GetBlockedUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();
        var followingIds = isFollowing ? new HashSet<int> { userId } : new HashSet<int>();

        var posts = await _context.GetPostsForFeed()
            .Where(p => p.UserId == userId) // Only posts from this user
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(currentUserId));
    }

    public async Task<IEnumerable<PostDto>> GetUserPhotosAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        // Check if current user is blocked by the profile owner or has blocked them
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            var isBlocked = await _blockService.IsUserBlockedAsync(currentUserId.Value, userId) ||
                           await _blockService.IsBlockedByUserAsync(currentUserId.Value, userId);
            if (isBlocked)
            {
                return new List<PostDto>(); // Return empty list if blocked
            }
        }

        // Check if current user is following the target user
        var isFollowing = false;
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == userId);
        }

        var blockedUserIds = currentUserId.HasValue
            ? await _context.GetBlockedUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();
        var followingIds = isFollowing ? new HashSet<int> { userId } : new HashSet<int>();

        var posts = await _context.GetPostsForFeed()
            .Where(p => p.UserId == userId && // Only posts from this user
                   p.PostMedia.Any(pm => pm.MediaType == MediaType.Image)) // Only posts with images
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(currentUserId));
    }

    public async Task<IEnumerable<PostDto>> GetUserVideosAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        // Check if current user is blocked by the profile owner or has blocked them
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            var isBlocked = await _blockService.IsUserBlockedAsync(currentUserId.Value, userId) ||
                           await _blockService.IsBlockedByUserAsync(currentUserId.Value, userId);
            if (isBlocked)
            {
                return new List<PostDto>(); // Return empty list if blocked
            }
        }

        // Check if current user is following the target user
        var isFollowing = false;
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == userId);
        }

        var blockedUserIds = currentUserId.HasValue
            ? await _context.GetBlockedUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();
        var followingIds = isFollowing ? new HashSet<int> { userId } : new HashSet<int>();

        var posts = await _context.GetPostsForFeed()
            .Where(p => p.UserId == userId && // Only posts from this user
                   p.PostMedia.Any(pm => pm.MediaType == MediaType.Video)) // Only posts with videos
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(p => p.MapToPostDto(currentUserId));
    }

    public async Task<IEnumerable<TimelineItemDto>> GetUserTimelineAsync(int userId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        // Check if current user is blocked by the profile owner or has blocked them
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            var isBlocked = await _blockService.IsUserBlockedAsync(currentUserId.Value, userId) ||
                           await _blockService.IsBlockedByUserAsync(currentUserId.Value, userId);
            if (isBlocked)
            {
                return new List<TimelineItemDto>(); // Return empty list if blocked
            }
        }

        // Check if current user is following the target user (for privacy filtering)
        var isFollowing = false;
        if (currentUserId.HasValue && currentUserId.Value != userId)
        {
            isFollowing = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == userId);
        }

        var fetchSize = pageSize * 2; // Fetch more to account for mixed posts and reposts

        var blockedUserIds = currentUserId.HasValue
            ? await _context.GetBlockedUserIdsAsync(currentUserId.Value)
            : new HashSet<int>();
        var followingIds = isFollowing ? new HashSet<int> { userId } : new HashSet<int>();

        // Get user's original posts
        var posts = await _context.GetPostsForFeed()
            .Where(p => p.UserId == userId) // Only posts from this user
            .ApplyVisibilityFilters(currentUserId, blockedUserIds, followingIds)
            .OrderByDescending(p => p.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Get user's reposts (using new unified system)
        var reposts = await _context.GetPostsWithIncludes()
            .Where(p => p.PostType == PostType.Repost &&
                       p.UserId == userId &&
                       !p.IsDeletedByUser &&
                       (!p.IsHidden ||
                        (p.HiddenReasonType == PostHiddenReasonType.VideoProcessing && p.UserId == currentUserId))) // Use hybrid system for filtering
            .Where(p => p.RepostedPost != null && // Ensure reposted post exists
                       !p.RepostedPost.IsDeletedByUser &&
                       (!p.RepostedPost.IsHidden ||
                        (p.RepostedPost.HiddenReasonType == PostHiddenReasonType.VideoProcessing && p.RepostedPost.UserId == currentUserId)) && // Use hybrid system for filtering
                       p.RepostedPost.User.Status == UserStatus.Active && // Hide reposts of posts from suspended/banned users
                       (p.RepostedPost.User.TrustScore >= 0.1f || p.RepostedPost.UserId == currentUserId) && // Hide reposts of low trust posts except from self
                       (p.RepostedPost.Privacy == PostPrivacy.Public || // Public posts can be seen
                        p.RepostedPost.UserId == currentUserId || // Current user's own posts
                        (p.RepostedPost.Privacy == PostPrivacy.Followers && currentUserId == userId))) // User viewing their own reposts
            .OrderByDescending(p => p.CreatedAt)
            .Take(fetchSize)
            .ToListAsync();

        // Note: Filtering is now handled in the main query above

        // Note: reposts with content are now included in the main reposts query above

        // Create timeline items and apply pagination
        return CreateTimelineItems(posts, reposts, currentUserId, page, pageSize);
    }

    public async Task<bool> DeletePostAsync(int postId, int userId)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

        if (post == null)
            return false;

        // Use hybrid hiding system: mark as hidden with DeletedByUser reason
        post.IsHidden = true;
        post.HiddenReasonType = PostHiddenReasonType.DeletedByUser;
        post.HiddenAt = DateTime.UtcNow;
        post.HiddenByUserId = userId; // User deleted their own post
        post.HiddenReason = "Post deleted by user";

        await _context.SaveChangesAsync();

        // Delete social interaction notifications for the deleted post
        // This preserves system/moderation notifications but removes likes, comments, reposts, mentions
        await _notificationService.DeleteSocialNotificationsForPostAsync(postId);

        return true;
    }

    public async Task<bool> LikePostAsync(int postId, int userId)
    {
        // Check trust-based permissions
        if (!await _trustBasedModerationService.CanPerformActionAsync(userId, TrustRequiredAction.LikeContent))
        {
            throw new InvalidOperationException("Insufficient trust score to like content");
        }

        // Check if already liked
        if (await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userId))
            return false;

        var like = new Like
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Likes.Add(like);
        await _context.SaveChangesAsync();

        // Invalidate post like count cache
        await _countCache.InvalidatePostCountsAsync(postId);

        // Get the post owner to create notification
        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
        {
            await _notificationService.CreateLikeNotificationAsync(post.UserId, userId, postId);
        }

        // Update trust score for like given
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                userId,
                TrustScoreAction.LikeGiven,
                "post",
                postId,
                "Liked a post"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for like by user {UserId}", userId);
            // Don't fail the like action if trust score update fails
        }

        return true;
    }

    public async Task<bool> UnlikePostAsync(int postId, int userId)
    {
        var like = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        
        if (like == null)
            return false;

        _context.Likes.Remove(like);
        await _context.SaveChangesAsync();

        // Invalidate post like count cache
        await _countCache.InvalidatePostCountsAsync(postId);

        return true;
    }

    public async Task<bool> LikeCommentAsync(int commentId, int userId)
    {
        // Legacy method - redirect to ReactToCommentAsync with Heart reaction
        return await ReactToCommentAsync(commentId, userId, ReactionType.Heart);
    }

    public async Task<bool> UnlikeCommentAsync(int commentId, int userId)
    {
        // Legacy method - redirect to RemoveCommentReactionAsync
        return await RemoveCommentReactionAsync(commentId, userId);
    }

    public async Task<bool> ReactToPostAsync(int postId, int userId, ReactionType reactionType)
    {
        // Check trust-based permissions
        if (!await _trustBasedModerationService.CanPerformActionAsync(userId, TrustRequiredAction.LikeContent))
        {
            throw new InvalidOperationException("Insufficient trust score to react to content");
        }

        // Check if user already has a reaction on this post
        var existingReaction = await _context.PostReactions
            .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

        if (existingReaction != null)
        {
            // If same reaction type, do nothing (already reacted)
            if (existingReaction.ReactionType == reactionType)
                return false;

            // Update existing reaction to new type
            existingReaction.ReactionType = reactionType;
            existingReaction.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new reaction
            var reaction = new PostReaction
            {
                PostId = postId,
                UserId = userId,
                ReactionType = reactionType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PostReactions.Add(reaction);
        }

        await _context.SaveChangesAsync();

        // Invalidate post reaction count cache
        await _countCache.InvalidatePostCountsAsync(postId);

        // Get the post owner to create notification
        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
        {
            await _notificationService.CreateReactionNotificationAsync(post.UserId, userId, postId, reactionType);
        }

        // Update trust score for reaction given
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                userId,
                TrustScoreAction.LikeGiven, // Reuse like action for now
                "post",
                postId,
                $"Reacted to a post with {reactionType.GetDisplayName()}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for reaction by user {UserId}", userId);
            // Don't fail the reaction action if trust score update fails
        }

        return true;
    }

    public async Task<bool> RemovePostReactionAsync(int postId, int userId)
    {
        var reaction = await _context.PostReactions
            .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId);

        if (reaction == null)
            return false;

        _context.PostReactions.Remove(reaction);
        await _context.SaveChangesAsync();

        // Invalidate post reaction count cache
        await _countCache.InvalidatePostCountsAsync(postId);

        return true;
    }

    public async Task<bool> ReactToCommentAsync(int commentId, int userId, ReactionType reactionType)
    {
        // Check trust-based permissions
        if (!await _trustBasedModerationService.CanPerformActionAsync(userId, TrustRequiredAction.LikeContent))
        {
            throw new InvalidOperationException("Insufficient trust score to react to content");
        }

        // Check if user already has a reaction on this comment (now a Post)
        var existingReaction = await _context.PostReactions
            .FirstOrDefaultAsync(r => r.PostId == commentId && r.UserId == userId);

        if (existingReaction != null)
        {
            // If same reaction type, do nothing (already reacted)
            if (existingReaction.ReactionType == reactionType)
                return false;

            // Update existing reaction to new type
            existingReaction.ReactionType = reactionType;
            existingReaction.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new reaction
            var reaction = new PostReaction
            {
                PostId = commentId,
                UserId = userId,
                ReactionType = reactionType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PostReactions.Add(reaction);
        }

        await _context.SaveChangesAsync();

        // Invalidate comment reaction count cache
        await _countCache.InvalidateCommentCountsAsync(commentId);

        // Get the comment owner to create notification
        var comment = await _context.Posts.FindAsync(commentId);
        if (comment != null)
        {
            await _notificationService.CreateCommentReactionNotificationAsync(comment.UserId, userId, comment.ParentId ?? 0, commentId, reactionType);
        }

        // Update trust score for reaction given
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                userId,
                TrustScoreAction.LikeGiven, // Reuse like action for now
                "comment",
                commentId,
                $"Reacted to a comment with {reactionType.GetDisplayName()}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for comment reaction by user {UserId}", userId);
            // Don't fail the reaction action if trust score update fails
        }

        // Track analytics for comment reaction
        try
        {
            await _analyticsService.TrackContentEngagementAsync(
                userId,
                ContentType.Comment,
                commentId,
                EngagementType.Like, // Reuse like engagement type for now
                comment?.UserId,
                "comment_reaction_button",
                $"User reacted to a comment with {reactionType.GetDisplayName()}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track analytics for comment reaction by user {UserId}", userId);
            // Don't fail the reaction action if analytics tracking fails
        }

        return true;
    }

    public async Task<bool> RemoveCommentReactionAsync(int commentId, int userId)
    {
        var reaction = await _context.PostReactions
            .FirstOrDefaultAsync(r => r.PostId == commentId && r.UserId == userId);

        if (reaction == null)
            return false;

        // Get comment info before removing the reaction for analytics
        var comment = await _context.Posts.FindAsync(commentId);

        _context.PostReactions.Remove(reaction);
        await _context.SaveChangesAsync();

        // Invalidate comment reaction count cache
        await _countCache.InvalidateCommentCountsAsync(commentId);

        // Track analytics for comment reaction removal
        try
        {
            await _analyticsService.TrackContentEngagementAsync(
                userId,
                ContentType.Comment,
                commentId,
                EngagementType.Unlike, // Reuse unlike engagement type for now
                comment?.UserId,
                "comment_reaction_button",
                "User removed reaction from a comment"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track analytics for comment reaction removal by user {UserId}", userId);
            // Don't fail the reaction removal action if analytics tracking fails
        }

        return true;
    }

    // Enhanced Repost functionality (replaces simple repost and quote tweet)
    public async Task<PostDto?> CreateRepostAsync(int userId, CreateRepostDto createDto)
    {
        _logger.LogInformation("Creating repost for user {UserId}, repostedPostId: {RepostedPostId}, content: '{Content}'",
            userId, createDto.RepostedPostId, createDto.Content);

        // Check trust-based permissions
        if (!await _trustBasedModerationService.CanPerformActionAsync(userId, TrustRequiredAction.CreatePost))
        {
            _logger.LogWarning("User {UserId} denied repost creation due to insufficient trust score", userId);
            throw new InvalidOperationException("Insufficient trust score to create reposts");
        }

        // Verify the reposted post exists and is accessible
        var repostedPost = await _context.Posts
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == createDto.RepostedPostId && !p.IsDeletedByUser);

        if (repostedPost == null)
        {
            _logger.LogWarning("Repost creation failed: reposted post {RepostedPostId} not found or deleted", createDto.RepostedPostId);
            throw new InvalidOperationException("Reposted post not found or has been deleted");
        }

        // Check if user can see the reposted post (privacy rules)
        if (!await CanUserSeePostAsync(repostedPost, userId))
        {
            throw new InvalidOperationException("You don't have permission to repost this post");
        }

        // Allow multiple reposts of the same post (e.g., for memories, sharing again, etc.)

        // Create repost as a Post with PostType.Repost and RepostedPostId
        var repost = new Post
        {
            Content = createDto.Content ?? string.Empty, // Allow empty content for simple reposts
            RepostedPostId = createDto.RepostedPostId,
            PostType = PostType.Repost,
            UserId = userId,
            Privacy = createDto.Privacy,
            GroupId = createDto.GroupId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(repost);
        await _context.SaveChangesAsync();

        // Process hashtags in the repost content (if any)
        if (!string.IsNullOrEmpty(createDto.Content))
        {
            await ProcessPostTagsAsync(repost.Id, createDto.Content);
        }

        // Perform content moderation analysis (if there's content)
        if (!string.IsNullOrEmpty(createDto.Content))
        {
            await ProcessContentModerationAsync(repost.Id, createDto.Content, userId);
        }

        // Invalidate reposted post's repost count cache
        await _countCache.InvalidatePostCountsAsync(createDto.RepostedPostId);

        // Create notification for the reposted post owner
        if (repostedPost.UserId != userId) // Don't notify if reposting own post
        {
            await _notificationService.CreateRepostNotificationAsync(repostedPost.UserId, userId, createDto.RepostedPostId);
        }

        // Create mention notifications for users mentioned in the repost content (if any)
        if (!string.IsNullOrWhiteSpace(createDto.Content))
        {
            await _notificationService.CreateMentionNotificationsAsync(createDto.Content, userId, repost.Id);
        }

        // Perform content moderation analysis (if there's content)
        if (!string.IsNullOrWhiteSpace(createDto.Content))
        {
            await ProcessContentModerationAsync(repost.Id, createDto.Content, userId);
        }

        // Update trust score for post creation
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                userId,
                TrustScoreAction.PostCreated,
                string.IsNullOrWhiteSpace(createDto.Content) ? "simple_repost" : "repost_with_content",
                repost.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update trust score for repost creation by user {UserId}", userId);
        }

        // Load the created repost with all necessary includes
        var createdRepost = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Group)
            .Include(p => p.RepostedPost)
                .ThenInclude(rp => rp!.User)
            .Include(p => p.Likes)
            .Include(p => p.Reactions)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment))

            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .Include(p => p.PostMedia)
            .AsSplitQuery()
            .FirstAsync(p => p.Id == repost.Id);

        return createdRepost.MapToPostDto(userId);
    }



    public async Task<PostDto?> CreateRepostWithMediaAsync(int userId, CreateRepostWithMediaDto createDto)
    {
        // Check trust-based permissions
        if (!await _trustBasedModerationService.CanPerformActionAsync(userId, TrustRequiredAction.CreatePost))
        {
            throw new InvalidOperationException("Insufficient trust score to create reposts");
        }

        // Verify the reposted post exists and is accessible
        var repostedPost = await _context.Posts
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == createDto.RepostedPostId && !p.IsDeletedByUser);

        if (repostedPost == null)
        {
            throw new InvalidOperationException("Reposted post not found or has been deleted");
        }

        // Check if user can see the reposted post (privacy rules)
        if (!await CanUserSeePostAsync(repostedPost, userId))
        {
            throw new InvalidOperationException("You don't have permission to repost this post");
        }

        // Allow multiple reposts of the same post (e.g., for memories, sharing again, etc.)

        // Validate media files
        if (createDto.MediaFiles != null && createDto.MediaFiles.Count > 10)
        {
            throw new InvalidOperationException("Cannot upload more than 10 media files per repost");
        }

        // Create repost as a Post with PostType.Repost and RepostedPostId
        var repost = new Post
        {
            Content = createDto.Content ?? string.Empty, // Allow empty content for simple reposts
            RepostedPostId = createDto.RepostedPostId,
            PostType = PostType.Repost,
            UserId = userId,
            Privacy = createDto.Privacy,
            GroupId = createDto.GroupId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(repost);
        await _context.SaveChangesAsync();

        // Handle media files if any
        if (createDto.MediaFiles != null && createDto.MediaFiles.Count > 0)
        {
            _logger.LogDebug("Processing {MediaCount} media files for repost {PostId}", createDto.MediaFiles.Count, repost.Id);
            foreach (var mediaFile in createDto.MediaFiles)
            {
                // Validate that non-GIF media types have a fileName
                if (mediaFile.MediaType != MediaType.Gif && string.IsNullOrEmpty(mediaFile.FileName))
                {
                    throw new InvalidOperationException($"FileName is required for {mediaFile.MediaType} media type");
                }

                // Validate that GIF media types have URLs
                if (mediaFile.MediaType == MediaType.Gif && (string.IsNullOrEmpty(mediaFile.GifUrl) || string.IsNullOrEmpty(mediaFile.GifPreviewUrl)))
                {
                    throw new InvalidOperationException("GifUrl and GifPreviewUrl are required for GIF media type");
                }
                var media = new PostMedia
                {
                    PostId = repost.Id,
                    MediaType = mediaFile.MediaType,
                    OriginalFileName = mediaFile.FileName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (mediaFile.MediaType == MediaType.Image)
                {
                    media.ImageFileName = mediaFile.FileName;
                    media.ImageWidth = mediaFile.Width;
                    media.ImageHeight = mediaFile.Height;
                    media.ImageFileSizeBytes = mediaFile.FileSizeBytes;
                }
                else if (mediaFile.MediaType == MediaType.Video)
                {
                    media.VideoFileName = mediaFile.FileName;
                    media.VideoProcessingStatus = VideoProcessingStatus.Pending;
                    media.OriginalVideoWidth = mediaFile.Width;
                    media.OriginalVideoHeight = mediaFile.Height;
                    media.OriginalVideoDuration = mediaFile.Duration;
                    media.OriginalVideoFileSizeBytes = mediaFile.FileSizeBytes;
                }
                else if (mediaFile.MediaType == MediaType.Gif)
                {
                    media.GifUrl = mediaFile.GifUrl;
                    media.GifPreviewUrl = mediaFile.GifPreviewUrl;
                    media.ImageWidth = mediaFile.Width;
                    media.ImageHeight = mediaFile.Height;
                }

                _context.PostMedia.Add(media);
            }

            await _context.SaveChangesAsync();
        }

        // Invalidate reposted post's repost count cache
        await _countCache.InvalidatePostCountsAsync(createDto.RepostedPostId);

        // Create notification for the reposted post owner
        if (repostedPost.UserId != userId) // Don't notify if reposting own post
        {
            await _notificationService.CreateRepostNotificationAsync(repostedPost.UserId, userId, createDto.RepostedPostId);
        }

        // Process hashtags in the repost content (if any)
        if (!string.IsNullOrWhiteSpace(createDto.Content))
        {
            await ProcessPostTagsAsync(repost.Id, createDto.Content);
        }

        // Create mention notifications for users mentioned in the repost content (if any)
        if (!string.IsNullOrWhiteSpace(createDto.Content))
        {
            await _notificationService.CreateMentionNotificationsAsync(createDto.Content, userId, repost.Id);
        }

        // Perform content moderation analysis (if there's content)
        if (!string.IsNullOrWhiteSpace(createDto.Content))
        {
            await ProcessContentModerationAsync(repost.Id, createDto.Content, userId);
        }

        // Update trust score for post creation
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                userId,
                TrustScoreAction.PostCreated,
                "repost_with_media",
                repost.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update trust score for repost with media creation by user {UserId}", userId);
        }

        // Load the created repost with all necessary includes
        var createdRepost = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Group)
            .Include(p => p.RepostedPost)
                .ThenInclude(rp => rp!.User)
            .Include(p => p.Likes)
            .Include(p => p.Reactions)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment))

            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.PostLinkPreviews)
                .ThenInclude(plp => plp.LinkPreview)
            .Include(p => p.PostMedia)
            .AsSplitQuery()
            .FirstAsync(p => p.Id == repost.Id);

        return createdRepost.MapToPostDto(userId);
    }







    public async Task<IEnumerable<PostDto>> GetRepostsAsync(int postId, int? currentUserId = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Posts
            .Include(p => p.User)
            .Include(p => p.Group)
            .Include(p => p.RepostedPost)
                .ThenInclude(rp => rp!.User)
            .Include(p => p.PostMedia)
            .Where(p => p.RepostedPostId == postId &&
                       p.PostType == PostType.Repost &&
                       !p.IsDeletedByUser &&
                       !p.IsHidden)
            .OrderByDescending(p => p.CreatedAt);

        var reposts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var repostDtos = new List<PostDto>();
        foreach (var repost in reposts)
        {
            // Check if current user can see this repost
            if (await CanUserSeePostAsync(repost, currentUserId))
            {
                var dto = repost.MapToPostDto(currentUserId);
                repostDtos.Add(dto);
            }
        }

        return repostDtos;
    }

    private async Task<bool> CanUserSeePostAsync(Post post, int? currentUserId)
    {
        // If post is deleted, no one can see it
        if (post.IsDeletedByUser)
            return false;

        // If post is hidden and not video processing, only author can see it
        if (post.IsHidden && post.HiddenReasonType != PostHiddenReasonType.VideoProcessing)
            return false;

        // If post is hidden for video processing, only author can see it
        if (post.IsHidden && post.HiddenReasonType == PostHiddenReasonType.VideoProcessing && post.UserId != currentUserId)
            return false;

        // Public posts can be seen by everyone
        if (post.Privacy == PostPrivacy.Public)
            return true;

        // Private posts can only be seen by the author
        if (post.Privacy == PostPrivacy.Private)
            return currentUserId == post.UserId;

        // Followers-only posts
        if (post.Privacy == PostPrivacy.Followers)
        {
            // Author can always see their own posts
            if (currentUserId == post.UserId)
                return true;

            // Check if current user follows the post author
            if (currentUserId.HasValue)
            {
                var isFollowing = await _context.Follows
                    .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == post.UserId);
                return isFollowing;
            }

            return false;
        }

        return false;
    }



    public async Task<CommentDto?> AddCommentAsync(int postId, int userId, CreateCommentDto createDto)
    {
        // Check trust-based permissions
        if (!await _trustBasedModerationService.CanPerformActionAsync(userId, TrustRequiredAction.CreateComment))
        {
            throw new InvalidOperationException("Insufficient trust score to create comments");
        }

        // Create comment as a Post with PostType.Comment and ParentId
        var comment = new Post
        {
            Content = createDto.Content,
            ParentId = postId,
            PostType = PostType.Comment,
            UserId = userId,
            Privacy = PostPrivacy.Public, // Comments inherit parent privacy
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(comment);
        await _context.SaveChangesAsync();

        // Invalidate post comment count cache
        await _countCache.InvalidatePostCountsAsync(postId);

        // Get the post owner to create comment notification
        var post = await _context.Posts.FindAsync(postId);
        if (post != null)
        {
            await _notificationService.CreateCommentNotificationAsync(post.UserId, userId, postId, comment.Id, createDto.Content);
        }

        // Create mention notifications for users mentioned in the comment
        await _notificationService.CreateMentionNotificationsAsync(createDto.Content, userId, postId, comment.Id);

        // Perform content moderation analysis on comment
        await ProcessCommentModerationAsync(comment.Id, createDto.Content, userId);

        // Update trust score for comment creation
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                userId,
                TrustScoreAction.CommentCreated,
                "comment",
                comment.Id,
                $"Created comment with {createDto.Content.Length} characters"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for comment creation by user {UserId}", userId);
            // Don't fail the comment creation if trust score update fails
        }

        // Check if comment should be auto-hidden based on trust score
        try
        {
            if (await _trustBasedModerationService.ShouldAutoHideContentAsync(userId, "comment"))
            {
                comment.IsHidden = true;
                comment.HiddenAt = DateTime.UtcNow;
                comment.HiddenReason = "Auto-hidden due to low trust score";
                comment.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Auto-hidden comment {CommentId} from low trust user {UserId}", comment.Id, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check auto-hide for comment {CommentId} by user {UserId}", comment.Id, userId);
            // Don't fail the comment creation if auto-hide check fails
        }

        // Load the comment with user data
        var createdComment = await _context.Posts
            .Include(c => c.User)
            .FirstAsync(c => c.Id == comment.Id);

        // For now, return basic comment info - we'll update cache methods later
        return MapPostToCommentDto(createdComment);
    }

    private CommentDto MapPostToCommentDto(Post post)
    {
        if (post.PostType != PostType.Comment)
            throw new ArgumentException("Post must be of type Comment", nameof(post));

        return new CommentDto(
            Id: post.Id,
            Content: post.Content,
            CreatedAt: post.CreatedAt,
            UpdatedAt: post.UpdatedAt,
            User: post.User.MapToUserDto(),
            IsEdited: post.UpdatedAt > post.CreatedAt.AddMinutes(1),
            LikeCount: 0, // Legacy - will be replaced by ReactionCounts
            IsLikedByCurrentUser: false, // Legacy - will be replaced by CurrentUserReaction
            ReactionCounts: post.Reactions?.GroupBy(r => r.ReactionType)
                .Select(g => new ReactionCountDto(g.Key, g.Key.GetEmoji(), g.Key.GetDisplayName(), g.Count())),
            CurrentUserReaction: null, // Will be set by caller if needed
            TotalReactionCount: post.Reactions?.Count ?? 0
        );
    }

    public async Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId)
    {
        var comments = await _context.Posts
            .Include(c => c.User)
            .Where(c => c.ParentId == postId && c.PostType == PostType.Comment &&
                       !c.IsDeletedByUser && !c.IsHidden) // Filter out user-deleted and moderator-hidden comments
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        // Map comments without like information (for unauthenticated users)
        return comments.Select(c => MapPostToCommentDto(c));
    }

    public async Task<IEnumerable<CommentDto>> GetPostCommentsAsync(int postId, int currentUserId)
    {
        var comments = await _context.Posts
            .Include(c => c.User)
            .Include(c => c.Reactions) // Post reactions (includes comment reactions)
            .Where(c => c.ParentId == postId && c.PostType == PostType.Comment &&
                       !c.IsDeletedByUser && !c.IsHidden) // Filter out user-deleted and moderator-hidden comments
            .OrderBy(c => c.CreatedAt)
            .AsSplitQuery()
            .ToListAsync();

        // Filter out comments from blocked users if user is authenticated
        var blockedUserIds = await GetBlockedUserIdsAsync(currentUserId);
        comments = comments.Where(c => !blockedUserIds.Contains(c.UserId)).ToList();

        // Map comments with reaction information
        var commentDtos = comments.Select(comment =>
            MapPostToCommentDtoWithReactions(comment, currentUserId)
        ).ToList();

        return commentDtos;
    }

    private CommentDto MapPostToCommentDtoWithReactions(Post post, int currentUserId)
    {
        if (post.PostType != PostType.Comment)
            throw new ArgumentException("Post must be of type Comment", nameof(post));

        var currentUserReaction = post.Reactions?.FirstOrDefault(r => r.UserId == currentUserId)?.ReactionType;
        var reactionCounts = post.Reactions?.GroupBy(r => r.ReactionType)
            .Select(g => new ReactionCountDto(g.Key, g.Key.GetEmoji(), g.Key.GetDisplayName(), g.Count()));

        return new CommentDto(
            Id: post.Id,
            Content: post.Content,
            CreatedAt: post.CreatedAt,
            UpdatedAt: post.UpdatedAt,
            User: post.User.MapToUserDto(),
            IsEdited: post.UpdatedAt > post.CreatedAt.AddMinutes(1),
            LikeCount: 0, // Legacy - will be replaced by ReactionCounts
            IsLikedByCurrentUser: false, // Legacy - will be replaced by CurrentUserReaction
            ReactionCounts: reactionCounts,
            CurrentUserReaction: currentUserReaction,
            TotalReactionCount: post.Reactions?.Count ?? 0
        );
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _context.Posts.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId && c.PostType == PostType.Comment);

        if (comment == null)
            return false;

        // Soft delete: mark as deleted by user instead of removing from database
        comment.IsDeletedByUser = true;
        comment.DeletedByUserAt = DateTime.UtcNow;
        comment.HiddenReasonType = PostHiddenReasonType.DeletedByUser;

        await _context.SaveChangesAsync();
        return true;
    }

    // Removed MapToPostDto method - now using extension method from MappingUtilities

    // Removed MapToCommentDto method - now using extension method from MappingUtilities

    public async Task<PostDto?> UpdatePostAsync(int postId, int userId, UpdatePostDto updateDto)
    {
        LogOperation(nameof(UpdatePostAsync), new { postId, userId });

        var post = await _context.GetPostsWithIncludes()
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
            return null;

        ValidateUserAuthorization(userId, post.UserId, "update this post");

        post.Content = updateDto.Content;
        post.Privacy = updateDto.Privacy;
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Update hashtags for the post
        await UpdatePostTagsAsync(post.Id, updateDto.Content);

        // Update link previews for the post
        await UpdatePostLinkPreviewsAsync(post.Id, updateDto.Content);

        // Create mention notifications for new mentions in the updated post
        await _notificationService.CreateMentionNotificationsAsync(updateDto.Content, userId, post.Id);

        // Perform content moderation analysis on updated content
        await ProcessContentModerationAsync(post.Id, updateDto.Content, userId);

        return post.MapToPostDto(userId);
    }

    public async Task<CommentDto?> UpdateCommentAsync(int commentId, int userId, UpdateCommentDto updateDto)
    {
        var comment = await _context.Posts
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.PostType == PostType.Comment);

        if (comment == null || comment.UserId != userId)
            return null;

        comment.Content = updateDto.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Create mention notifications for new mentions in the updated comment
        await _notificationService.CreateMentionNotificationsAsync(updateDto.Content, userId, comment.ParentId ?? 0, comment.Id);

        // Perform content moderation analysis on updated comment
        await ProcessCommentModerationAsync(comment.Id, updateDto.Content, userId);

        return MapPostToCommentDto(comment);
    }

    private new async Task<List<int>> GetBlockedUserIdsAsync(int userId)
    {
        // Get users that the current user has blocked
        var blockedByUser = await _context.Blocks
            .Where(b => b.BlockerId == userId)
            .Select(b => b.BlockedId)
            .ToListAsync();

        // Get users that have blocked the current user
        var blockedByOthers = await _context.Blocks
            .Where(b => b.BlockedId == userId)
            .Select(b => b.BlockerId)
            .ToListAsync();

        // Combine both lists to filter out all blocked relationships
        return blockedByUser.Concat(blockedByOthers).Distinct().ToList();
    }

    private async Task ProcessPostTagsAsync(int postId, string? content)
    {
        if (content == null) return;
        // Extract hashtags from the content
        var tagNames = TagParser.ExtractTags(content);

        if (!tagNames.Any())
            return;

        // Get or create tags
        var existingTags = await _context.Tags
            .Where(t => tagNames.Contains(t.Name))
            .ToListAsync();

        var existingTagNames = existingTags.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newTagNames = tagNames.Where(name => !existingTagNames.Contains(name)).ToList();

        // Create new tags
        var newTags = newTagNames.Select(name => new Tag
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            PostCount = 1
        }).ToList();

        if (newTags.Any())
        {
            _context.Tags.AddRange(newTags);
            await _context.SaveChangesAsync();
        }

        // Update post count for existing tags
        foreach (var existingTag in existingTags)
        {
            existingTag.PostCount++;
        }

        // Combine all tags (existing + new)
        var allTags = existingTags.Concat(newTags).ToList();

        // Create PostTag relationships
        var postTags = allTags.Select(tag => new PostTag
        {
            PostId = postId,
            TagId = tag.Id,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.PostTags.AddRange(postTags);
        await _context.SaveChangesAsync();

        // Invalidate tag count caches for all affected tags
        foreach (var tag in allTags)
        {
            await _countCache.InvalidateTagCountsAsync(tag.Name);
        }
    }

    private async Task ProcessPostLinkPreviewsAsync(int postId, string? content)
    {
        if (content == null) return;
        try
        {
            // Extract and process link previews
            var linkPreviews = (await _linkPreviewService.ProcessPostLinksAsync(content)).ToList();

            if (!linkPreviews.Any())
                return;

            // Create PostLinkPreview relationships
            var postLinkPreviews = linkPreviews.Select(lp => new PostLinkPreview
            {
                PostId = postId,
                LinkPreviewId = lp.Id,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.PostLinkPreviews.AddRange(postLinkPreviews);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't fail post creation if link preview processing fails
            // This could be logged to a proper logging system
            Console.WriteLine($"Failed to process link previews for post {postId}: {ex.Message}");
        }
    }

    private async Task UpdatePostTagsAsync(int postId, string content)
    {
        // Get current tags for the post
        var currentPostTags = await _context.PostTags
            .Include(pt => pt.Tag)
            .Where(pt => pt.PostId == postId)
            .ToListAsync();

        var currentTagNames = currentPostTags.Select(pt => pt.Tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Extract new tags from content
        var newTagNames = TagParser.ExtractTags(content).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Find tags to remove and add
        var tagsToRemove = currentPostTags.Where(pt => !newTagNames.Contains(pt.Tag.Name)).ToList();
        var tagNamesToAdd = newTagNames.Where(name => !currentTagNames.Contains(name)).ToList();

        // Remove old tags
        if (tagsToRemove.Any())
        {
            _context.PostTags.RemoveRange(tagsToRemove);

            // Decrease post count for removed tags
            foreach (var removedPostTag in tagsToRemove)
            {
                removedPostTag.Tag.PostCount = Math.Max(0, removedPostTag.Tag.PostCount - 1);
            }
        }

        // Add new tags
        if (tagNamesToAdd.Any())
        {
            // Get or create new tags
            var existingTags = await _context.Tags
                .Where(t => tagNamesToAdd.Contains(t.Name))
                .ToListAsync();

            var existingTagNames = existingTags.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var newTagNamesToCreate = tagNamesToAdd.Where(name => !existingTagNames.Contains(name)).ToList();

            // Create new tags
            var newTags = newTagNamesToCreate.Select(name => new Tag
            {
                Name = name,
                CreatedAt = DateTime.UtcNow,
                PostCount = 1
            }).ToList();

            if (newTags.Any())
            {
                _context.Tags.AddRange(newTags);
                await _context.SaveChangesAsync();
            }

            // Update post count for existing tags
            foreach (var existingTag in existingTags)
            {
                existingTag.PostCount++;
            }

            // Combine all tags (existing + new)
            var allTags = existingTags.Concat(newTags).ToList();

            // Create PostTag relationships
            var newPostTags = allTags.Select(tag => new PostTag
            {
                PostId = postId,
                TagId = tag.Id,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.PostTags.AddRange(newPostTags);
        }

        await _context.SaveChangesAsync();
    }

    private async Task UpdatePostLinkPreviewsAsync(int postId, string content)
    {
        try
        {
            // Get current link previews for the post
            var currentPostLinkPreviews = await _context.PostLinkPreviews
                .Include(plp => plp.LinkPreview)
                .Where(plp => plp.PostId == postId)
                .ToListAsync();

            // Remove all existing link preview relationships for this post
            if (currentPostLinkPreviews.Any())
            {
                _context.PostLinkPreviews.RemoveRange(currentPostLinkPreviews);
                await _context.SaveChangesAsync();
            }

            // Process new link previews
            var linkPreviews = (await _linkPreviewService.ProcessPostLinksAsync(content)).ToList();

            if (linkPreviews.Any())
            {
                // Create new PostLinkPreview relationships
                var newPostLinkPreviews = linkPreviews.Select(lp => new PostLinkPreview
                {
                    PostId = postId,
                    LinkPreviewId = lp.Id,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.PostLinkPreviews.AddRange(newPostLinkPreviews);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail post update if link preview processing fails
            Console.WriteLine($"Failed to update link previews for post {postId}: {ex.Message}");
        }
    }

    private async Task ProcessContentModerationAsync(int postId, string? content, int userId)
    {
        if (content == null) return;
        try
        {
            var moderationEnabled = _configuration.GetValue("ContentModeration:Enabled", true);
            if (!moderationEnabled)
                return;

            // Check if the content moderation service is available
            var isServiceAvailable = await _contentModerationService.IsServiceAvailableAsync();
            if (!isServiceAvailable)
            {
                _logger.LogWarning("Content moderation service is not available for post {PostId}", postId);
                return;
            }

            // Analyze the content
            var moderationResult = await _contentModerationService.AnalyzeContentAsync(content);

            // Log the analysis result
            _logger.LogInformation("Content moderation analysis for post {PostId}: Risk Level {RiskLevel}, Requires Review: {RequiresReview}",
                postId, moderationResult.RiskAssessment.Level, moderationResult.RequiresReview);

            // Apply suggested tags if any were found
            if (moderationResult.SuggestedTags.Any())
            {
                await _contentModerationService.ApplySuggestedTagsToPostAsync(postId, moderationResult, userId);
            }

            // Auto-hide content if risk is very high
            var autoHideThreshold = _configuration.GetValue("ContentModeration:AutoHideThreshold", 0.8);
            if (moderationResult.RiskAssessment.Score >= autoHideThreshold)
            {
                var post = await _context.Posts.FindAsync(postId);
                if (post != null)
                {
                    post.IsHidden = true;
                    post.HiddenReasonType = PostHiddenReasonType.ContentModerationHidden;
                    post.HiddenReason = $"Auto-hidden due to high risk content (Score: {moderationResult.RiskAssessment.Score:F2})";
                    post.HiddenByUserId = userId; // System user ID would be better
                    post.HiddenAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Auto-hidden post {PostId} due to high risk score {RiskScore}",
                        postId, moderationResult.RiskAssessment.Score);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing content moderation for post {PostId}", postId);
            // Don't fail post creation/update if moderation fails
        }
    }

    private async Task ProcessCommentModerationAsync(int commentId, string content, int userId)
    {
        try
        {
            var moderationEnabled = _configuration.GetValue("ContentModeration:Enabled", true);
            if (!moderationEnabled)
                return;

            // Check if the content moderation service is available
            var isServiceAvailable = await _contentModerationService.IsServiceAvailableAsync();
            if (!isServiceAvailable)
            {
                _logger.LogWarning("Content moderation service is not available for comment {CommentId}", commentId);
                return;
            }

            // Analyze the content
            var moderationResult = await _contentModerationService.AnalyzeContentAsync(content);

            // Log the analysis result
            _logger.LogInformation("Content moderation analysis for comment {CommentId}: Risk Level {RiskLevel}, Requires Review: {RequiresReview}",
                commentId, moderationResult.RiskAssessment.Level, moderationResult.RequiresReview);

            // Apply suggested tags if any were found
            if (moderationResult.SuggestedTags.Any())
            {
                await _contentModerationService.ApplySuggestedTagsToCommentAsync(commentId, moderationResult, userId);
            }

            // Auto-hide content if risk is very high
            var autoHideThreshold = _configuration.GetValue("ContentModeration:AutoHideThreshold", 0.8);
            if (moderationResult.RiskAssessment.Score >= autoHideThreshold)
            {
                var comment = await _context.Posts.FindAsync(commentId);
                if (comment != null && comment.PostType == PostType.Comment)
                {
                    comment.IsHidden = true;
                    comment.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
                    comment.HiddenByUserId = userId; // System user ID would be better
                    comment.HiddenAt = DateTime.UtcNow;
                    comment.HiddenReason = $"Auto-hidden due to high risk content (Score: {moderationResult.RiskAssessment.Score:F2})";
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Auto-hidden comment {CommentId} due to high risk score {RiskScore}",
                        commentId, moderationResult.RiskAssessment.Score);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing content moderation for comment {CommentId}", commentId);
            // Don't fail comment creation/update if moderation fails
        }
    }

    private async Task ProcessVideoAsync(int postId, int userId, string videoFileName, string? postContent)
    {
        try
        {
            _logger.LogInformation("Publishing video processing request for Post {PostId}, Video: {VideoFileName}", postId, videoFileName);

            // Construct the full path to the uploaded video
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "videos");
            var videoPath = Path.Combine(uploadsPath, videoFileName);

            // Update video media status to processing
            var post = await _context.Posts
                .Include(p => p.PostMedia)
                .FirstOrDefaultAsync(p => p.Id == postId);
            if (post != null)
            {
                var videoMedia = post.PostMedia.FirstOrDefault(m => m.MediaType == MediaType.Video);
                if (videoMedia != null)
                {
                    videoMedia.VideoProcessingStatus = VideoProcessingStatus.Processing;
                    videoMedia.VideoProcessingStartedAt = DateTime.UtcNow;
                    videoMedia.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // Publish video processing request to RabbitMQ
            _logger.LogDebug("Preparing video processing request for Post {PostId}, Video: {VideoFileName}", postId, videoFileName);
            var videoProcessingRequest = new VideoProcessingRequest
            {
                PostId = postId,
                UserId = userId,
                OriginalVideoFileName = videoFileName,
                OriginalVideoPath = videoPath,
                RequestedAt = DateTime.UtcNow,
                PostContent = postContent
            };

            _logger.LogDebug("Publishing video processing request to RabbitMQ for Post {PostId}", postId);

            // Add timeout to prevent hanging on RabbitMQ issues (like disk space alarms)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await _publishEndpoint.Publish(videoProcessingRequest, cts.Token);
                _logger.LogDebug("Video processing request published successfully to RabbitMQ for Post {PostId}", postId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("RabbitMQ publish timeout for Post {PostId} - likely RabbitMQ disk space alarm", postId);
                throw new InvalidOperationException("Video processing service is temporarily unavailable. Please try again later.");
            }

            _logger.LogInformation("Video processing request published successfully for Post {PostId}", postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing video processing request for Post {PostId}", postId);

            // Update video media status to failed
            try
            {
                var post = await _context.Posts
                    .Include(p => p.PostMedia)
                    .FirstOrDefaultAsync(p => p.Id == postId);
                if (post != null)
                {
                    var videoMedia = post.PostMedia.FirstOrDefault(m => m.MediaType == MediaType.Video);
                    if (videoMedia != null)
                    {
                        videoMedia.VideoProcessingStatus = VideoProcessingStatus.Failed;
                        videoMedia.VideoProcessingError = $"Failed to publish processing request: {ex.Message}";
                        videoMedia.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Error updating post status to failed for Post {PostId}", postId);
            }
        }
    }

    /// <summary>
    /// Determine media type based on file extension
    /// </summary>
    private MediaType DetermineMediaTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };

        if (!string.IsNullOrEmpty(extension))
        {
            if (imageExtensions.Contains(extension))
                return MediaType.Image;

            if (videoExtensions.Contains(extension))
                return MediaType.Video;
        }

        // Default to image if unknown
        return MediaType.Image;
    }
}
