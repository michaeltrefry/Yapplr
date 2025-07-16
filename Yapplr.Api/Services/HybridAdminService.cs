using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

/// <summary>
/// Hybrid admin service that can use either database or InfluxDB for analytics
/// This allows gradual migration from database analytics to external analytics
/// </summary>
public class HybridAdminService : IAdminService
{
    private readonly IAdminService _databaseAdminService;
    private readonly IInfluxAdminAnalyticsService _influxAnalyticsService;
    private readonly ILogger<HybridAdminService> _logger;
    private readonly bool _useInfluxForAnalytics;

    public HybridAdminService(
        IAdminService databaseAdminService,
        IInfluxAdminAnalyticsService influxAnalyticsService,
        ILogger<HybridAdminService> logger,
        IConfiguration configuration)
    {
        _databaseAdminService = databaseAdminService;
        _influxAnalyticsService = influxAnalyticsService;
        _logger = logger;
        _useInfluxForAnalytics = configuration.GetValue<bool>("Analytics:UseInfluxForAdminDashboard", false);

        _logger.LogInformation("Hybrid Admin Service initialized. Use InfluxDB for analytics: {UseInflux}", _useInfluxForAnalytics);
    }

    // Analytics methods - these can use InfluxDB or database
    public async Task<UserGrowthStatsDto> GetUserGrowthStatsAsync(int days = 30)
    {
        if (_useInfluxForAnalytics && await _influxAnalyticsService.IsAvailableAsync())
        {
            _logger.LogDebug("Using InfluxDB for user growth stats");
            return await _influxAnalyticsService.GetUserGrowthStatsAsync(days);
        }

        _logger.LogDebug("Using database for user growth stats");
        return await _databaseAdminService.GetUserGrowthStatsAsync(days);
    }

    public async Task<ContentStatsDto> GetContentStatsAsync(int days = 30)
    {
        if (_useInfluxForAnalytics && await _influxAnalyticsService.IsAvailableAsync())
        {
            _logger.LogDebug("Using InfluxDB for content stats");
            return await _influxAnalyticsService.GetContentStatsAsync(days);
        }

        _logger.LogDebug("Using database for content stats");
        return await _databaseAdminService.GetContentStatsAsync(days);
    }

    public async Task<ModerationTrendsDto> GetModerationTrendsAsync(int days = 30)
    {
        if (_useInfluxForAnalytics && await _influxAnalyticsService.IsAvailableAsync())
        {
            _logger.LogDebug("Using InfluxDB for moderation trends");
            return await _influxAnalyticsService.GetModerationTrendsAsync(days);
        }

        _logger.LogDebug("Using database for moderation trends");
        return await _databaseAdminService.GetModerationTrendsAsync(days);
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        if (_useInfluxForAnalytics && await _influxAnalyticsService.IsAvailableAsync())
        {
            _logger.LogDebug("Using InfluxDB for system health");
            return await _influxAnalyticsService.GetSystemHealthAsync();
        }

        _logger.LogDebug("Using database for system health");
        return await _databaseAdminService.GetSystemHealthAsync();
    }

    public async Task<TopModeratorsDto> GetTopModeratorsAsync(int days = 30, int limit = 10)
    {
        if (_useInfluxForAnalytics && await _influxAnalyticsService.IsAvailableAsync())
        {
            _logger.LogDebug("Using InfluxDB for top moderators");
            return await _influxAnalyticsService.GetTopModeratorsAsync(days, limit);
        }

        _logger.LogDebug("Using database for top moderators");
        return await _databaseAdminService.GetTopModeratorsAsync(days, limit);
    }

    public async Task<ContentTrendsDto> GetContentTrendsAsync(int days = 30)
    {
        if (_useInfluxForAnalytics && await _influxAnalyticsService.IsAvailableAsync())
        {
            _logger.LogDebug("Using InfluxDB for content trends");
            return await _influxAnalyticsService.GetContentTrendsAsync(days);
        }

        _logger.LogDebug("Using database for content trends");
        return await _databaseAdminService.GetContentTrendsAsync(days);
    }

    public async Task<UserEngagementStatsDto> GetUserEngagementStatsAsync(int days = 30)
    {
        if (_useInfluxForAnalytics && await _influxAnalyticsService.IsAvailableAsync())
        {
            _logger.LogDebug("Using InfluxDB for user engagement stats");
            return await _influxAnalyticsService.GetUserEngagementStatsAsync(days);
        }

        _logger.LogDebug("Using database for user engagement stats");
        return await _databaseAdminService.GetUserEngagementStatsAsync(days);
    }

    // All other methods delegate to the database admin service
    public Task<IEnumerable<SystemTagDto>> GetSystemTagsAsync(SystemTagCategory? category = null, bool? isActive = null)
        => _databaseAdminService.GetSystemTagsAsync(category, isActive);

    public Task<SystemTagDto?> GetSystemTagAsync(int id)
        => _databaseAdminService.GetSystemTagAsync(id);

    public Task<SystemTagDto> CreateSystemTagAsync(CreateSystemTagDto createDto)
        => _databaseAdminService.CreateSystemTagAsync(createDto);

    public Task<SystemTagDto?> UpdateSystemTagAsync(int id, UpdateSystemTagDto updateDto)
        => _databaseAdminService.UpdateSystemTagAsync(id, updateDto);

    public Task<bool> DeleteSystemTagAsync(int id)
        => _databaseAdminService.DeleteSystemTagAsync(id);

    public Task<IEnumerable<AdminPostDto>> GetPostsForModerationAsync(int page = 1, int pageSize = 25, bool? isHidden = null)
        => _databaseAdminService.GetPostsForModerationAsync(page, pageSize, isHidden);

    public Task<IEnumerable<AdminCommentDto>> GetCommentsForModerationAsync(int page = 1, int pageSize = 25, bool? isHidden = null)
        => _databaseAdminService.GetCommentsForModerationAsync(page, pageSize, isHidden);

    public Task<AdminPostDto?> GetPostForModerationAsync(int postId)
        => _databaseAdminService.GetPostForModerationAsync(postId);

    public Task<AdminCommentDto?> GetCommentForModerationAsync(int commentId)
        => _databaseAdminService.GetCommentForModerationAsync(commentId);

    public Task<bool> HidePostAsync(int postId, int hiddenByUserId, string reason)
        => _databaseAdminService.HidePostAsync(postId, hiddenByUserId, reason);

    public Task<bool> UnhidePostAsync(int postId)
        => _databaseAdminService.UnhidePostAsync(postId);

    public Task<bool> HideCommentAsync(int commentId, int hiddenByUserId, string reason)
        => _databaseAdminService.HideCommentAsync(commentId, hiddenByUserId, reason);

    public Task<bool> UnhideCommentAsync(int commentId)
        => _databaseAdminService.UnhideCommentAsync(commentId);

    public Task<IEnumerable<UserDto>> GetUsersAsync(int page = 1, int pageSize = 25, UserStatus? status = null, string? searchTerm = null)
        => _databaseAdminService.GetUsersAsync(page, pageSize, status, searchTerm);

    public Task<UserDto?> GetUserAsync(int id)
        => _databaseAdminService.GetUserAsync(id);

    public Task<bool> SuspendUserAsync(int userId, int suspendedByUserId, string reason, DateTime? suspendedUntil = null)
        => _databaseAdminService.SuspendUserAsync(userId, suspendedByUserId, reason, suspendedUntil);

    public Task<bool> UnsuspendUserAsync(int userId)
        => _databaseAdminService.UnsuspendUserAsync(userId);

    public Task<bool> BanUserAsync(int userId, int bannedByUserId, string reason)
        => _databaseAdminService.BanUserAsync(userId, bannedByUserId, reason);

    public Task<bool> UnbanUserAsync(int userId)
        => _databaseAdminService.UnbanUserAsync(userId);

    public Task<ModerationStatsDto> GetModerationStatsAsync()
        => _databaseAdminService.GetModerationStatsAsync();

    public Task<ContentQueueDto> GetContentQueueAsync()
        => _databaseAdminService.GetContentQueueAsync();

    public Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(int page = 1, int pageSize = 25, AuditAction? action = null, int? performedByUserId = null, int? targetUserId = null)
        => _databaseAdminService.GetAuditLogsAsync(page, pageSize, action, performedByUserId, targetUserId);

    public Task<IEnumerable<UserReportDto>> GetUserReportsAsync(int page = 1, int pageSize = 25, UserReportStatus? status = null, UserReportType? type = null)
        => _databaseAdminService.GetUserReportsAsync(page, pageSize, status, type);

    public Task<UserReportDto?> GetUserReportAsync(int reportId)
        => _databaseAdminService.GetUserReportAsync(reportId);

    public Task<bool> ResolveUserReportAsync(int reportId, int resolvedByUserId, string resolution, bool hideContent = false)
        => _databaseAdminService.ResolveUserReportAsync(reportId, resolvedByUserId, resolution, hideContent);

    public Task<IEnumerable<AiSuggestedTagDto>> GetPendingAiSuggestionsAsync(int? postId = null, int? commentId = null, int page = 1, int pageSize = 25)
        => _databaseAdminService.GetPendingAiSuggestionsAsync(postId, commentId, page, pageSize);

    public Task<bool> ApproveAiSuggestedTagAsync(int suggestedTagId, int approvedByUserId, string? reason = null)
        => _databaseAdminService.ApproveAiSuggestedTagAsync(suggestedTagId, approvedByUserId, reason);

    public Task<bool> RejectAiSuggestedTagAsync(int suggestedTagId, int approvedByUserId, string? reason = null)
        => _databaseAdminService.RejectAiSuggestedTagAsync(suggestedTagId, approvedByUserId, reason);

    public Task<bool> BulkApproveAiSuggestedTagsAsync(IEnumerable<int> suggestedTagIds, int approvedByUserId, string? reason = null)
        => _databaseAdminService.BulkApproveAiSuggestedTagsAsync(suggestedTagIds, approvedByUserId, reason);

    public Task<bool> BulkRejectAiSuggestedTagsAsync(IEnumerable<int> suggestedTagIds, int approvedByUserId, string? reason = null)
        => _databaseAdminService.BulkRejectAiSuggestedTagsAsync(suggestedTagIds, approvedByUserId, reason);

    public Task<IEnumerable<UserTrustScoreDto>> GetUserTrustScoresAsync(int page = 1, int pageSize = 25, float? minScore = null, float? maxScore = null)
        => _databaseAdminService.GetUserTrustScoresAsync(page, pageSize, minScore, maxScore);

    public Task<UserTrustScoreDto?> GetUserTrustScoreAsync(int userId)
        => _databaseAdminService.GetUserTrustScoreAsync(userId);

    public Task<IEnumerable<TrustScoreHistoryDto>> GetUserTrustScoreHistoryAsync(int userId, int page = 1, int pageSize = 25)
        => _databaseAdminService.GetUserTrustScoreHistoryAsync(userId, page, pageSize);

    public Task<TrustScoreStatsDto> GetTrustScoreStatisticsAsync()
        => _databaseAdminService.GetTrustScoreStatisticsAsync();

    public Task<TrustScoreFactorsDto?> GetUserTrustScoreFactorsAsync(int userId)
        => _databaseAdminService.GetUserTrustScoreFactorsAsync(userId);

    public Task<bool> UpdateUserTrustScoreAsync(int userId, int adjustedByUserId, UpdateTrustScoreDto updateDto)
        => _databaseAdminService.UpdateUserTrustScoreAsync(userId, adjustedByUserId, updateDto);

    public Task<AdminUserDetailsDto?> GetUserDetailsAsync(int userId)
        => _databaseAdminService.GetUserDetailsAsync(userId);
}
