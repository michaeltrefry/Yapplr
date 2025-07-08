using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class UserReportService : IUserReportService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<UserReportService> _logger;
    private readonly IAuditService _auditService;

    public UserReportService(
        YapplrDbContext context,
        ILogger<UserReportService> logger,
        IAuditService auditService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<UserReportDto?> CreateReportAsync(int reportedByUserId, CreateUserReportDto dto)
    {
        try
        {
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
                var commentExists = await _context.Comments.AnyAsync(c => c.Id == dto.CommentId.Value);
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
            var report = await _context.UserReports.FindAsync(reportId);
            if (report == null)
                return null;

            report.Status = dto.Status;
            report.ReviewedByUserId = reviewedByUserId;
            report.ReviewNotes = dto.ReviewNotes;
            report.ReviewedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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
            User = new UserDto(
                post.User.Id,
                post.User.Email,
                post.User.Username,
                post.User.Bio,
                post.User.Birthday,
                post.User.Pronouns,
                post.User.Tagline,
                post.User.ProfileImageFileName,
                post.User.CreatedAt,
                post.User.FcmToken,
                post.User.EmailVerified,
                post.User.Role,
                post.User.Status
            ),
            LikeCount = post.Likes?.Count ?? 0,
            CommentCount = post.Comments?.Count ?? 0,
            RepostCount = post.Reposts?.Count ?? 0
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
            User = new UserDto(
                comment.User.Id,
                comment.User.Email,
                comment.User.Username,
                comment.User.Bio,
                comment.User.Birthday,
                comment.User.Pronouns,
                comment.User.Tagline,
                comment.User.ProfileImageFileName,
                comment.User.CreatedAt,
                comment.User.FcmToken,
                comment.User.EmailVerified,
                comment.User.Role,
                comment.User.Status
            ),
            PostId = comment.PostId
        };
    }
}
