using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Services;

public class AdminService : IAdminService
{
    private readonly YapplrDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(YapplrDbContext context, IAuditService auditService, INotificationService notificationService, ILogger<AdminService> logger)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
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

        return tags.Select(MapToSystemTagDto);
    }

    public async Task<SystemTagDto?> GetSystemTagAsync(int id)
    {
        var tag = await _context.SystemTags.FindAsync(id);
        return tag == null ? null : MapToSystemTagDto(tag);
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

        return MapToSystemTagDto(systemTag);
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
        return MapToSystemTagDto(systemTag);
    }

    public async Task<bool> DeleteSystemTagAsync(int id)
    {
        var systemTag = await _context.SystemTags.FindAsync(id);
        if (systemTag == null) return false;

        _context.SystemTags.Remove(systemTag);
        await _context.SaveChangesAsync();
        return true;
    }

    // Content Moderation
    public async Task<IEnumerable<AdminPostDto>> GetPostsForModerationAsync(int page = 1, int pageSize = 25, bool? isHidden = null)
    {
        var query = _context.Posts
            .Include(p => p.User)
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

        return posts.Select(p => MapToAdminPostDtoWithTags(p, aiSuggestedTagsByPostId.GetValueOrDefault(p.Id, new List<AiSuggestedTag>()))).ToList();
    }

    public async Task<IEnumerable<AdminCommentDto>> GetCommentsForModerationAsync(int page = 1, int pageSize = 25, bool? isHidden = null)
    {
        var query = _context.Comments
            .Include(c => c.User)
            .Include(c => c.HiddenByUser)
            .Include(c => c.CommentSystemTags)
                .ThenInclude(cst => cst.SystemTag)
            .AsQueryable();

        if (isHidden.HasValue)
            query = query.Where(c => c.IsHidden == isHidden.Value);

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return comments.Select(MapToAdminCommentDto);
    }

    public async Task<AdminPostDto?> GetPostForModerationAsync(int postId)
    {
        var post = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.HiddenByUser)
            .Include(p => p.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Reposts)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null) return null;

        // Get AI suggested tags for this post
        var aiSuggestedTags = await _context.AiSuggestedTags
            .Include(ast => ast.ApprovedByUser)
            .Where(ast => ast.PostId == post.Id)
            .OrderByDescending(ast => ast.SuggestedAt)
            .ToListAsync();

        return MapToAdminPostDtoWithTags(post, aiSuggestedTags);
    }

    public async Task<AdminCommentDto?> GetCommentForModerationAsync(int commentId)
    {
        var comment = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.HiddenByUser)
            .Include(c => c.CommentSystemTags)
                .ThenInclude(cst => cst.SystemTag)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        return comment == null ? null : MapToAdminCommentDto(comment);
    }

    public async Task<bool> HidePostAsync(int postId, int hiddenByUserId, string reason)
    {
        var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null) return false;

        var moderator = await _context.Users.FindAsync(hiddenByUserId);
        if (moderator == null) return false;

        post.IsHidden = true;
        post.HiddenByUserId = hiddenByUserId;
        post.HiddenAt = DateTime.UtcNow;
        post.HiddenReason = reason;

        await _context.SaveChangesAsync();
        await _auditService.LogPostHiddenAsync(postId, hiddenByUserId, reason);

        // Send notification to the post owner
        await _notificationService.CreateContentHiddenNotificationAsync(post.UserId, "post", postId, reason, moderator.Username);

        return true;
    }

    public async Task<bool> UnhidePostAsync(int postId)
    {
        var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null) return false;

        post.IsHidden = false;
        post.HiddenByUserId = null;
        post.HiddenAt = null;
        post.HiddenReason = null;

        await _context.SaveChangesAsync();

        // Send notification to the post owner
        await _notificationService.CreateContentRestoredNotificationAsync(post.UserId, "post", postId, "System");

        return true;
    }

    public async Task<bool> DeletePostAsync(int postId, int deletedByUserId, string reason)
    {
        var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null) return false;

        var moderator = await _context.Users.FindAsync(deletedByUserId);
        if (moderator == null) return false;

        var postUserId = post.UserId; // Store before deletion

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        await _auditService.LogPostDeletedAsync(postId, deletedByUserId, reason);

        // Send notification to the post owner
        await _notificationService.CreateContentDeletedNotificationAsync(postUserId, "post", postId, reason, moderator.Username);

        return true;
    }

    public async Task<bool> HideCommentAsync(int commentId, int hiddenByUserId, string reason)
    {
        var comment = await _context.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null) return false;

        var moderator = await _context.Users.FindAsync(hiddenByUserId);
        if (moderator == null) return false;

        comment.IsHidden = true;
        comment.HiddenByUserId = hiddenByUserId;
        comment.HiddenAt = DateTime.UtcNow;
        comment.HiddenReason = reason;

        await _context.SaveChangesAsync();
        await _auditService.LogCommentHiddenAsync(commentId, hiddenByUserId, reason);

        // Send notification to the comment owner
        await _notificationService.CreateContentHiddenNotificationAsync(comment.UserId, "comment", commentId, reason, moderator.Username);

        return true;
    }

    public async Task<bool> UnhideCommentAsync(int commentId)
    {
        var comment = await _context.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null) return false;

        comment.IsHidden = false;
        comment.HiddenByUserId = null;
        comment.HiddenAt = null;
        comment.HiddenReason = null;

        await _context.SaveChangesAsync();

        // Send notification to the comment owner
        await _notificationService.CreateContentRestoredNotificationAsync(comment.UserId, "comment", commentId, "System");

        return true;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int deletedByUserId, string reason)
    {
        var comment = await _context.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null) return false;

        var moderator = await _context.Users.FindAsync(deletedByUserId);
        if (moderator == null) return false;

        var commentUserId = comment.UserId; // Store before deletion

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        await _auditService.LogCommentDeletedAsync(commentId, deletedByUserId, reason);

        // Send notification to the comment owner
        await _notificationService.CreateContentDeletedNotificationAsync(commentUserId, "comment", commentId, reason, moderator.Username);

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
        var existing = await _context.CommentSystemTags
            .FirstOrDefaultAsync(cst => cst.CommentId == commentId && cst.SystemTagId == systemTagId);

        if (existing != null) return false;

        var commentSystemTag = new CommentSystemTag
        {
            CommentId = commentId,
            SystemTagId = systemTagId,
            AppliedByUserId = appliedByUserId,
            Reason = reason,
            AppliedAt = DateTime.UtcNow
        };

        _context.CommentSystemTags.Add(commentSystemTag);
        await _context.SaveChangesAsync();
        await _auditService.LogCommentSystemTagAddedAsync(commentId, systemTagId, appliedByUserId, reason);
        return true;
    }

    public async Task<bool> RemoveSystemTagFromCommentAsync(int commentId, int systemTagId, int removedByUserId)
    {
        var commentSystemTag = await _context.CommentSystemTags
            .FirstOrDefaultAsync(cst => cst.CommentId == commentId && cst.SystemTagId == systemTagId);

        if (commentSystemTag == null) return false;

        _context.CommentSystemTags.Remove(commentSystemTag);
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
            post.HiddenByUserId = hiddenByUserId;
            post.HiddenAt = DateTime.UtcNow;
            post.HiddenReason = reason;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogBulkContentHiddenAsync(postIds, hiddenByUserId, reason);
        return posts.Count;
    }

    public async Task<int> BulkDeletePostsAsync(IEnumerable<int> postIds, int deletedByUserId, string reason)
    {
        var posts = await _context.Posts
            .Where(p => postIds.Contains(p.Id))
            .ToListAsync();

        _context.Posts.RemoveRange(posts);
        await _context.SaveChangesAsync();
        await _auditService.LogBulkContentDeletedAsync(postIds, deletedByUserId, reason);
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

        var totalPosts = await _context.Posts.CountAsync();
        var hiddenPosts = await _context.Posts.CountAsync(p => p.IsHidden);
        var totalComments = await _context.Comments.CountAsync();
        var hiddenComments = await _context.Comments.CountAsync(c => c.IsHidden);

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
        // Get flagged posts (posts with system tags indicating violations)
        var flaggedPosts = await _context.Posts
            .Include(p => p.User)
            .Include(p => p.HiddenByUser)
            .Include(p => p.PostSystemTags)
                .ThenInclude(pst => pst.SystemTag)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.Reposts)
            .Where(p => p.PostSystemTags.Any(pst =>
                pst.SystemTag.Category == SystemTagCategory.Violation ||
                pst.SystemTag.Category == SystemTagCategory.ModerationStatus))
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .ToListAsync();

        // Get flagged comments
        var flaggedComments = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.HiddenByUser)
            .Include(c => c.CommentSystemTags)
                .ThenInclude(cst => cst.SystemTag)
            .Where(c => c.CommentSystemTags.Any(cst =>
                cst.SystemTag.Category == SystemTagCategory.Violation ||
                cst.SystemTag.Category == SystemTagCategory.ModerationStatus))
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
            FlaggedPosts = flaggedPosts.Select(p => MapToAdminPostDtoWithTags(p, aiSuggestedTagsByPostId.GetValueOrDefault(p.Id, new List<AiSuggestedTag>()))).ToList(),
            FlaggedComments = flaggedComments.Select(MapToAdminCommentDto).ToList(),
            PendingAppeals = pendingAppeals.Select(MapToUserAppealDto).ToList(),
            TotalFlaggedContent = flaggedPosts.Count + flaggedComments.Count
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

        return appeals.Select(MapToUserAppealDto);
    }

    public async Task<UserAppealDto?> GetUserAppealAsync(int appealId)
    {
        var appeal = await _context.UserAppeals
            .Include(ua => ua.User)
            .Include(ua => ua.ReviewedByUser)
            .Include(ua => ua.TargetPost)
            .Include(ua => ua.TargetComment)
            .FirstOrDefaultAsync(ua => ua.Id == appealId);

        return appeal == null ? null : MapToUserAppealDto(appeal);
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

        return MapToUserAppealDto(appeal);
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

        // Send notification to the user about the appeal decision
        if (reviewDto.Status == AppealStatus.Approved)
        {
            await _notificationService.CreateAppealApprovedNotificationAsync(appeal.UserId, appealId, reviewDto.ReviewNotes, moderator.Username);
        }
        else if (reviewDto.Status == AppealStatus.Denied)
        {
            await _notificationService.CreateAppealDeniedNotificationAsync(appeal.UserId, appealId, reviewDto.ReviewNotes, moderator.Username);
        }

        return MapToUserAppealDto(appeal);
    }

    // System Administration - placeholder implementations
    public async Task<bool> CreateSystemAnnouncementAsync(string title, string content, int createdByUserId, DateTime? expiresAt = null)
    {
        // TODO: Implement system announcements
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> ToggleFeatureFlagAsync(string featureName, bool isEnabled, int changedByUserId)
    {
        // TODO: Implement feature flags
        await Task.CompletedTask;
        return true;
    }

    public async Task<Dictionary<string, bool>> GetFeatureFlagsAsync()
    {
        // TODO: Implement feature flags
        await Task.CompletedTask;
        return new Dictionary<string, bool>();
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
            .Where(p => p.CreatedAt >= startDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new DailyStatsDto
            {
                Date = g.Key,
                Count = g.Count(),
                Label = g.Key.ToString("MMM dd")
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        var dailyComments = await _context.Comments
            .Where(c => c.CreatedAt >= startDate)
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
        var actionStats = actionBreakdown.Select(a => new ActionTypeStatsDto
        {
            ActionType = a.Action.ToString(),
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

        // Get trending hashtags from recent posts
        var trendingHashtags = await _context.PostTags
            .Include(pt => pt.Tag)
            .Include(pt => pt.Post)
            .Where(pt => pt.Post.CreatedAt >= startDate)
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
        var dailyCommentsCount = await _context.Comments
            .Where(c => c.CreatedAt >= startDate)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Get daily reposts
        var dailyReposts = await _context.Reposts
            .Where(r => r.CreatedAt >= startDate)
            .GroupBy(r => r.CreatedAt.Date)
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
            .Where(p => p.CreatedAt >= startDate)
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
        var dailyCommentsEngagement = await _context.Comments
            .Where(c => c.CreatedAt >= startDate)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Get daily reposts
        var dailyRepostsCount = await _context.Reposts
            .Where(r => r.CreatedAt >= startDate)
            .GroupBy(r => r.CreatedAt.Date)
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
        var totalPosts = await _context.Posts.Where(p => p.CreatedAt >= startDate).CountAsync();
        var totalComments = await _context.Comments.Where(c => c.CreatedAt >= startDate).CountAsync();
        var totalLikes = await _context.Likes.Where(l => l.CreatedAt >= startDate).CountAsync();
        var totalReposts = await _context.Reposts.Where(r => r.CreatedAt >= startDate).CountAsync();

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

    // Mapping methods
    private static SystemTagDto MapToSystemTagDto(SystemTag systemTag)
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

    private static AdminPostDto MapToAdminPostDto(Post post)
    {
        return new AdminPostDto
        {
            Id = post.Id,
            Content = post.Content,
            ImageFileName = post.ImageFileName,
            Privacy = post.Privacy,
            IsHidden = post.IsHidden,
            HiddenReason = post.HiddenReason,
            HiddenAt = post.HiddenAt,
            HiddenByUsername = post.HiddenByUser?.Username,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            User = post.User.ToDto(),
            LikeCount = post.Likes?.Count ?? 0,
            CommentCount = post.Comments?.Count ?? 0,
            RepostCount = post.Reposts?.Count ?? 0,
            SystemTags = post.PostSystemTags?.Select(pst => MapToSystemTagDto(pst.SystemTag)).ToList() ?? new List<SystemTagDto>()
        };
    }

    private static AdminPostDto MapToAdminPostDtoWithTags(Post post, List<AiSuggestedTag> aiSuggestedTags)
    {
        var adminPostDto = MapToAdminPostDto(post);
        adminPostDto.AiSuggestedTags = aiSuggestedTags.Select(MapToAiSuggestedTagDto).ToList();
        return adminPostDto;
    }



    private static AiSuggestedTagDto MapToAiSuggestedTagDto(AiSuggestedTag aiSuggestedTag)
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

    private static AdminCommentDto MapToAdminCommentDto(Comment comment)
    {
        return new AdminCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            IsHidden = comment.IsHidden,
            HiddenReason = comment.HiddenReason,
            HiddenAt = comment.HiddenAt,
            HiddenByUsername = comment.HiddenByUser?.Username,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            User = comment.User.ToDto(),
            PostId = comment.PostId,
            SystemTags = comment.CommentSystemTags?.Select(cst => MapToSystemTagDto(cst.SystemTag)).ToList() ?? new List<SystemTagDto>()
        };
    }

    private static UserAppealDto MapToUserAppealDto(UserAppeal appeal)
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

        return suggestions.Select(MapToAiSuggestedTagDto);
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
}
