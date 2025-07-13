using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IAdminService
{
    // System Tags
    Task<IEnumerable<SystemTagDto>> GetSystemTagsAsync(SystemTagCategory? category = null, bool? isActive = null);
    Task<SystemTagDto?> GetSystemTagAsync(int id);
    Task<SystemTagDto> CreateSystemTagAsync(CreateSystemTagDto createDto);
    Task<SystemTagDto?> UpdateSystemTagAsync(int id, UpdateSystemTagDto updateDto);
    Task<bool> DeleteSystemTagAsync(int id);
    
    // Content Moderation
    Task<IEnumerable<AdminPostDto>> GetPostsForModerationAsync(int page = 1, int pageSize = 25, bool? isHidden = null);
    Task<IEnumerable<AdminCommentDto>> GetCommentsForModerationAsync(int page = 1, int pageSize = 25, bool? isHidden = null);
    Task<AdminPostDto?> GetPostForModerationAsync(int postId);
    Task<AdminCommentDto?> GetCommentForModerationAsync(int commentId);
    
    Task<bool> HidePostAsync(int postId, int hiddenByUserId, string reason);
    Task<bool> UnhidePostAsync(int postId);

    
    Task<bool> HideCommentAsync(int commentId, int hiddenByUserId, string reason);
    Task<bool> UnhideCommentAsync(int commentId);

    
    Task<bool> ApplySystemTagToPostAsync(int postId, int systemTagId, int appliedByUserId, string? reason = null);
    Task<bool> RemoveSystemTagFromPostAsync(int postId, int systemTagId, int removedByUserId);
    Task<bool> ApplySystemTagToCommentAsync(int commentId, int systemTagId, int appliedByUserId, string? reason = null);
    Task<bool> RemoveSystemTagFromCommentAsync(int commentId, int systemTagId, int removedByUserId);
    
    // Bulk Actions
    Task<int> BulkHidePostsAsync(IEnumerable<int> postIds, int hiddenByUserId, string reason);

    Task<int> BulkApplySystemTagAsync(IEnumerable<int> postIds, int systemTagId, int appliedByUserId, string? reason = null);
    
    // Analytics and Reporting
    Task<ModerationStatsDto> GetModerationStatsAsync();
    Task<ContentQueueDto> GetContentQueueAsync();
    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(int page = 1, int pageSize = 25, AuditAction? action = null, int? performedByUserId = null, int? targetUserId = null);

    // Enhanced Analytics
    Task<UserGrowthStatsDto> GetUserGrowthStatsAsync(int days = 30);
    Task<ContentStatsDto> GetContentStatsAsync(int days = 30);
    Task<ModerationTrendsDto> GetModerationTrendsAsync(int days = 30);
    Task<SystemHealthDto> GetSystemHealthAsync();
    Task<TopModeratorsDto> GetTopModeratorsAsync(int days = 30, int limit = 10);
    Task<ContentTrendsDto> GetContentTrendsAsync(int days = 30);
    Task<UserEngagementStatsDto> GetUserEngagementStatsAsync(int days = 30);
    
    // User Appeals
    Task<IEnumerable<UserAppealDto>> GetUserAppealsAsync(int page = 1, int pageSize = 25, AppealStatus? status = null, AppealType? type = null, int? userId = null);
    Task<UserAppealDto?> GetUserAppealAsync(int appealId);
    Task<UserAppealDto> CreateUserAppealAsync(int userId, CreateAppealDto createDto);
    Task<UserAppealDto?> ReviewUserAppealAsync(int appealId, int reviewedByUserId, ReviewAppealDto reviewDto);
    
    // AI Suggested Tags Management
    Task<IEnumerable<AiSuggestedTagDto>> GetPendingAiSuggestionsAsync(int? postId = null, int? commentId = null, int page = 1, int pageSize = 25);
    Task<bool> ApproveAiSuggestedTagAsync(int suggestedTagId, int approvedByUserId, string? reason = null);
    Task<bool> RejectAiSuggestedTagAsync(int suggestedTagId, int approvedByUserId, string? reason = null);
    Task<bool> BulkApproveAiSuggestedTagsAsync(IEnumerable<int> suggestedTagIds, int approvedByUserId, string? reason = null);
    Task<bool> BulkRejectAiSuggestedTagsAsync(IEnumerable<int> suggestedTagIds, int approvedByUserId, string? reason = null);

    // System Administration
    Task<bool> CreateSystemAnnouncementAsync(string title, string content, int createdByUserId, DateTime? expiresAt = null);
    Task<bool> ToggleFeatureFlagAsync(string featureName, bool isEnabled, int changedByUserId);
    Task<Dictionary<string, bool>> GetFeatureFlagsAsync();

    // Trust Score Management
    Task<IEnumerable<UserTrustScoreDto>> GetUserTrustScoresAsync(int page = 1, int pageSize = 25, float? minScore = null, float? maxScore = null);
    Task<UserTrustScoreDto?> GetUserTrustScoreAsync(int userId);
    Task<IEnumerable<TrustScoreHistoryDto>> GetUserTrustScoreHistoryAsync(int userId, int page = 1, int pageSize = 25);
    Task<TrustScoreStatsDto> GetTrustScoreStatisticsAsync();
    Task<TrustScoreFactorsDto?> GetUserTrustScoreFactorsAsync(int userId);
    Task<bool> UpdateUserTrustScoreAsync(int userId, int adjustedByUserId, UpdateTrustScoreDto updateDto);
}
