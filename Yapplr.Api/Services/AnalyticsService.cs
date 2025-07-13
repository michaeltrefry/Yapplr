using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for recording and analyzing user activities and content engagement
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AnalyticsService(
        YapplrDbContext context,
        ILogger<AnalyticsService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task TrackUserActivityAsync(int userId, ActivityType activityType, 
        string? targetEntityType = null, int? targetEntityId = null, 
        string? metadata = null, string? sessionId = null, 
        int? durationMs = null, bool? success = null, string? errorMessage = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

            var activity = new UserActivity
            {
                UserId = userId,
                ActivityType = activityType,
                TargetEntityType = targetEntityType,
                TargetEntityId = targetEntityId,
                Metadata = metadata,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                SessionId = sessionId,
                DurationMs = durationMs,
                Success = success,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Tracked user activity: User {UserId} performed {ActivityType}", 
                userId, activityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track user activity for user {UserId}: {ActivityType}", 
                userId, activityType);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }

    public async Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(int userId, 
        DateTime? fromDate = null, DateTime? toDate = null, 
        ActivityType? activityType = null, int limit = 100)
    {
        var query = _context.UserActivities
            .Where(ua => ua.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(ua => ua.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ua => ua.CreatedAt <= toDate.Value);

        if (activityType.HasValue)
            query = query.Where(ua => ua.ActivityType == activityType.Value);

        return await query
            .OrderByDescending(ua => ua.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Dictionary<ActivityType, int>> GetUserActivitySummaryAsync(int userId, 
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.UserActivities
            .Where(ua => ua.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(ua => ua.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ua => ua.CreatedAt <= toDate.Value);

        return await query
            .GroupBy(ua => ua.ActivityType)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task TrackContentEngagementAsync(int userId, ContentType contentType, int contentId, 
        EngagementType engagementType, int? contentOwnerId = null, 
        string? source = null, string? metadata = null, string? sessionId = null, 
        int? durationMs = null, int? position = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var deviceType = GetDeviceType(httpContext?.Request?.Headers["User-Agent"].ToString());
            var platform = GetPlatform(httpContext?.Request?.Headers["User-Agent"].ToString());

            var engagement = new ContentEngagement
            {
                UserId = userId,
                ContentType = contentType,
                ContentId = contentId,
                ContentOwnerId = contentOwnerId,
                EngagementType = engagementType,
                Metadata = metadata,
                Source = source,
                DeviceType = deviceType,
                Platform = platform,
                DurationMs = durationMs,
                Position = position,
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContentEngagements.Add(engagement);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Tracked content engagement: User {UserId} {EngagementType} {ContentType} {ContentId}", 
                userId, engagementType, contentType, contentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track content engagement for user {UserId}: {EngagementType} {ContentType} {ContentId}", 
                userId, engagementType, contentType, contentId);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }

    public async Task<IEnumerable<ContentEngagement>> GetContentEngagementsAsync(
        ContentType? contentType = null, int? contentId = null, 
        EngagementType? engagementType = null, DateTime? fromDate = null, 
        DateTime? toDate = null, int limit = 100)
    {
        var query = _context.ContentEngagements.AsQueryable();

        if (contentType.HasValue)
            query = query.Where(ce => ce.ContentType == contentType.Value);

        if (contentId.HasValue)
            query = query.Where(ce => ce.ContentId == contentId.Value);

        if (engagementType.HasValue)
            query = query.Where(ce => ce.EngagementType == engagementType.Value);

        if (fromDate.HasValue)
            query = query.Where(ce => ce.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ce => ce.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(ce => ce.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Dictionary<EngagementType, int>> GetContentEngagementSummaryAsync(
        ContentType contentType, int contentId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.ContentEngagements
            .Where(ce => ce.ContentType == contentType && ce.ContentId == contentId);

        if (fromDate.HasValue)
            query = query.Where(ce => ce.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ce => ce.CreatedAt <= toDate.Value);

        return await query
            .GroupBy(ce => ce.EngagementType)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    private string GetDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "unknown";

        userAgent = userAgent.ToLowerInvariant();
        
        if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
            return "mobile";
        
        if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
            return "tablet";
        
        return "desktop";
    }

    private string GetPlatform(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "unknown";

        userAgent = userAgent.ToLowerInvariant();
        
        if (userAgent.Contains("android"))
            return "android";
        
        if (userAgent.Contains("iphone") || userAgent.Contains("ipad") || userAgent.Contains("ios"))
            return "ios";
        
        return "web";
    }

    public async Task UpdateUserTrustScoreAsync(int userId, float scoreChange,
        TrustScoreChangeReason reason, string? details = null,
        string? relatedEntityType = null, int? relatedEntityId = null,
        int? triggeredByUserId = null, string? calculatedBy = null,
        bool isAutomatic = true, float? confidence = null)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot update trust score for non-existent user {UserId}", userId);
                return;
            }

            var previousScore = user.TrustScore ?? 1.0f;
            var newScore = Math.Max(0.0f, Math.Min(1.0f, previousScore + scoreChange));

            // Update user's trust score
            user.TrustScore = newScore;

            // Record the change in history
            var history = new UserTrustScoreHistory
            {
                UserId = userId,
                PreviousScore = previousScore,
                NewScore = newScore,
                ScoreChange = scoreChange,
                Reason = reason,
                Details = details,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                TriggeredByUserId = triggeredByUserId,
                CalculatedBy = calculatedBy,
                IsAutomatic = isAutomatic,
                Confidence = confidence,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserTrustScoreHistories.Add(history);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated trust score for user {UserId}: {PreviousScore} -> {NewScore} (change: {ScoreChange}, reason: {Reason})",
                userId, previousScore, newScore, scoreChange, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update trust score for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<UserTrustScoreHistory>> GetUserTrustScoreHistoryAsync(int userId,
        DateTime? fromDate = null, DateTime? toDate = null, int limit = 50)
    {
        var query = _context.UserTrustScoreHistories
            .Where(h => h.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(h => h.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(h => h.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<float> GetCurrentUserTrustScoreAsync(int userId)
    {
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TrustScore)
            .FirstOrDefaultAsync();

        return user ?? 1.0f;
    }

    public async Task TrackTagActionAsync(int tagId, TagAction action, int? userId = null,
        string? relatedContentType = null, int? relatedContentId = null,
        string? source = null, string? metadata = null, string? sessionId = null,
        int? position = null, bool? wasSuggested = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var deviceType = GetDeviceType(httpContext?.Request?.Headers["User-Agent"].ToString());
            var platform = GetPlatform(httpContext?.Request?.Headers["User-Agent"].ToString());

            var tagAnalytics = new TagAnalytics
            {
                TagId = tagId,
                UserId = userId,
                Action = action,
                RelatedContentType = relatedContentType,
                RelatedContentId = relatedContentId,
                Source = source,
                Metadata = metadata,
                SessionId = sessionId,
                DeviceType = deviceType,
                Platform = platform,
                Position = position,
                WasSuggested = wasSuggested,
                CreatedAt = DateTime.UtcNow
            };

            _context.TagAnalytics.Add(tagAnalytics);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Tracked tag action: Tag {TagId} {Action} by user {UserId}",
                tagId, action, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track tag action for tag {TagId}: {Action}",
                tagId, action);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }

    public async Task<IEnumerable<TagAnalytics>> GetTagAnalyticsAsync(int tagId,
        DateTime? fromDate = null, DateTime? toDate = null,
        TagAction? action = null, int limit = 100)
    {
        var query = _context.TagAnalytics
            .Where(ta => ta.TagId == tagId);

        if (fromDate.HasValue)
            query = query.Where(ta => ta.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ta => ta.CreatedAt <= toDate.Value);

        if (action.HasValue)
            query = query.Where(ta => ta.Action == action.Value);

        return await query
            .OrderByDescending(ta => ta.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Dictionary<TagAction, int>> GetTagActionSummaryAsync(int tagId,
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.TagAnalytics
            .Where(ta => ta.TagId == tagId);

        if (fromDate.HasValue)
            query = query.Where(ta => ta.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ta => ta.CreatedAt <= toDate.Value);

        return await query
            .GroupBy(ta => ta.Action)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task RecordPerformanceMetricAsync(MetricType metricType, double value, string unit,
        string source, string? operation = null, string? tags = null,
        string? instanceId = null, string? environment = null, string? version = null,
        bool? success = null, string? errorMessage = null, int? userId = null,
        string? sessionId = null)
    {
        try
        {
            var metric = new PerformanceMetric
            {
                MetricType = metricType,
                Value = value,
                Unit = unit,
                Source = source,
                Operation = operation,
                Tags = tags,
                InstanceId = instanceId,
                Environment = environment,
                Version = version,
                Success = success,
                ErrorMessage = errorMessage,
                UserId = userId,
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.PerformanceMetrics.Add(metric);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Recorded performance metric: {MetricType} = {Value} {Unit} from {Source}",
                metricType, value, unit, source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record performance metric: {MetricType} from {Source}",
                metricType, source);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }

    public async Task<IEnumerable<PerformanceMetric>> GetPerformanceMetricsAsync(
        MetricType? metricType = null, string? source = null,
        DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
    {
        var query = _context.PerformanceMetrics.AsQueryable();

        if (metricType.HasValue)
            query = query.Where(pm => pm.MetricType == metricType.Value);

        if (!string.IsNullOrEmpty(source))
            query = query.Where(pm => pm.Source == source);

        if (fromDate.HasValue)
            query = query.Where(pm => pm.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(pm => pm.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(pm => pm.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Dictionary<string, double>> GetPerformanceMetricSummaryAsync(
        MetricType metricType, string? source = null,
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.PerformanceMetrics
            .Where(pm => pm.MetricType == metricType);

        if (!string.IsNullOrEmpty(source))
            query = query.Where(pm => pm.Source == source);

        if (fromDate.HasValue)
            query = query.Where(pm => pm.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(pm => pm.CreatedAt <= toDate.Value);

        var metrics = await query.ToListAsync();

        if (!metrics.Any())
            return new Dictionary<string, double>();

        return new Dictionary<string, double>
        {
            ["count"] = metrics.Count,
            ["average"] = metrics.Average(m => m.Value),
            ["min"] = metrics.Min(m => m.Value),
            ["max"] = metrics.Max(m => m.Value),
            ["sum"] = metrics.Sum(m => m.Value)
        };
    }

    public async Task<Dictionary<string, object>> GetUserEngagementStatsAsync(
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.ContentEngagements.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(ce => ce.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ce => ce.CreatedAt <= toDate.Value);

        var engagements = await query.ToListAsync();

        var stats = new Dictionary<string, object>
        {
            ["totalEngagements"] = engagements.Count,
            ["uniqueUsers"] = engagements.Select(e => e.UserId).Distinct().Count(),
            ["engagementsByType"] = engagements.GroupBy(e => e.EngagementType.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            ["engagementsByContentType"] = engagements.GroupBy(e => e.ContentType.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            ["averageEngagementsPerUser"] = engagements.Any() ?
                (double)engagements.Count / engagements.Select(e => e.UserId).Distinct().Count() : 0
        };

        return stats;
    }

    public async Task<Dictionary<string, object>> GetContentPerformanceStatsAsync(
        ContentType? contentType = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.ContentEngagements.AsQueryable();

        if (contentType.HasValue)
            query = query.Where(ce => ce.ContentType == contentType.Value);

        if (fromDate.HasValue)
            query = query.Where(ce => ce.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ce => ce.CreatedAt <= toDate.Value);

        var engagements = await query.ToListAsync();

        var stats = new Dictionary<string, object>
        {
            ["totalEngagements"] = engagements.Count,
            ["uniqueContent"] = engagements.Select(e => new { e.ContentType, e.ContentId }).Distinct().Count(),
            ["topContent"] = engagements.GroupBy(e => new { e.ContentType, e.ContentId })
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new { g.Key.ContentType, g.Key.ContentId, EngagementCount = g.Count() })
                .ToList(),
            ["engagementsByType"] = engagements.GroupBy(e => e.EngagementType.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }

    public async Task<Dictionary<string, object>> GetSystemPerformanceStatsAsync(
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.PerformanceMetrics.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(pm => pm.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(pm => pm.CreatedAt <= toDate.Value);

        var metrics = await query.ToListAsync();

        var stats = new Dictionary<string, object>
        {
            ["totalMetrics"] = metrics.Count,
            ["metricsByType"] = metrics.GroupBy(m => m.MetricType.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            ["metricsBySource"] = metrics.GroupBy(m => m.Source)
                .ToDictionary(g => g.Key, g => g.Count()),
            ["averageResponseTime"] = metrics.Where(m => m.MetricType == MetricType.ResponseTime)
                .Select(m => m.Value).DefaultIfEmpty(0).Average(),
            ["errorRate"] = metrics.Where(m => m.Success.HasValue)
                .GroupBy(m => m.Success!.Value)
                .ToDictionary(g => g.Key ? "success" : "error", g => g.Count())
        };

        return stats;
    }

    public async Task CleanupOldAnalyticsDataAsync(TimeSpan maxAge)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - maxAge;
            var deletedCounts = new Dictionary<string, int>();

            // Clean up old user activities
            var oldActivities = await _context.UserActivities
                .Where(ua => ua.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldActivities.Any())
            {
                _context.UserActivities.RemoveRange(oldActivities);
                deletedCounts["UserActivities"] = oldActivities.Count;
            }

            // Clean up old content engagements
            var oldEngagements = await _context.ContentEngagements
                .Where(ce => ce.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldEngagements.Any())
            {
                _context.ContentEngagements.RemoveRange(oldEngagements);
                deletedCounts["ContentEngagements"] = oldEngagements.Count;
            }

            // Clean up old tag analytics
            var oldTagAnalytics = await _context.TagAnalytics
                .Where(ta => ta.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldTagAnalytics.Any())
            {
                _context.TagAnalytics.RemoveRange(oldTagAnalytics);
                deletedCounts["TagAnalytics"] = oldTagAnalytics.Count;
            }

            // Clean up old performance metrics
            var oldMetrics = await _context.PerformanceMetrics
                .Where(pm => pm.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldMetrics.Any())
            {
                _context.PerformanceMetrics.RemoveRange(oldMetrics);
                deletedCounts["PerformanceMetrics"] = oldMetrics.Count;
            }

            // Note: We keep trust score history for audit purposes
            // Only clean up very old entries (e.g., older than 2 years)
            var veryOldCutoff = DateTime.UtcNow - TimeSpan.FromDays(730);
            var oldTrustHistory = await _context.UserTrustScoreHistories
                .Where(h => h.CreatedAt < veryOldCutoff)
                .ToListAsync();

            if (oldTrustHistory.Any())
            {
                _context.UserTrustScoreHistories.RemoveRange(oldTrustHistory);
                deletedCounts["UserTrustScoreHistory"] = oldTrustHistory.Count;
            }

            await _context.SaveChangesAsync();

            var totalDeleted = deletedCounts.Values.Sum();
            _logger.LogInformation("Cleaned up {TotalDeleted} old analytics records older than {MaxAge}: {Details}",
                totalDeleted, maxAge, string.Join(", ", deletedCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old analytics data");
            throw;
        }
    }

    public async Task<Dictionary<string, long>> GetAnalyticsStorageStatsAsync()
    {
        var stats = new Dictionary<string, long>
        {
            ["UserActivities"] = await _context.UserActivities.LongCountAsync(),
            ["ContentEngagements"] = await _context.ContentEngagements.LongCountAsync(),
            ["UserTrustScoreHistories"] = await _context.UserTrustScoreHistories.LongCountAsync(),
            ["TagAnalytics"] = await _context.TagAnalytics.LongCountAsync(),
            ["PerformanceMetrics"] = await _context.PerformanceMetrics.LongCountAsync()
        };

        stats["Total"] = stats.Values.Sum();
        return stats;
    }
}
