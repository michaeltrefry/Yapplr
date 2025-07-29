using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Common;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;
using Yapplr.Api.Extensions;

public class UserReportService : IUserReportService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<UserReportService> _logger;
    private readonly IAuditService _auditService;
    private readonly IAdminService _adminService;
    private readonly IModerationMessageService _moderationMessageService;
    private readonly ITrustScoreService _trustScoreService;
    private readonly ITrustBasedModerationService _trustBasedModerationService;

    public UserReportService(
        YapplrDbContext context,
        ILogger<UserReportService> logger,
        IAuditService auditService,
        IAdminService adminService,
        IModerationMessageService moderationMessageService,
        ITrustScoreService trustScoreService,
        ITrustBasedModerationService trustBasedModerationService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
        _adminService = adminService;
        _moderationMessageService = moderationMessageService;
        _trustScoreService = trustScoreService;
        _trustBasedModerationService = trustBasedModerationService;
    }

    public async Task<UserReportDto?> CreateReportAsync(int reportedByUserId, CreateUserReportDto dto)
    {
        try
        {
            // Check trust-based permissions
            if (!await _trustBasedModerationService.CanPerformActionAsync(reportedByUserId, TrustRequiredAction.ReportContent))
            {
                throw new InvalidOperationException("Insufficient trust score to report content");
            }

            // Validate that the content exists
            if (dto.PostId.HasValue)
            {
                var postExists = await _context.Posts.AnyAsync(p => p.Id == dto.PostId.Value);
                if (!postExists)
                {
                    _logger.LogWarning("Attempted to report non-existent post {PostId}", dto.PostId.Value);
                    return null;
                }
            }

            if (dto.CommentId.HasValue)
            {
                var commentExists = await _context.Posts.AnyAsync(c => c.Id == dto.CommentId.Value && c.PostType == PostType.Comment);
                if (!commentExists)
                {
                    _logger.LogWarning("Attempted to report non-existent comment {CommentId}", dto.CommentId.Value);
                    return null;
                }
            }

            // Check if user has already reported this content
            var existingReport = await _context.UserReports
                .AnyAsync(ur => ur.ReportedByUserId == reportedByUserId &&
                               ur.PostId == dto.PostId &&
                               ur.CommentId == dto.CommentId);

            if (existingReport)
            {
                _logger.LogWarning("User {UserId} attempted to report content they've already reported", reportedByUserId);
                return null;
            }

            // Create the report
            var report = new UserReport
            {
                ReportedByUserId = reportedByUserId,
                PostId = dto.PostId,
                CommentId = dto.CommentId,
                Reason = dto.Reason,
                Status = UserReportStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserReports.Add(report);
            await _context.SaveChangesAsync();

            // Add selected system tags
            if (dto.SystemTagIds.Any())
            {
                var validSystemTagIds = await _context.SystemTags
                    .Where(st => dto.SystemTagIds.Contains(st.Id) && st.IsActive)
                    .Select(st => st.Id)
                    .ToListAsync();

                foreach (var systemTagId in validSystemTagIds)
                {
                    var reportSystemTag = new UserReportSystemTag
                    {
                        UserReportId = report.Id,
                        SystemTagId = systemTagId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserReportSystemTags.Add(reportSystemTag);
                }

                await _context.SaveChangesAsync();
            }

            // Log the action
            var contentType = dto.PostId.HasValue ? "post" : "comment";
            var contentId = dto.PostId ?? dto.CommentId;
            await _auditService.LogActionAsync(
                AuditAction.UserReportCreated,
                reportedByUserId,
                $"User reported {contentType} {contentId}",
                $"Report ID: {report.Id}, Reason: {dto.Reason}"
            );

            _logger.LogInformation("User {UserId} created report {ReportId} for {ContentType} {ContentId}",
                reportedByUserId, report.Id, contentType, contentId);

            // Update trust score for submitting a report (small positive for community participation)
            try
            {
                await _trustScoreService.UpdateTrustScoreForActionAsync(
                    reportedByUserId,
                    TrustScoreAction.HelpfulReport,
                    contentType,
                    contentId,
                    $"Submitted report: {dto.Reason}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update trust score for report submission by user {UserId}", reportedByUserId);
                // Don't fail the report creation if trust score update fails
            }

            return await GetReportByIdAsync(report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user report for user {UserId}", reportedByUserId);
            return null;
        }
    }

    public async Task<IEnumerable<UserReportDto>> GetUserReportsAsync(int userId, int page = 1, int pageSize = 25)
    {
        var reports = await _context.UserReports
            .Include(ur => ur.ReportedByUser)
            .Include(ur => ur.ReviewedByUser)
            .Include(ur => ur.Post)
                .ThenInclude(p => p!.User)
            .Include(ur => ur.Comment)
                .ThenInclude(c => c!.User)
            .Include(ur => ur.UserReportSystemTags)
                .ThenInclude(urst => urst.SystemTag)
            .Where(ur => ur.ReportedByUserId == userId)
            .OrderByDescending(ur => ur.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return reports.Select(MapToUserReportDto);
    }

    public async Task<IEnumerable<UserReportDto>> GetAllReportsAsync(int page = 1, int pageSize = 50)
    {
        var reports = await _context.UserReports
            .Include(ur => ur.ReportedByUser)
            .Include(ur => ur.ReviewedByUser)
            .Include(ur => ur.Post)
                .ThenInclude(p => p!.User)
            .Include(ur => ur.Comment)
                .ThenInclude(c => c!.User)
            .Include(ur => ur.UserReportSystemTags)
                .ThenInclude(urst => urst.SystemTag)
            .OrderByDescending(ur => ur.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return reports.Select(MapToUserReportDto);
    }

    public async Task<UserReportDto?> ReviewReportAsync(int reportId, int reviewedByUserId, ReviewUserReportDto dto)
    {
        try
        {
            // Get the report with related data for messaging
            var report = await _context.UserReports
                .Include(ur => ur.ReportedByUser)
                .Include(ur => ur.Post)
                .Include(ur => ur.Comment)
                .FirstOrDefaultAsync(ur => ur.Id == reportId);

            if (report == null)
                return null;

            var moderator = await _context.Users.FindAsync(reviewedByUserId);
            if (moderator == null)
                return null;

            report.Status = dto.Status;
            report.ReviewedByUserId = reviewedByUserId;
            report.ReviewNotes = dto.ReviewNotes;
            report.ReviewedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send message to reporting user if report was dismissed
            if (dto.Status == UserReportStatus.Dismissed)
            {
                string contentType = report.PostId.HasValue ? "post" : "comment";
                int contentId = report.PostId ?? report.CommentId ?? 0;
                string contentPreview = report.Post?.Content ?? report.Comment?.Content ?? "";

                await _moderationMessageService.SendReportDismissedMessageAsync(
                    report.ReportedByUserId,
                    contentType,
                    contentId,
                    contentPreview,
                    moderator.Username
                );
            }

            // Log the action
            await _auditService.LogActionAsync(
                AuditAction.UserReportReviewed,
                reviewedByUserId,
                $"Reviewed user report {reportId}",
                $"Status: {dto.Status}, Notes: {dto.ReviewNotes}"
            );

            _logger.LogInformation("User {UserId} reviewed report {ReportId} with status {Status}",
                reviewedByUserId, reportId, dto.Status);

            return await GetReportByIdAsync(reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing user report {ReportId}", reportId);
            return null;
        }
    }

    public async Task<UserReportDto?> GetReportByIdAsync(int reportId)
    {
        var report = await _context.UserReports
            .Include(ur => ur.ReportedByUser)
            .Include(ur => ur.ReviewedByUser)
            .Include(ur => ur.Post)
                .ThenInclude(p => p!.User)
            .Include(ur => ur.Comment)
                .ThenInclude(c => c!.User)
            .Include(ur => ur.UserReportSystemTags)
                .ThenInclude(urst => urst.SystemTag)
            .FirstOrDefaultAsync(ur => ur.Id == reportId);

        return report == null ? null : MapToUserReportDto(report);
    }

    private static UserReportDto MapToUserReportDto(UserReport report)
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
            Comment = report.Comment != null ? MapToAdminCommentDto(report.Comment) : null,
            SystemTags = report.UserReportSystemTags.Select(urst => new SystemTagDto
            {
                Id = urst.SystemTag.Id,
                Name = urst.SystemTag.Name,
                Description = urst.SystemTag.Description,
                Category = urst.SystemTag.Category,
                IsVisibleToUsers = urst.SystemTag.IsVisibleToUsers,
                IsActive = urst.SystemTag.IsActive,
                Color = urst.SystemTag.Color,
                Icon = urst.SystemTag.Icon,
                SortOrder = urst.SystemTag.SortOrder,
                CreatedAt = urst.SystemTag.CreatedAt,
                UpdatedAt = urst.SystemTag.UpdatedAt
            }).ToList()
        };
    }

    private static AdminPostDto MapToAdminPostDto(Post post)
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
            ImageFileName = post.ImageFileName,
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
            RepostCount = post.Reposts?.Count ?? 0
        };
    }

    private static AdminCommentDto MapToAdminCommentDto(Post comment)
    {
        if (comment.PostType != PostType.Comment)
            throw new ArgumentException("Post must be of type Comment", nameof(comment));

        // Map group information if comment is in a group
        GroupDto? groupDto = null;
        if (comment.Group != null)
        {
            groupDto = new GroupDto(
                comment.Group.Id,
                comment.Group.Name,
                comment.Group.Description,
                comment.Group.ImageFileName,
                comment.Group.CreatedAt,
                comment.Group.UpdatedAt,
                comment.Group.IsOpen,
                comment.Group.User.MapToUserDto(),
                comment.Group.Members?.Count ?? 0,
                comment.Group.Posts?.Count ?? 0,
                false // IsCurrentUserMember - we don't have this info in admin context
            );
        }

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
            User = comment.User.MapToUserDto(),
            Group = groupDto,
            PostId = comment.ParentId ?? 0 // Comments now use ParentId instead of PostId
        };
    }

    public async Task<bool> HideContentFromReportAsync(int reportId, int moderatorUserId, string reason)
    {
        try
        {
            // Get the report with all related data
            var report = await _context.UserReports
                .Include(ur => ur.ReportedByUser)
                .Include(ur => ur.Post)
                    .ThenInclude(p => p!.User)
                .Include(ur => ur.Comment)
                    .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(ur => ur.Id == reportId);

            if (report == null)
            {
                _logger.LogWarning("Report {ReportId} not found", reportId);
                return false;
            }

            var moderator = await _context.Users.FindAsync(moderatorUserId);
            if (moderator == null)
            {
                _logger.LogWarning("Moderator {ModeratorId} not found", moderatorUserId);
                return false;
            }

            bool contentHidden = false;
            string contentType = "";
            int contentId = 0;
            string contentPreview = "";
            int contentOwnerId = 0;

            // Hide the content using AdminService
            if (report.PostId.HasValue && report.Post != null)
            {
                contentHidden = await _adminService.HidePostAsync(report.PostId.Value, moderatorUserId, reason);
                contentType = "post";
                contentId = report.PostId.Value;
                contentPreview = report.Post.Content;
                contentOwnerId = report.Post.UserId;
            }
            else if (report.CommentId.HasValue && report.Comment != null)
            {
                contentHidden = await _adminService.HideCommentAsync(report.CommentId.Value, moderatorUserId, reason);
                contentType = "comment";
                contentId = report.CommentId.Value;
                contentPreview = report.Comment.Content;
                contentOwnerId = report.Comment.UserId;
            }

            if (!contentHidden)
            {
                _logger.LogWarning("Failed to hide {ContentType} {ContentId} from report {ReportId}", contentType, contentId, reportId);
                return false;
            }

            // Update the report status to ActionTaken
            report.Status = UserReportStatus.ActionTaken;
            report.ReviewedByUserId = moderatorUserId;
            report.ReviewNotes = $"Content hidden: {reason}";
            report.ReviewedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send message to the reporting user
            await _moderationMessageService.SendReportActionTakenMessageAsync(
                report.ReportedByUserId,
                contentType,
                contentId,
                contentPreview,
                reason,
                moderator.Username
            );

            // Log the action
            await _auditService.LogActionAsync(
                AuditAction.UserReportReviewed,
                moderatorUserId,
                $"Hid {contentType} {contentId} from user report {reportId}",
                $"Reason: {reason}, Reporting user notified"
            );

            _logger.LogInformation("Successfully hid {ContentType} {ContentId} from report {ReportId} and notified users",
                contentType, contentId, reportId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding content from report {ReportId}", reportId);
            return false;
        }
    }
}
