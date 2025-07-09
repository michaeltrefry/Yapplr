using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IAuditService
{
    // Core audit logging
    Task LogActionAsync(AuditAction action, int performedByUserId, string? reason = null, string? details = null, 
        int? targetUserId = null, int? targetPostId = null, int? targetCommentId = null, 
        string? ipAddress = null, string? userAgent = null);
    
    // User action logging
    Task LogUserSuspendedAsync(int suspendedUserId, int suspendedByUserId, string reason, DateTime? suspendedUntil, string? ipAddress = null);
    Task LogUserBannedAsync(int bannedUserId, int bannedByUserId, string reason, bool isShadowBan, string? ipAddress = null);
    Task LogUserUnsuspendedAsync(int unsuspendedUserId, int unsuspendedByUserId, string? ipAddress = null);
    Task LogUserUnbannedAsync(int unbannedUserId, int unbannedByUserId, string? ipAddress = null);
    Task LogUserRoleChangedAsync(int targetUserId, int changedByUserId, UserRole oldRole, UserRole newRole, string reason, string? ipAddress = null);
    Task LogUserForcePasswordResetAsync(int targetUserId, int requestedByUserId, string reason, string? ipAddress = null);
    
    // Content action logging
    Task LogPostHiddenAsync(int postId, int hiddenByUserId, string reason, string? ipAddress = null);
    Task LogPostDeletedAsync(int postId, int deletedByUserId, string reason, string? ipAddress = null);
    Task LogPostRestoredAsync(int postId, int restoredByUserId, string? ipAddress = null);
    Task LogPostSystemTagAddedAsync(int postId, int systemTagId, int appliedByUserId, string? reason = null, string? ipAddress = null);
    Task LogPostSystemTagRemovedAsync(int postId, int systemTagId, int removedByUserId, string? ipAddress = null);
    
    Task LogCommentHiddenAsync(int commentId, int hiddenByUserId, string reason, string? ipAddress = null);
    Task LogCommentDeletedAsync(int commentId, int deletedByUserId, string reason, string? ipAddress = null);
    Task LogCommentRestoredAsync(int commentId, int restoredByUserId, string? ipAddress = null);
    Task LogCommentSystemTagAddedAsync(int commentId, int systemTagId, int appliedByUserId, string? reason = null, string? ipAddress = null);
    Task LogCommentSystemTagRemovedAsync(int commentId, int systemTagId, int removedByUserId, string? ipAddress = null);
    
    // System action logging
    Task LogSystemTagCreatedAsync(int systemTagId, int createdByUserId, string? ipAddress = null);
    Task LogSystemTagUpdatedAsync(int systemTagId, int updatedByUserId, string? ipAddress = null);
    Task LogSystemTagDeletedAsync(int systemTagId, int deletedByUserId, string? ipAddress = null);
    
    // Bulk action logging
    Task LogBulkContentDeletedAsync(IEnumerable<int> postIds, int deletedByUserId, string reason, string? ipAddress = null);
    Task LogBulkContentHiddenAsync(IEnumerable<int> postIds, int hiddenByUserId, string reason, string? ipAddress = null);
    Task LogBulkUsersActionedAsync(IEnumerable<int> userIds, AuditAction action, int performedByUserId, string reason, string? ipAddress = null);

    // Appeal action logging
    Task LogAppealCreatedAsync(int appealId, int userId, string appealType, int? targetPostId = null, int? targetCommentId = null, string? ipAddress = null);
    Task LogAppealApprovedAsync(int appealId, int reviewedByUserId, int appealUserId, int? targetPostId = null, int? targetCommentId = null, string? ipAddress = null);
    Task LogAppealDeniedAsync(int appealId, int reviewedByUserId, int appealUserId, int? targetPostId = null, int? targetCommentId = null, string? ipAddress = null);
    Task LogAppealEscalatedAsync(int appealId, int escalatedByUserId, int appealUserId, int? targetPostId = null, int? targetCommentId = null, string? ipAddress = null);
    
    // Security action logging
    Task LogIpBlockedAsync(string ipAddress, int blockedByUserId, string reason, string? userAgent = null);
    Task LogIpUnblockedAsync(string ipAddress, int unblockedByUserId, string? userAgent = null);
    Task LogSecurityIncidentAsync(string incidentType, string details, int reportedByUserId, string? ipAddress = null);
    
    // Query methods
    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(int page = 1, int pageSize = 25, AuditAction? action = null, 
        int? performedByUserId = null, int? targetUserId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<AuditLogDto>> GetUserAuditLogsAsync(int userId, int page = 1, int pageSize = 25);
    Task<IEnumerable<AuditLogDto>> GetPostAuditLogsAsync(int postId, int page = 1, int pageSize = 25);
    Task<IEnumerable<AuditLogDto>> GetCommentAuditLogsAsync(int commentId, int page = 1, int pageSize = 25);
    
    // Analytics
    Task<Dictionary<AuditAction, int>> GetActionCountsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<Dictionary<string, int>> GetModerationStatsAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
