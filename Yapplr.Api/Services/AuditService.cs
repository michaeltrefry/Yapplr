using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class AuditService : IAuditService
{
    private readonly YapplrDbContext _context;

    public AuditService(YapplrDbContext context)
    {
        _context = context;
    }

    public async Task LogActionAsync(AuditAction action, int performedByUserId, string? reason = null, string? details = null,
        int? targetUserId = null, int? targetPostId = null, int? targetCommentId = null,
        string? ipAddress = null, string? userAgent = null)
    {
        var auditLog = new AuditLog
        {
            Action = action,
            PerformedByUserId = performedByUserId,
            TargetUserId = targetUserId,
            TargetPostId = targetPostId,
            TargetCommentId = targetCommentId,
            Reason = reason,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task LogUserSuspendedAsync(int suspendedUserId, int suspendedByUserId, string reason, DateTime? suspendedUntil, string? ipAddress = null)
    {
        var details = suspendedUntil.HasValue ? $"Suspended until: {suspendedUntil:yyyy-MM-dd HH:mm:ss} UTC" : "Suspended indefinitely";
        await LogActionAsync(AuditAction.UserSuspended, suspendedByUserId, reason, details, suspendedUserId, ipAddress: ipAddress);
    }

    public async Task LogUserBannedAsync(int bannedUserId, int bannedByUserId, string reason, bool isShadowBan, string? ipAddress = null)
    {
        var action = isShadowBan ? AuditAction.UserShadowBanned : AuditAction.UserBanned;
        await LogActionAsync(action, bannedByUserId, reason, targetUserId: bannedUserId, ipAddress: ipAddress);
    }

    public async Task LogUserUnsuspendedAsync(int unsuspendedUserId, int unsuspendedByUserId, string? ipAddress = null)
    {
        await LogActionAsync(AuditAction.UserUnsuspended, unsuspendedByUserId, targetUserId: unsuspendedUserId, ipAddress: ipAddress);
    }

    public async Task LogUserUnbannedAsync(int unbannedUserId, int unbannedByUserId, string? ipAddress = null)
    {
        await LogActionAsync(AuditAction.UserUnbanned, unbannedByUserId, targetUserId: unbannedUserId, ipAddress: ipAddress);
    }

    public async Task LogUserRoleChangedAsync(int targetUserId, int changedByUserId, UserRole oldRole, UserRole newRole, string reason, string? ipAddress = null)
    {
        var details = $"Role changed from {oldRole} to {newRole}";
        await LogActionAsync(AuditAction.UserRoleChanged, changedByUserId, reason, details, targetUserId, ipAddress: ipAddress);
    }

    public async Task LogUserForcePasswordResetAsync(int targetUserId, int requestedByUserId, string reason, string? ipAddress = null)
    {
        await LogActionAsync(AuditAction.UserForcePasswordReset, requestedByUserId, reason, targetUserId: targetUserId, ipAddress: ipAddress);
    }

    public async Task LogPostHiddenAsync(int postId, int hiddenByUserId, string reason, string? ipAddress = null)
    {
        await LogActionAsync(AuditAction.PostHidden, hiddenByUserId, reason, targetPostId: postId, ipAddress: ipAddress);
    }

    public async Task LogPostDeletedAsync(int postId, int deletedByUserId, string reason, string? ipAddress = null)
    {
        await LogActionAsync(AuditAction.PostDeleted, deletedByUserId, reason, targetPostId: postId, ipAddress: ipAddress);
    }

    public async Task LogPostRestoredAsync(int postId, int restoredByUserId, string? ipAddress = null)
    {
        await LogActionAsync(AuditAction.PostRestored, restoredByUserId, targetPostId: postId, ipAddress: ipAddress);
    }

    public async Task LogPostSystemTagAddedAsync(int postId, int systemTagId, int appliedByUserId, string? reason = null, string? ipAddress = null)
    {
        var details = $"System tag ID: {systemTagId}";
        await LogActionAsync(AuditAction.PostSystemTagAdded, appliedByUserId, reason, details, targetPostId: postId, ipAddress: ipAddress);
    }

    public async Task LogPostSystemTagRemovedAsync(int postId, int systemTagId, int removedByUserId, string? ipAddress = null)
    {
        var details = $"System tag ID: {systemTagId}";
        await LogActionAsync(AuditAction.PostSystemTagRemoved, removedByUserId, details: details, targetPostId: postId, ipAddress: ipAddress);
    }

    public async Task LogCommentHiddenAsync(int commentId, int hiddenByUserId, string reason, string? ipAddress = null)
    {
        await LogActionAsync(AuditAction.CommentHidden, hiddenByUserId, reason, targetCommentId: commentId, ipAddress: ipAddress);
    }

    public async Task LogCommentDeletedAsync(int commentId, int deletedByUserId, string reason, string? ipAddress = null)
    {
        await LogActionAsync(AuditAction.CommentDeleted, deletedByUserId, reason, targetCommentId: commentId, ipAddress: ipAddress);
    }

    public async Task LogCommentRestoredAsync(int commentId, int restoredByUserId, string? ipAddress = null)
    {
        await LogActionAsync(AuditAction.CommentRestored, restoredByUserId, targetCommentId: commentId, ipAddress: ipAddress);
    }

    public async Task LogCommentSystemTagAddedAsync(int commentId, int systemTagId, int appliedByUserId, string? reason = null, string? ipAddress = null)
    {
        var details = $"System tag ID: {systemTagId}";
        await LogActionAsync(AuditAction.CommentSystemTagAdded, appliedByUserId, reason, details, targetCommentId: commentId, ipAddress: ipAddress);
    }

    public async Task LogCommentSystemTagRemovedAsync(int commentId, int systemTagId, int removedByUserId, string? ipAddress = null)
    {
        var details = $"System tag ID: {systemTagId}";
        await LogActionAsync(AuditAction.CommentSystemTagRemoved, removedByUserId, details: details, targetCommentId: commentId, ipAddress: ipAddress);
    }

    public async Task LogSystemTagCreatedAsync(int systemTagId, int createdByUserId, string? ipAddress = null)
    {
        var details = $"System tag ID: {systemTagId}";
        await LogActionAsync(AuditAction.SystemTagCreated, createdByUserId, details: details, ipAddress: ipAddress);
    }

    public async Task LogSystemTagUpdatedAsync(int systemTagId, int updatedByUserId, string? ipAddress = null)
    {
        var details = $"System tag ID: {systemTagId}";
        await LogActionAsync(AuditAction.SystemTagUpdated, updatedByUserId, details: details, ipAddress: ipAddress);
    }

    public async Task LogSystemTagDeletedAsync(int systemTagId, int deletedByUserId, string? ipAddress = null)
    {
        var details = $"System tag ID: {systemTagId}";
        await LogActionAsync(AuditAction.SystemTagDeleted, deletedByUserId, details: details, ipAddress: ipAddress);
    }

    public async Task LogBulkContentDeletedAsync(IEnumerable<int> postIds, int deletedByUserId, string reason, string? ipAddress = null)
    {
        var details = $"Post IDs: {string.Join(", ", postIds)}";
        await LogActionAsync(AuditAction.BulkContentDeleted, deletedByUserId, reason, details, ipAddress: ipAddress);
    }

    public async Task LogBulkContentHiddenAsync(IEnumerable<int> postIds, int hiddenByUserId, string reason, string? ipAddress = null)
    {
        var details = $"Post IDs: {string.Join(", ", postIds)}";
        await LogActionAsync(AuditAction.BulkContentHidden, hiddenByUserId, reason, details, ipAddress: ipAddress);
    }

    public async Task LogBulkUsersActionedAsync(IEnumerable<int> userIds, AuditAction action, int performedByUserId, string reason, string? ipAddress = null)
    {
        var details = $"User IDs: {string.Join(", ", userIds)}";
        await LogActionAsync(action, performedByUserId, reason, details, ipAddress: ipAddress);
    }

    public async Task LogIpBlockedAsync(string ipAddress, int blockedByUserId, string reason, string? userAgent = null)
    {
        var details = $"IP: {ipAddress}";
        await LogActionAsync(AuditAction.IpBlocked, blockedByUserId, reason, details, ipAddress: ipAddress, userAgent: userAgent);
    }

    public async Task LogIpUnblockedAsync(string ipAddress, int unblockedByUserId, string? userAgent = null)
    {
        var details = $"IP: {ipAddress}";
        await LogActionAsync(AuditAction.IpUnblocked, unblockedByUserId, details: details, ipAddress: ipAddress, userAgent: userAgent);
    }

    public async Task LogSecurityIncidentAsync(string incidentType, string details, int reportedByUserId, string? ipAddress = null)
    {
        var reason = $"Incident type: {incidentType}";
        await LogActionAsync(AuditAction.SecurityIncidentReported, reportedByUserId, reason, details, ipAddress: ipAddress);
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(int page = 1, int pageSize = 25, AuditAction? action = null,
        int? performedByUserId = null, int? targetUserId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditLogs
            .Include(a => a.PerformedByUser)
            .Include(a => a.TargetUser)
            .AsQueryable();

        if (action.HasValue)
            query = query.Where(a => a.Action == action.Value);

        if (performedByUserId.HasValue)
            query = query.Where(a => a.PerformedByUserId == performedByUserId.Value);

        if (targetUserId.HasValue)
            query = query.Where(a => a.TargetUserId == targetUserId.Value);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        var auditLogs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return auditLogs.Select(a => new AuditLogDto
        {
            Id = a.Id,
            Action = a.Action,
            PerformedByUsername = a.PerformedByUser.Username,
            TargetUsername = a.TargetUser?.Username,
            TargetPostId = a.TargetPostId,
            TargetCommentId = a.TargetCommentId,
            Reason = a.Reason,
            Details = a.Details,
            IpAddress = a.IpAddress,
            CreatedAt = a.CreatedAt
        });
    }

    public async Task<IEnumerable<AuditLogDto>> GetUserAuditLogsAsync(int userId, int page = 1, int pageSize = 25)
    {
        return await GetAuditLogsAsync(page, pageSize, targetUserId: userId);
    }

    public async Task<IEnumerable<AuditLogDto>> GetPostAuditLogsAsync(int postId, int page = 1, int pageSize = 25)
    {
        var auditLogs = await _context.AuditLogs
            .Include(a => a.PerformedByUser)
            .Include(a => a.TargetUser)
            .Where(a => a.TargetPostId == postId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return auditLogs.Select(a => new AuditLogDto
        {
            Id = a.Id,
            Action = a.Action,
            PerformedByUsername = a.PerformedByUser.Username,
            TargetUsername = a.TargetUser?.Username,
            TargetPostId = a.TargetPostId,
            TargetCommentId = a.TargetCommentId,
            Reason = a.Reason,
            Details = a.Details,
            IpAddress = a.IpAddress,
            CreatedAt = a.CreatedAt
        });
    }

    public async Task<IEnumerable<AuditLogDto>> GetCommentAuditLogsAsync(int commentId, int page = 1, int pageSize = 25)
    {
        var auditLogs = await _context.AuditLogs
            .Include(a => a.PerformedByUser)
            .Include(a => a.TargetUser)
            .Where(a => a.TargetCommentId == commentId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return auditLogs.Select(a => new AuditLogDto
        {
            Id = a.Id,
            Action = a.Action,
            PerformedByUsername = a.PerformedByUser.Username,
            TargetUsername = a.TargetUser?.Username,
            TargetPostId = a.TargetPostId,
            TargetCommentId = a.TargetCommentId,
            Reason = a.Reason,
            Details = a.Details,
            IpAddress = a.IpAddress,
            CreatedAt = a.CreatedAt
        });
    }

    public async Task<Dictionary<AuditAction, int>> GetActionCountsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        return await query
            .GroupBy(a => a.Action)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetModerationStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        var actionCounts = await query
            .GroupBy(a => a.Action)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        return new Dictionary<string, int>
        {
            ["UserActions"] = actionCounts.Where(kvp => (int)kvp.Key >= 100 && (int)kvp.Key < 200).Sum(kvp => kvp.Value),
            ["ContentActions"] = actionCounts.Where(kvp => (int)kvp.Key >= 200 && (int)kvp.Key < 300).Sum(kvp => kvp.Value),
            ["SystemActions"] = actionCounts.Where(kvp => (int)kvp.Key >= 300 && (int)kvp.Key < 400).Sum(kvp => kvp.Value),
            ["SecurityActions"] = actionCounts.Where(kvp => (int)kvp.Key >= 400 && (int)kvp.Key < 500).Sum(kvp => kvp.Value),
            ["BulkActions"] = actionCounts.Where(kvp => (int)kvp.Key >= 500).Sum(kvp => kvp.Value)
        };
    }
}
