using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Serilog.Context;
using Yapplr.Api.Common;

namespace Yapplr.Api.Services;

public class AdminService : IAdminService
{
    private readonly YapplrDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ITrustScoreService _trustScoreService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminService> _logger;

    public AdminService(YapplrDbContext context, IAuditService auditService, INotificationService notificationService, ITrustScoreService trustScoreService, IServiceProvider serviceProvider, ILogger<AdminService> logger)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
        _trustScoreService = trustScoreService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // System Tags
    public async Task<IEnumerable<SystemTagDto>> GetSystemTagsAsync(SystemTagCategory? category = null, bool? isActive = null)
    {
        var query = _context.SystemTags.AsQueryable();

        if (category.HasValue)
            query = query.Where(st => st.Category == category.Value);

        if (isActive.HasValue)
            query = query.Where(st => st.IsActive == isActive.Value);

        var tags = await query
            .OrderBy(st => st.SortOrder)
            .ThenBy(st => st.Name)
            .ToListAsync();

        return tags.Select(MappingUtilities.MapToSystemTagDto);
    }

    public async Task<SystemTagDto?> GetSystemTagAsync(int id)
    {
        var tag = await _context.SystemTags.FindAsync(id);
        return tag == null ? null : tag.MapToSystemTagDto();
    }

    public async Task<SystemTagDto> CreateSystemTagAsync(CreateSystemTagDto createDto)
    {
        var systemTag = new SystemTag
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Category = createDto.Category,
            IsVisibleToUsers = createDto.IsVisibleToUsers,
            Color = createDto.Color,
            Icon = createDto.Icon,
            SortOrder = createDto.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SystemTags.Add(systemTag);
        await _context.SaveChangesAsync();

        return systemTag.MapToSystemTagDto();
    }

    public async Task<SystemTagDto?> UpdateSystemTagAsync(int id, UpdateSystemTagDto updateDto)
    {
        var systemTag = await _context.SystemTags.FindAsync(id);
        if (systemTag == null) return null;

        if (updateDto.Name != null) systemTag.Name = updateDto.Name;
        if (updateDto.Description != null) systemTag.Description = updateDto.Description;
        if (updateDto.Category.HasValue) systemTag.Category = updateDto.Category.Value;
        if (updateDto.IsVisibleToUsers.HasValue) systemTag.IsVisibleToUsers = updateDto.IsVisibleToUsers.Value;
        if (updateDto.IsActive.HasValue) systemTag.IsActive = updateDto.IsActive.Value;
        if (updateDto.Color != null) systemTag.Color = updateDto.Color;
        if (updateDto.Icon != null) systemTag.Icon = updateDto.Icon;
        if (updateDto.SortOrder.HasValue) systemTag.SortOrder = updateDto.SortOrder.Value;

        systemTag.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return systemTag.MapToSystemTagDto();
    }

    public async Task<bool> DeleteSystemTagAsync(int id)
    {
        var systemTag = await _context.SystemTags.FindAsync(id);
        if (systemTag == null) return false;

        _context.SystemTags.Remove(systemTag);
        await _context.SaveChangesAsync();
        return true;
    }

    // Content Moderation - includes ALL posts (both public and group posts) for admin moderation
    public async Task<IEnumerable<AdminPostDto>> GetPostsForModerationAsync(int page = 1, int pageSize = 25, bool? isHidden = null)
    {
        var query = _context.Posts
            .Where(p => p.PostType == PostType.Post) // Only top-level posts, exclude comments
            .Include(p => p.User)
            .Include(p => p.Group) // Include group information for group posts
            .Include(p => p.HiddenByUser)
            .Include(p => p.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .AsQueryable();

        if (isHidden.HasValue)
            query = query.Where(p => p.IsHidden == isHidden.Value);

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get all AI suggested tags for the posts in a single query to avoid concurrency issues
        var postIds = posts.Select(p => p.Id).ToList();
        var aiSuggestedTagsLookup = await _context.AiSuggestedTags
            .Include(ast => ast.ApprovedByUser)
            .Where(ast => ast.PostId.HasValue && postIds.Contains(ast.PostId.Value))
            .OrderByDescending(ast => ast.SuggestedAt)
            .ToListAsync();

        var aiSuggestedTagsByPostId = aiSuggestedTagsLookup
            .Where(ast => ast.PostId.HasValue)
            .GroupBy(ast => ast.PostId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return posts.Select(p => p.MapToAdminPostDtoWithTags(aiSuggestedTagsByPostId.GetValueOrDefault(p.Id, new List<AiSuggestedTag>()))).ToList();
    }

    // Comment Moderation - includes ALL comments (both public and group comments) for admin moderation
    public async Task<IEnumerable<AdminCommentDto>> GetCommentsForModerationAsync(int page = 1, int pageSize = 25, bool? isHidden = null)
    {
        var query = _context.Posts
            .Include(c => c.User)
            .Include(c => c.Group) // Include group information for group comments
            .Include(c => c.HiddenByUser)
            .Include(c => c.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .Where(c => c.PostType == PostType.Comment)
            .AsQueryable();

        if (isHidden.HasValue)
            query = query.Where(c => c.IsHidden == isHidden.Value);

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return comments.Select(MappingUtilities.MapPostToAdminCommentDto);
    }

    public async Task<AdminPostDto?> GetPostForModerationAsync(int postId)
    {
        var post = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Group) // Include group information for group posts
            .Include(p => p.HiddenByUser)
            .Include(p => p.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .Include(p => p.Likes)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment))
            .Include(p => p.Reposts)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null) return null;

        // Get AI suggested tags for this post
        var aiSuggestedTags = await _context.AiSuggestedTags
            .Include(ast => ast.ApprovedByUser)
            .Where(ast => ast.PostId == post.Id)
            .OrderByDescending(ast => ast.SuggestedAt)
            .ToListAsync();

        return post.MapToAdminPostDtoWithTags(aiSuggestedTags);
    }

    public async Task<AdminCommentDto?> GetCommentForModerationAsync(int commentId)
    {
        var comment = await _context.Posts
            .Include(c => c.User)
            .Include(c => c.Group) // Include group information for group comments
            .Include(c => c.HiddenByUser)
            .Include(c => c.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.PostType == PostType.Comment);

        return comment == null ? null : comment.MapPostToAdminCommentDto();
    }

    public async Task<bool> HidePostAsync(int postId, int hiddenByUserId, string reason)
    {
        using var operationScope = LogContext.PushProperty("Operation", "HidePost");
        using var postScope = LogContext.PushProperty("PostId", postId);
        using var moderatorScope = LogContext.PushProperty("ModeratorId", hiddenByUserId);
        using var reasonScope = LogContext.PushProperty("HideReason", reason);

        _logger.LogInformation("Starting post hide operation for post {PostId} by moderator {ModeratorId}",
            postId, hiddenByUserId);

        var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null)
        {
            _logger.LogWarning("Hide post failed: Post {PostId} not found", postId);
            return false;
        }

        using var postOwnerScope = LogContext.PushProperty("PostOwnerId", post.UserId);

        var moderator = await _context.Users.FindAsync(hiddenByUserId);
        if (moderator == null)
        {
            _logger.LogWarning("Hide post failed: Moderator {ModeratorId} not found", hiddenByUserId);
            return false;
        }

        using var moderatorUsernameScope = LogContext.PushProperty("ModeratorUsername", moderator.Username);

        post.IsHidden = true;
        post.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
        post.HiddenReason = reason;
        post.HiddenByUserId = hiddenByUserId;
        post.HiddenAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Post {PostId} hidden successfully by moderator {ModeratorUsername} ({ModeratorId})",
            postId, moderator.Username, hiddenByUserId);

        await _auditService.LogPostHiddenAsync(postId, hiddenByUserId, reason);

        // Send notification to the post owner
        await _notificationService.CreateContentHiddenNotificationAsync(post.UserId, "post", postId, reason, moderator.Username);

        _logger.LogInformation("Content hidden notification sent to post owner {PostOwnerId}", post.UserId);

        // Update trust score for content being hidden (negative impact)
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                post.UserId,
                TrustScoreAction.ContentHidden,
                "post",
                postId,
                $"Post hidden by moderator: {reason}"
            );
            _logger.LogInformation("Trust score updated for user {PostOwnerId} due to hidden post", post.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for hidden post by user {PostOwnerId}", post.UserId);
            // Don't fail the moderation action if trust score update fails
        }

        return true;
    }

    public async Task<bool> UnhidePostAsync(int postId)
    {
        var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null) return false;

        post.IsHidden = false;
        post.HiddenReasonType = PostHiddenReasonType.None;
        post.HiddenReason = null;
        post.HiddenByUserId = null;
        post.HiddenAt = null;

        await _context.SaveChangesAsync();

        // Send notification to the post owner
        await _notificationService.CreateContentRestoredNotificationAsync(post.UserId, "post", postId, "System");

        return true;
    }



    public async Task<bool> HideCommentAsync(int commentId, int hiddenByUserId, string reason)
    {
        var comment = await _context.Posts.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == commentId && c.PostType == PostType.Comment);
        if (comment == null) return false;

        var moderator = await _context.Users.FindAsync(hiddenByUserId);
        if (moderator == null) return false;

        comment.IsHidden = true;
        comment.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
        comment.HiddenByUserId = hiddenByUserId;
        comment.HiddenAt = DateTime.UtcNow;
        comment.HiddenReason = reason;

        await _context.SaveChangesAsync();
        await _auditService.LogCommentHiddenAsync(commentId, hiddenByUserId, reason);

        // Send notification to the comment owner
        await _notificationService.CreateContentHiddenNotificationAsync(comment.UserId, "comment", commentId, reason, moderator.Username);

        // Update trust score for content being hidden (negative impact)
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                comment.UserId,
                TrustScoreAction.ContentHidden,
                "comment",
                commentId,
                $"Comment hidden by moderator: {reason}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for hidden comment by user {UserId}", comment.UserId);
            // Don't fail the moderation action if trust score update fails
        }

        return true;
    }

    public async Task<bool> UnhideCommentAsync(int commentId)
    {
        var comment = await _context.Posts.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == commentId && c.PostType == PostType.Comment);
        if (comment == null) return false;

        comment.IsHidden = false;
        comment.HiddenReasonType = PostHiddenReasonType.None;
        comment.HiddenByUserId = null;
        comment.HiddenAt = null;
        comment.HiddenReason = null;

        await _context.SaveChangesAsync();

        // Send notification to the comment owner
        await _notificationService.CreateContentRestoredNotificationAsync(comment.UserId, "comment", commentId, "System");

        return true;
    }



    public async Task<bool> ApplySystemTagToPostAsync(int postId, int systemTagId, int appliedByUserId, string? reason = null)
    {
        // Check if tag is already applied
        var existing = await _context.PostSystemTags
            .FirstOrDefaultAsync(pst => pst.PostId == postId && pst.SystemTagId == systemTagId);

        if (existing != null) return false;

        var postSystemTag = new PostSystemTag
        {
            PostId = postId,
            SystemTagId = systemTagId,
            AppliedByUserId = appliedByUserId,
            Reason = reason,
            AppliedAt = DateTime.UtcNow
        };

        _context.PostSystemTags.Add(postSystemTag);
        await _context.SaveChangesAsync();
        await _auditService.LogPostSystemTagAddedAsync(postId, systemTagId, appliedByUserId, reason);
        return true;
    }

    public async Task<bool> RemoveSystemTagFromPostAsync(int postId, int systemTagId, int removedByUserId)
    {
        var postSystemTag = await _context.PostSystemTags
            .FirstOrDefaultAsync(pst => pst.PostId == postId && pst.SystemTagId == systemTagId);

        if (postSystemTag == null) return false;

        _context.PostSystemTags.Remove(postSystemTag);
        await _context.SaveChangesAsync();
        await _auditService.LogPostSystemTagRemovedAsync(postId, systemTagId, removedByUserId);
        return true;
    }

    public async Task<bool> ApplySystemTagToCommentAsync(int commentId, int systemTagId, int appliedByUserId, string? reason = null)
    {
        // Check if tag is already applied
        var existing = await _context.PostSystemTags
            .FirstOrDefaultAsync(pst => pst.PostId == commentId && pst.SystemTagId == systemTagId);

        if (existing != null) return false;

        var postSystemTag = new PostSystemTag
        {
            PostId = commentId,
            SystemTagId = systemTagId,
            AppliedByUserId = appliedByUserId,
            Reason = reason,
            AppliedAt = DateTime.UtcNow
        };

        _context.PostSystemTags.Add(postSystemTag);
        await _context.SaveChangesAsync();
        await _auditService.LogCommentSystemTagAddedAsync(commentId, systemTagId, appliedByUserId, reason);
        return true;
    }

    public async Task<bool> RemoveSystemTagFromCommentAsync(int commentId, int systemTagId, int removedByUserId)
    {
        var postSystemTag = await _context.PostSystemTags
            .FirstOrDefaultAsync(pst => pst.PostId == commentId && pst.SystemTagId == systemTagId);

        if (postSystemTag == null) return false;

        _context.PostSystemTags.Remove(postSystemTag);
        await _context.SaveChangesAsync();
        await _auditService.LogCommentSystemTagRemovedAsync(commentId, systemTagId, removedByUserId);
        return true;
    }

    // Bulk Actions
    public async Task<int> BulkHidePostsAsync(IEnumerable<int> postIds, int hiddenByUserId, string reason)
    {
        var posts = await _context.Posts
            .Where(p => postIds.Contains(p.Id) && !p.IsHidden)
            .ToListAsync();

        foreach (var post in posts)
        {
            post.IsHidden = true;
            post.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
            post.HiddenReason = reason;
            post.HiddenByUserId = hiddenByUserId;
            post.HiddenAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogBulkContentHiddenAsync(postIds, hiddenByUserId, reason);
        return posts.Count;
    }



    public async Task<int> BulkApplySystemTagAsync(IEnumerable<int> postIds, int systemTagId, int appliedByUserId, string? reason = null)
    {
        var existingTags = await _context.PostSystemTags
            .Where(pst => postIds.Contains(pst.PostId) && pst.SystemTagId == systemTagId)
            .Select(pst => pst.PostId)
            .ToListAsync();

        var newPostIds = postIds.Except(existingTags);
        var newTags = newPostIds.Select(postId => new PostSystemTag
        {
            PostId = postId,
            SystemTagId = systemTagId,
            AppliedByUserId = appliedByUserId,
            Reason = reason,
            AppliedAt = DateTime.UtcNow
        }).ToList();

        _context.PostSystemTags.AddRange(newTags);
        await _context.SaveChangesAsync();
        return newTags.Count;
    }

    // Analytics and Reporting
    public async Task<ModerationStatsDto> GetModerationStatsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active);
        var suspendedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Suspended);
        var bannedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Banned);
        var shadowBannedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.ShadowBanned);

        var totalPosts = await _context.Posts.CountAsync(p => p.PostType == PostType.Post);
        var hiddenPosts = await _context.Posts.CountAsync(p => p.PostType == PostType.Post && p.IsHidden);
        var totalComments = await _context.Posts.CountAsync(p => p.PostType == PostType.Comment);
        var hiddenComments = await _context.Posts.CountAsync(p => p.PostType == PostType.Comment && p.IsHidden);

        var pendingAppeals = await _context.UserAppeals.CountAsync(ua => ua.Status == AppealStatus.Pending);

        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);

        var todayActions = await _context.AuditLogs.CountAsync(al => al.CreatedAt >= today);
        var weekActions = await _context.AuditLogs.CountAsync(al => al.CreatedAt >= weekAgo);
        var monthActions = await _context.AuditLogs.CountAsync(al => al.CreatedAt >= monthAgo);

        return new ModerationStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            SuspendedUsers = suspendedUsers,
            BannedUsers = bannedUsers,
            ShadowBannedUsers = shadowBannedUsers,
            TotalPosts = totalPosts,
            HiddenPosts = hiddenPosts,
            TotalComments = totalComments,
            HiddenComments = hiddenComments,
            PendingAppeals = pendingAppeals,
            TodayActions = todayActions,
            WeekActions = weekActions,
            MonthActions = monthActions
        };
    }

    public async Task<ContentQueueDto> GetContentQueueAsync()
    {
        // Get flagged posts (posts with system tags indicating violations) - includes both public and group posts
        var flaggedPosts = await _context.Posts
            .Where(p => p.PostType == PostType.Post) // Only top-level posts, exclude comments
            .Include(p => p.User)
            .Include(p => p.Group) // Include group information for group posts
            .Include(p => p.HiddenByUser)
            .Include(p => p.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .Include(p => p.Likes)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment))
            .Include(p => p.Reposts)
            .Where(p => p.PostSystemTags.Any(pst =>
                pst.SystemTag.Category == SystemTagCategory.Violation ||
                pst.SystemTag.Category == SystemTagCategory.ModerationStatus))
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .ToListAsync();

        // Get flagged comments - includes both public and group comments
        var flaggedComments = await _context.Posts
            .Include(c => c.User)
            .Include(c => c.Group) // Include group information for group comments
            .Include(c => c.HiddenByUser)
            .Include(c => c.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .Where(c => c.PostType == PostType.Comment && c.PostSystemTags.Any(pst =>
                pst.SystemTag.Category == SystemTagCategory.Violation ||
                pst.SystemTag.Category == SystemTagCategory.ModerationStatus))
            .OrderByDescending(c => c.CreatedAt)
            .Take(50)
            .ToListAsync();

        // Get pending appeals
        var pendingAppeals = await _context.UserAppeals
            .Include(ua => ua.User)
            .Include(ua => ua.ReviewedByUser)
            .Include(ua => ua.TargetPost)
            .Include(ua => ua.TargetComment)
            .Where(ua => ua.Status == AppealStatus.Pending)
            .OrderByDescending(ua => ua.CreatedAt)
            .Take(25)
            .ToListAsync();

        // Get user reports
        var userReports = await _context.UserReports
            .Include(ur => ur.ReportedByUser)
            .Include(ur => ur.ReviewedByUser)
            .Include(ur => ur.Post)
                .ThenInclude(p => p!.User)
            .Include(ur => ur.Post)
                .ThenInclude(p => p!.Likes)
            .Include(ur => ur.Post)
                .ThenInclude(p => p!.Children.Where(c => c.PostType == PostType.Comment))
            .Include(ur => ur.Post)
                .ThenInclude(p => p!.Reposts)
            .Include(ur => ur.Comment)
                .ThenInclude(c => c!.User)
            .Include(ur => ur.UserReportSystemTags)
                .ThenInclude(urst => urst.SystemTag)
            .Where(ur => ur.Status == UserReportStatus.Pending)
            .OrderByDescending(ur => ur.CreatedAt)
            .Take(50)
            .ToListAsync();

        // Get all AI suggested tags for the flagged posts in a single query to avoid concurrency issues
        var postIds = flaggedPosts.Select(p => p.Id).ToList();
        var aiSuggestedTagsLookup = await _context.AiSuggestedTags
            .Include(ast => ast.ApprovedByUser)
            .Where(ast => ast.PostId.HasValue && postIds.Contains(ast.PostId.Value))
            .OrderByDescending(ast => ast.SuggestedAt)
            .ToListAsync();

        var aiSuggestedTagsByPostId = aiSuggestedTagsLookup
            .Where(ast => ast.PostId.HasValue)
            .GroupBy(ast => ast.PostId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return new ContentQueueDto
        {
            FlaggedPosts = flaggedPosts.Select(p => p.MapToAdminPostDtoWithTags(aiSuggestedTagsByPostId.GetValueOrDefault(p.Id, new List<AiSuggestedTag>()))).ToList(),
            FlaggedComments = flaggedComments.Select(MappingUtilities.MapPostToAdminCommentDto).ToList(),
            PendingAppeals = pendingAppeals.Select(MappingUtilities.MapToUserAppealDto).ToList(),
            UserReports = userReports.Select(MappingUtilities.MapToUserReportDto).ToList(),
            TotalFlaggedContent = flaggedPosts.Count + flaggedComments.Count + userReports.Count
        };
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(int page = 1, int pageSize = 25, AuditAction? action = null, int? performedByUserId = null, int? targetUserId = null)
    {
        return await _auditService.GetAuditLogsAsync(page, pageSize, action, performedByUserId, targetUserId);
    }

    // User Appeals - placeholder implementations
    public async Task<IEnumerable<UserAppealDto>> GetUserAppealsAsync(int page = 1, int pageSize = 25, AppealStatus? status = null, AppealType? type = null, int? userId = null)
    {
        var query = _context.UserAppeals
            .Include(ua => ua.User)
            .Include(ua => ua.ReviewedByUser)
            .Include(ua => ua.TargetPost)
            .Include(ua => ua.TargetComment)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(ua => ua.Status == status.Value);

        if (type.HasValue)
            query = query.Where(ua => ua.Type == type.Value);

        if (userId.HasValue)
            query = query.Where(ua => ua.UserId == userId.Value);

        var appeals = await query
            .OrderByDescending(ua => ua.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return appeals.Select(MappingUtilities.MapToUserAppealDto);
    }

    public async Task<UserAppealDto?> GetUserAppealAsync(int appealId)
    {
        var appeal = await _context.UserAppeals
            .Include(ua => ua.User)
            .Include(ua => ua.ReviewedByUser)
            .Include(ua => ua.TargetPost)
            .Include(ua => ua.TargetComment)
            .FirstOrDefaultAsync(ua => ua.Id == appealId);

        return appeal == null ? null : appeal.MapToUserAppealDto();
    }

    public async Task<UserAppealDto> CreateUserAppealAsync(int userId, CreateAppealDto createDto)
    {
        var appeal = new UserAppeal
        {
            UserId = userId,
            Type = createDto.Type,
            Reason = createDto.Reason,
            AdditionalInfo = createDto.AdditionalInfo,
            TargetPostId = createDto.TargetPostId,
            TargetCommentId = createDto.TargetCommentId,
            Status = AppealStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserAppeals.Add(appeal);
        await _context.SaveChangesAsync();

        // Reload the appeal with User navigation property
        var appealWithUser = await _context.UserAppeals
            .Include(a => a.User)
            .FirstAsync(a => a.Id == appeal.Id);

        // Log the appeal creation
        await _auditService.LogAppealCreatedAsync(
            appeal.Id,
            userId,
            appeal.Type.ToString(),
            appeal.TargetPostId,
            appeal.TargetCommentId
        );

        return appealWithUser.MapToUserAppealDto();
    }

    public async Task<UserAppealDto?> ReviewUserAppealAsync(int appealId, int reviewedByUserId, ReviewAppealDto reviewDto)
    {
        var appeal = await _context.UserAppeals
            .Include(ua => ua.User)
            .FirstOrDefaultAsync(ua => ua.Id == appealId);

        if (appeal == null) return null;

        var moderator = await _context.Users.FindAsync(reviewedByUserId);
        if (moderator == null) return null;

        appeal.Status = reviewDto.Status;
        appeal.ReviewedByUserId = reviewedByUserId;
        appeal.ReviewNotes = reviewDto.ReviewNotes;
        appeal.ReviewedAt = DateTime.UtcNow;
        appeal.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Handle approved appeals - restore content
        if (reviewDto.Status == AppealStatus.Approved)
        {
            await HandleApprovedAppealAsync(appeal, reviewedByUserId);
            await _notificationService.CreateAppealApprovedNotificationAsync(appeal.UserId, appealId, reviewDto.ReviewNotes, moderator.Username);

            // Log appeal approval
            await _auditService.LogAppealApprovedAsync(
                appealId,
                reviewedByUserId,
                appeal.UserId,
                appeal.TargetPostId,
                appeal.TargetCommentId
            );
        }
        else if (reviewDto.Status == AppealStatus.Denied)
        {
            await _notificationService.CreateAppealDeniedNotificationAsync(appeal.UserId, appealId, reviewDto.ReviewNotes, moderator.Username);

            // Log appeal denial
            await _auditService.LogAppealDeniedAsync(
                appealId,
                reviewedByUserId,
                appeal.UserId,
                appeal.TargetPostId,
                appeal.TargetCommentId
            );
        }
        else if (reviewDto.Status == AppealStatus.Escalated)
        {
            // Log appeal escalation
            await _auditService.LogAppealEscalatedAsync(
                appealId,
                reviewedByUserId,
                appeal.UserId,
                appeal.TargetPostId,
                appeal.TargetCommentId
            );
        }

        return appeal.MapToUserAppealDto();
    }

    private async Task HandleApprovedAppealAsync(UserAppeal appeal, int reviewedByUserId)
    {
        try
        {
            if (appeal.Type == AppealType.ContentRemoval)
            {
                // Handle post appeals
                if (appeal.TargetPostId.HasValue)
                {
                    var post = await _context.Posts
                        .Include(p => p.PostSystemTags)
                        .FirstOrDefaultAsync(p => p.Id == appeal.TargetPostId.Value);

                    if (post != null && post.IsHidden)
                    {
                        // Unhide the post
                        post.IsHidden = false;
                        post.HiddenReasonType = PostHiddenReasonType.None;
                        post.HiddenReason = null;
                        post.HiddenByUserId = null;
                        post.HiddenAt = null;

                        // Remove all system tags applied to this post
                        var systemTags = post.PostSystemTags.ToList();
                        _context.PostSystemTags.RemoveRange(systemTags);

                        await _context.SaveChangesAsync();

                        // Log the restoration
                        await _auditService.LogActionAsync(
                            AuditAction.PostRestored,
                            reviewedByUserId,
                            targetPostId: post.Id,
                            reason: $"Appeal approved - content restored",
                            details: $"Appeal ID: {appeal.Id}, Removed {systemTags.Count} system tags"
                        );

                        // Send notification to the post owner
                        await _notificationService.CreateContentRestoredNotificationAsync(
                            post.UserId,
                            "post",
                            post.Id,
                            "Appeal Approved"
                        );

                        _logger.LogInformation("Post {PostId} restored due to approved appeal {AppealId}", post.Id, appeal.Id);
                    }
                }
                // Handle comment appeals
                else if (appeal.TargetCommentId.HasValue)
                {
                    var comment = await _context.Posts
                        .Include(c => c.PostSystemTags)
                        .FirstOrDefaultAsync(c => c.Id == appeal.TargetCommentId.Value && c.PostType == PostType.Comment);

                    if (comment != null && comment.IsHidden)
                    {
                        // Unhide the comment
                        comment.IsHidden = false;
                        comment.HiddenReasonType = PostHiddenReasonType.None;
                        comment.HiddenByUserId = null;
                        comment.HiddenAt = null;
                        comment.HiddenReason = null;

                        // Remove all system tags applied to this comment
                        var systemTags = comment.PostSystemTags.ToList();
                        _context.PostSystemTags.RemoveRange(systemTags);

                        await _context.SaveChangesAsync();

                        // Log the restoration
                        await _auditService.LogActionAsync(
                            AuditAction.CommentRestored,
                            reviewedByUserId,
                            targetCommentId: comment.Id,
                            reason: $"Appeal approved - content restored",
                            details: $"Appeal ID: {appeal.Id}, Removed {systemTags.Count} system tags"
                        );

                        // Send notification to the comment owner
                        await _notificationService.CreateContentRestoredNotificationAsync(
                            comment.UserId,
                            "comment",
                            comment.Id,
                            "Appeal Approved"
                        );

                        _logger.LogInformation("Comment {CommentId} restored due to approved appeal {AppealId}", comment.Id, appeal.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling approved appeal {AppealId}", appeal.Id);
            throw;
        }
    }



    // Enhanced Analytics Methods
    public async Task<UserGrowthStatsDto> GetUserGrowthStatsAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var dailyStats = await _context.Users
            .Where(u => u.CreatedAt >= startDate)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new DailyStatsDto
            {
                Date = g.Key,
                Count = g.Count(),
                Label = g.Key.ToString("MMM dd")
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        var totalNewUsers = dailyStats.Sum(d => d.Count);
        var totalActiveUsers = await _context.Users
            .Where(u => u.LastLoginAt >= startDate)
            .CountAsync();

        var previousPeriodUsers = await _context.Users
            .Where(u => u.CreatedAt >= startDate.AddDays(-days) && u.CreatedAt < startDate)
            .CountAsync();

        var growthRate = previousPeriodUsers > 0
            ? ((double)(totalNewUsers - previousPeriodUsers) / previousPeriodUsers) * 100
            : 0;

        var peakDay = dailyStats.OrderByDescending(d => d.Count).FirstOrDefault();

        return new UserGrowthStatsDto
        {
            DailyStats = dailyStats,
            TotalNewUsers = totalNewUsers,
            TotalActiveUsers = totalActiveUsers,
            GrowthRate = Math.Round(growthRate, 2),
            PeakDayNewUsers = peakDay?.Count ?? 0,
            PeakDate = peakDay?.Date ?? DateTime.UtcNow
        };
    }

    public async Task<ContentStatsDto> GetContentStatsAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var dailyPosts = await _context.Posts
            .Where(p => p.PostType == PostType.Post && p.CreatedAt >= startDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new DailyStatsDto
            {
                Date = g.Key,
                Count = g.Count(),
                Label = g.Key.ToString("MMM dd")
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        var dailyComments = await _context.Posts
            .Where(c => c.PostType == PostType.Comment && c.CreatedAt >= startDate)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new DailyStatsDto
            {
                Date = g.Key,
                Count = g.Count(),
                Label = g.Key.ToString("MMM dd")
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        var totalPosts = dailyPosts.Sum(d => d.Count);
        var totalComments = dailyComments.Sum(d => d.Count);

        return new ContentStatsDto
        {
            DailyPosts = dailyPosts,
            DailyComments = dailyComments,
            TotalPosts = totalPosts,
            TotalComments = totalComments,
            PostsGrowthRate = 0, // Could calculate vs previous period
            CommentsGrowthRate = 0, // Could calculate vs previous period
            AveragePostsPerDay = totalPosts / days,
            AverageCommentsPerDay = totalComments / days
        };
    }

    public async Task<ModerationTrendsDto> GetModerationTrendsAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var dailyActions = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startDate)
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new DailyStatsDto
            {
                Date = g.Key,
                Count = g.Count(),
                Label = g.Key.ToString("MMM dd")
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        var actionBreakdown = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startDate)
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalActions = actionBreakdown.Sum(a => a.Count);
        var actionStats = actionBreakdown.Select(a => new ActionBreakdownDto
        {
            Action = a.Action.ToString(),
            Count = a.Count,
            Percentage = totalActions > 0 ? Math.Round((double)a.Count / totalActions * 100, 2) : 0
        }).ToList();

        var peakDay = dailyActions.OrderByDescending(d => d.Count).FirstOrDefault();

        return new ModerationTrendsDto
        {
            DailyActions = dailyActions,
            ActionBreakdown = actionStats,
            TotalActions = totalActions,
            ActionsGrowthRate = 0, // Could calculate vs previous period
            PeakDayActions = peakDay?.Count ?? 0,
            PeakDate = peakDay?.Date ?? DateTime.UtcNow
        };
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        var last24Hours = DateTime.UtcNow.AddHours(-24);

        var activeUsers24h = await _context.Users
            .Where(u => u.LastLoginAt >= last24Hours)
            .CountAsync();

        // Mock system health data - in real implementation, you'd get this from monitoring systems
        return new SystemHealthDto
        {
            UptimePercentage = 99.9,
            ActiveUsers24h = activeUsers24h,
            ErrorCount24h = 0, // Would come from logging system
            AverageResponseTime = 150.5, // Would come from monitoring
            DatabaseConnections = 25, // Would come from DB monitoring
            MemoryUsage = 1024 * 1024 * 512, // 512MB - would come from system monitoring
            CpuUsage = 15.5, // Would come from system monitoring
            Alerts = new List<SystemAlertDto>() // Would come from alerting system
        };
    }

    public async Task<TopModeratorsDto> GetTopModeratorsAsync(int days = 30, int limit = 10)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var moderatorStats = await _context.AuditLogs
            .Where(a => a.CreatedAt >= startDate)
            .Include(a => a.PerformedByUser)
            .GroupBy(a => new { a.PerformedByUserId, a.PerformedByUser.Username, a.PerformedByUser.Role })
            .Select(g => new
            {
                UserId = g.Key.PerformedByUserId,
                Username = g.Key.Username,
                Role = g.Key.Role,
                TotalActions = g.Count(),
                UserActions = g.Count(a => a.Action >= AuditAction.UserSuspended && a.Action < AuditAction.PostHidden),
                ContentActions = g.Count(a => a.Action >= AuditAction.PostHidden && a.Action < AuditAction.SystemTagCreated),
                LastActive = g.Max(a => a.CreatedAt)
            })
            .OrderByDescending(m => m.TotalActions)
            .Take(limit)
            .ToListAsync();

        var moderators = moderatorStats.Select(m => new ModeratorStatsDto
        {
            Username = m.Username,
            Role = m.Role,
            TotalActions = m.TotalActions,
            UserActions = m.UserActions,
            ContentActions = m.ContentActions,
            SuccessRate = 95.0, // Would calculate based on appeals/reversals
            LastActive = m.LastActive
        }).ToList();

        return new TopModeratorsDto
        {
            Moderators = moderators,
            TotalModerators = moderators.Count,
            TotalActions = moderators.Sum(m => m.TotalActions)
        };
    }

    public async Task<ContentTrendsDto> GetContentTrendsAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        // Get trending hashtags from recent posts (excluding hidden/deleted posts)
        var trendingHashtags = await _context.PostTags
            .Include(pt => pt.Tag)
            .Include(pt => pt.Post)
                .ThenInclude(p => p.User)
            .Where(pt => pt.Post.CreatedAt >= startDate &&
                        !pt.Post.IsHidden &&
                        pt.Post.User.Status == UserStatus.Active)
            .GroupBy(pt => pt.Tag.Name)
            .Select(g => new HashtagStatsDto
            {
                Hashtag = g.Key,
                Count = g.Count(),
                GrowthRate = 0, // Would calculate vs previous period
                UniqueUsers = g.Select(pt => pt.Post.UserId).Distinct().Count()
            })
            .OrderByDescending(h => h.Count)
            .Take(10)
            .ToListAsync();

        // Get engagement trends (likes, comments, reposts) - calculate separately to avoid EF translation issues
        var engagementTrends = new List<DailyStatsDto>();

        // Get daily likes
        var dailyLikes = await _context.Likes
            .Where(l => l.CreatedAt >= startDate)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Get daily comments
        var dailyCommentsCount = await _context.Posts
            .Where(c => c.PostType == PostType.Comment && c.CreatedAt >= startDate)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Get daily reposts (using new unified system)
        var dailyReposts = await _context.Posts
            .Where(p => p.PostType == PostType.Repost && p.CreatedAt >= startDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Combine all engagement data by date
        var allDates = dailyLikes.Select(d => d.Date)
            .Union(dailyCommentsCount.Select(d => d.Date))
            .Union(dailyReposts.Select(d => d.Date))
            .Distinct()
            .OrderBy(d => d);

        foreach (var date in allDates)
        {
            var likesCount = dailyLikes.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
            var commentsCount = dailyCommentsCount.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
            var repostsCount = dailyReposts.FirstOrDefault(d => d.Date == date)?.Count ?? 0;

            engagementTrends.Add(new DailyStatsDto
            {
                Date = date,
                Count = likesCount + commentsCount + repostsCount,
                Label = date.ToString("MMM dd")
            });
        }

        return new ContentTrendsDto
        {
            TrendingHashtags = trendingHashtags,
            EngagementTrends = engagementTrends,
            TotalHashtags = trendingHashtags.Count,
            AverageEngagementRate = engagementTrends.Any() ? engagementTrends.Average(e => e.Count) : 0
        };
    }

    public async Task<UserEngagementStatsDto> GetUserEngagementStatsAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        // Daily engagement (posts + comments + likes + reposts) - calculate separately to avoid EF translation issues
        var dailyEngagement = new List<DailyStatsDto>();

        // Get daily posts
        var dailyPostsCount = await _context.Posts
            .Where(p => p.PostType == PostType.Post && p.CreatedAt >= startDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Get daily likes
        var dailyLikesCount = await _context.Likes
            .Where(l => l.CreatedAt >= startDate)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Get daily comments
        var dailyCommentsEngagement = await _context.Posts
            .Where(c => c.PostType == PostType.Comment && c.CreatedAt >= startDate)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Get daily reposts (using new unified system)
        var dailyRepostsCount = await _context.Posts
            .Where(p => p.PostType == PostType.Repost && p.CreatedAt >= startDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Combine all engagement data by date
        var allEngagementDates = dailyPostsCount.Select(d => d.Date)
            .Union(dailyLikesCount.Select(d => d.Date))
            .Union(dailyCommentsEngagement.Select(d => d.Date))
            .Union(dailyRepostsCount.Select(d => d.Date))
            .Distinct()
            .OrderBy(d => d);

        foreach (var date in allEngagementDates)
        {
            var postsCount = dailyPostsCount.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
            var likesCount = dailyLikesCount.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
            var commentsCount = dailyCommentsEngagement.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
            var repostsCount = dailyRepostsCount.FirstOrDefault(d => d.Date == date)?.Count ?? 0;

            dailyEngagement.Add(new DailyStatsDto
            {
                Date = date,
                Count = postsCount + likesCount + commentsCount + repostsCount,
                Label = date.ToString("MMM dd")
            });
        }

        // Engagement breakdown by type
        var totalPosts = await _context.Posts.Where(p => p.PostType == PostType.Post && p.CreatedAt >= startDate).CountAsync();
        var totalComments = await _context.Posts.Where(c => c.PostType == PostType.Comment && c.CreatedAt >= startDate).CountAsync();
        var totalLikes = await _context.Likes.Where(l => l.CreatedAt >= startDate).CountAsync();
        var totalReposts = await _context.Posts.Where(p => p.PostType == PostType.Repost && p.CreatedAt >= startDate).CountAsync();

        var totalEngagement = totalPosts + totalComments + totalLikes + totalReposts;

        var engagementBreakdown = new List<EngagementTypeStatsDto>
        {
            new() { Type = "Posts", Count = totalPosts, Percentage = totalEngagement > 0 ? Math.Round((double)totalPosts / totalEngagement * 100, 2) : 0 },
            new() { Type = "Comments", Count = totalComments, Percentage = totalEngagement > 0 ? Math.Round((double)totalComments / totalEngagement * 100, 2) : 0 },
            new() { Type = "Likes", Count = totalLikes, Percentage = totalEngagement > 0 ? Math.Round((double)totalLikes / totalEngagement * 100, 2) : 0 },
            new() { Type = "Reposts", Count = totalReposts, Percentage = totalEngagement > 0 ? Math.Round((double)totalReposts / totalEngagement * 100, 2) : 0 }
        };

        return new UserEngagementStatsDto
        {
            DailyEngagement = dailyEngagement,
            AverageSessionDuration = 25.5, // Would come from analytics tracking
            TotalSessions = 1000, // Would come from analytics tracking
            RetentionRate = 75.0, // Would calculate based on user return visits
            EngagementBreakdown = engagementBreakdown
        };
    }
    
    // AI Suggested Tags Management
    public async Task<IEnumerable<AiSuggestedTagDto>> GetPendingAiSuggestionsAsync(int? postId = null, int? commentId = null, int page = 1, int pageSize = 25)
    {
        var query = _context.AiSuggestedTags
            .Include(ast => ast.Post)
                .ThenInclude(p => p!.User)
            .Include(ast => ast.Comment)
                .ThenInclude(c => c!.User)
            .Include(ast => ast.ApprovedByUser)
            .Where(ast => !ast.IsApproved && !ast.IsRejected);

        if (postId.HasValue)
            query = query.Where(ast => ast.PostId == postId.Value);

        if (commentId.HasValue)
            query = query.Where(ast => ast.CommentId == commentId.Value);

        var suggestions = await query
            .OrderByDescending(ast => ast.SuggestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return suggestions.Select(MappingUtilities.MapToAiSuggestedTagDto);
    }

    public async Task<bool> ApproveAiSuggestedTagAsync(int suggestedTagId, int approvedByUserId, string? reason = null)
    {
        try
        {
            var suggestedTag = await _context.AiSuggestedTags.FindAsync(suggestedTagId);
            if (suggestedTag == null)
                return false;

            suggestedTag.IsApproved = true;
            suggestedTag.IsRejected = false;
            suggestedTag.ApprovedByUserId = approvedByUserId;
            suggestedTag.ApprovedAt = DateTime.UtcNow;
            suggestedTag.ApprovalReason = reason;

            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogActionAsync(
                suggestedTag.PostId.HasValue ? AuditAction.PostSystemTagAdded : AuditAction.CommentSystemTagAdded,
                approvedByUserId,
                targetPostId: suggestedTag.PostId,
                targetCommentId: suggestedTag.CommentId,
                reason: $"Approved AI suggestion: {suggestedTag.TagName}",
                details: reason
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving AI suggested tag {SuggestedTagId}", suggestedTagId);
            return false;
        }
    }

    public async Task<bool> RejectAiSuggestedTagAsync(int suggestedTagId, int approvedByUserId, string? reason = null)
    {
        try
        {
            var suggestedTag = await _context.AiSuggestedTags.FindAsync(suggestedTagId);
            if (suggestedTag == null)
                return false;

            suggestedTag.IsApproved = false;
            suggestedTag.IsRejected = true;
            suggestedTag.ApprovedByUserId = approvedByUserId;
            suggestedTag.ApprovedAt = DateTime.UtcNow;
            suggestedTag.ApprovalReason = reason;

            await _context.SaveChangesAsync();

            // Log the action
            await _auditService.LogActionAsync(
                suggestedTag.PostId.HasValue ? AuditAction.PostSystemTagRemoved : AuditAction.CommentSystemTagRemoved,
                approvedByUserId,
                targetPostId: suggestedTag.PostId,
                targetCommentId: suggestedTag.CommentId,
                reason: $"Rejected AI suggestion: {suggestedTag.TagName}",
                details: reason
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting AI suggested tag {SuggestedTagId}", suggestedTagId);
            return false;
        }
    }

    public async Task<bool> BulkApproveAiSuggestedTagsAsync(IEnumerable<int> suggestedTagIds, int approvedByUserId, string? reason = null)
    {
        try
        {
            var tagIds = suggestedTagIds.ToList();
            var suggestedTags = await _context.AiSuggestedTags
                .Where(ast => tagIds.Contains(ast.Id))
                .ToListAsync();

            foreach (var suggestedTag in suggestedTags)
            {
                suggestedTag.IsApproved = true;
                suggestedTag.IsRejected = false;
                suggestedTag.ApprovedByUserId = approvedByUserId;
                suggestedTag.ApprovedAt = DateTime.UtcNow;
                suggestedTag.ApprovalReason = reason;
            }

            await _context.SaveChangesAsync();

            // Log the bulk action
            await _auditService.LogActionAsync(
                AuditAction.PostSystemTagAdded,
                approvedByUserId,
                reason: $"Bulk approved {suggestedTags.Count} AI suggestions",
                details: reason
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk approving AI suggested tags");
            return false;
        }
    }

    public async Task<bool> BulkRejectAiSuggestedTagsAsync(IEnumerable<int> suggestedTagIds, int approvedByUserId, string? reason = null)
    {
        try
        {
            var tagIds = suggestedTagIds.ToList();
            var suggestedTags = await _context.AiSuggestedTags
                .Where(ast => tagIds.Contains(ast.Id))
                .ToListAsync();

            foreach (var suggestedTag in suggestedTags)
            {
                suggestedTag.IsApproved = false;
                suggestedTag.IsRejected = true;
                suggestedTag.ApprovedByUserId = approvedByUserId;
                suggestedTag.ApprovedAt = DateTime.UtcNow;
                suggestedTag.ApprovalReason = reason;
            }

            await _context.SaveChangesAsync();

            // Log the bulk action
            await _auditService.LogActionAsync(
                AuditAction.PostSystemTagRemoved,
                approvedByUserId,
                reason: $"Bulk rejected {suggestedTags.Count} AI suggestions",
                details: reason
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk rejecting AI suggested tags");
            return false;
        }
    }

    // Trust Score Management
    public async Task<IEnumerable<UserTrustScoreDto>> GetUserTrustScoresAsync(int page = 1, int pageSize = 25, float? minScore = null, float? maxScore = null)
    {
        var query = _context.Users.AsQueryable();

        if (minScore.HasValue)
            query = query.Where(u => u.TrustScore >= minScore.Value);

        if (maxScore.HasValue)
            query = query.Where(u => u.TrustScore <= maxScore.Value);

        var users = await query
            .OrderBy(u => u.TrustScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserTrustScoreDto
            {
                UserId = u.Id,
                Username = u.Username,
                Email = u.Email,
                TrustScore = u.TrustScore ?? 1.0f,
                CreatedAt = u.CreatedAt,
                LastSeenAt = u.LastSeenAt,
                Status = u.Status,
                Role = u.Role,
                PostCount = u.Posts.Count,
                CommentCount = u.Posts.Count(p => p.PostType == PostType.Comment),
                LikeCount = u.Likes.Count,
                ReportCount = _context.UserReports.Count(ur => ur.ReportedByUserId == u.Id),
                ModerationActionCount = _context.AuditLogs.Count(al => al.TargetUserId == u.Id &&
                    (al.Action == AuditAction.PostHidden || al.Action == AuditAction.CommentHidden || al.Action == AuditAction.UserSuspended))
            })
            .ToListAsync();

        return users;
    }

    public async Task<UserTrustScoreDto?> GetUserTrustScoreAsync(int userId)
    {
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserTrustScoreDto
            {
                UserId = u.Id,
                Username = u.Username,
                Email = u.Email,
                TrustScore = u.TrustScore ?? 1.0f,
                CreatedAt = u.CreatedAt,
                LastSeenAt = u.LastSeenAt,
                Status = u.Status,
                Role = u.Role,
                PostCount = u.Posts.Count,
                CommentCount = u.Posts.Count(p => p.PostType == PostType.Comment),
                LikeCount = u.Likes.Count,
                ReportCount = _context.UserReports.Count(ur => ur.ReportedByUserId == u.Id),
                ModerationActionCount = _context.AuditLogs.Count(al => al.TargetUserId == u.Id &&
                    (al.Action == AuditAction.PostHidden || al.Action == AuditAction.CommentHidden || al.Action == AuditAction.UserSuspended))
            })
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<IEnumerable<TrustScoreHistoryDto>> GetUserTrustScoreHistoryAsync(int userId, int page = 1, int pageSize = 25)
    {
        var history = await _context.UserTrustScoreHistories
            .Include(h => h.User)
            .Include(h => h.TriggeredByUser)
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new TrustScoreHistoryDto
            {
                Id = h.Id,
                UserId = h.UserId,
                Username = h.User.Username,
                PreviousScore = h.PreviousScore,
                NewScore = h.NewScore,
                ScoreChange = h.ScoreChange,
                Reason = h.Reason.ToString(),
                Details = h.Details,
                RelatedEntityType = h.RelatedEntityType,
                RelatedEntityId = h.RelatedEntityId,
                TriggeredByUsername = h.TriggeredByUser != null ? h.TriggeredByUser.Username : null,
                CalculatedBy = h.CalculatedBy,
                IsAutomatic = h.IsAutomatic,
                Confidence = h.Confidence,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();

        return history;
    }

    public async Task<TrustScoreStatsDto> GetTrustScoreStatisticsAsync()
    {
        var stats = await _trustScoreService.GetTrustScoreStatisticsAsync();

        return new TrustScoreStatsDto
        {
            TotalUsers = (int)(stats.GetValueOrDefault("totalUsers", 0)),
            AverageScore = (float)(stats.GetValueOrDefault("averageScore", 0.0)),
            MedianScore = (float)(stats.GetValueOrDefault("medianScore", 0.0)),
            MinScore = (float)(stats.GetValueOrDefault("minScore", 0.0)),
            MaxScore = (float)(stats.GetValueOrDefault("maxScore", 1.0)),
            Distribution = stats.GetValueOrDefault("distribution", new Dictionary<string, int>()) as Dictionary<string, int> ?? new()
        };
    }

    public async Task<TrustScoreFactorsDto?> GetUserTrustScoreFactorsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var factors = await _trustScoreService.GetTrustScoreFactorsAsync(userId);

        return new TrustScoreFactorsDto
        {
            UserId = userId,
            Username = user.Username,
            CurrentScore = user.TrustScore ?? 1.0f,
            Factors = factors
        };
    }

    public async Task<bool> UpdateUserTrustScoreAsync(int userId, int adjustedByUserId, UpdateTrustScoreDto updateDto)
    {
        try
        {
            await _trustScoreService.UpdateTrustScoreForActionAsync(
                userId,
                TrustScoreAction.AdminAdjustment,
                "user",
                userId,
                updateDto.Details ?? updateDto.Reason
            );

            // Log the manual adjustment
            await _auditService.LogActionAsync(
                AuditAction.UserRoleChanged, // Using closest available action
                adjustedByUserId,
                targetUserId: userId,
                reason: $"Trust score manually adjusted: {updateDto.Reason}",
                details: $"Score change: {updateDto.ScoreChange}, Details: {updateDto.Details}"
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for user {UserId}", userId);
            return false;
        }
    }

    public async Task<AdminUserDetailsDto?> GetUserDetailsAsync(int userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.SuspendedByUser)
                .Include(u => u.Posts)
                .Include(u => u.Likes)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            // Get trust score factors
            var trustScoreFactors = await GetUserTrustScoreFactorsAsync(userId);

            // Get recent trust score history (last 10 entries)
            var trustScoreHistory = await GetUserTrustScoreHistoryAsync(userId, 1, 10);

            // Get recent moderation actions (last 10 entries)
            var recentModerationActions = await _context.AuditLogs
                .Include(al => al.PerformedByUser)
                .Where(al => al.TargetUserId == userId)
                .OrderByDescending(al => al.CreatedAt)
                .Take(10)
                .Select(al => new AuditLogDto
                {
                    Id = al.Id,
                    Action = al.Action,
                    UserId = al.PerformedByUserId,
                    PerformedByUsername = al.PerformedByUser.Username,
                    TargetUserId = al.TargetUserId,
                    Reason = al.Reason,
                    Details = al.Details,
                    CreatedAt = al.CreatedAt
                })
                .ToListAsync();

            // Get rate limiting information
            var rateLimitService = _serviceProvider.GetRequiredService<IApiRateLimitService>();
            var isCurrentlyRateLimited = await rateLimitService.IsUserBlockedAsync(userId);
            var recentViolations = await rateLimitService.GetRecentViolationsAsync(userId);

            // Count reports made by this user
            var reportCount = await _context.UserReports
                .CountAsync(ur => ur.ReportedByUserId == userId);

            // Count moderation actions against this user
            var moderationActionCount = await _context.AuditLogs
                .CountAsync(al => al.TargetUserId == userId &&
                    (al.Action == AuditAction.PostHidden ||
                     al.Action == AuditAction.CommentHidden ||
                     al.Action == AuditAction.UserSuspended ||
                     al.Action == AuditAction.UserBanned));
            var dto = user.MapToAdminUserDetailsDto();
            dto.TrustScoreFactors = trustScoreFactors;
            dto.RecentTrustScoreHistory = trustScoreHistory.ToList();
            dto.IsCurrentlyRateLimited = isCurrentlyRateLimited;
            dto.RecentRateLimitViolations = recentViolations.Count;
            dto.ReportCount = reportCount;
            dto.ModerationActionCount = moderationActionCount;
            dto.RecentModerationActions = recentModerationActions;
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user details for user {UserId}", userId);
            return null;
        }
    }
}
