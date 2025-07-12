using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Yapplr.Api.Data;

namespace Yapplr.Api.Services;

/// <summary>
/// Audit log entry for notification events
/// </summary>
public class NotificationAuditLog
{
    public int Id { get; set; }
    [StringLength(64)]
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [StringLength(30)]
    public string EventType { get; set; } = string.Empty; // "sent", "delivered", "failed", "blocked", "rate_limited"
    public int? UserId { get; set; }
    [StringLength(100)]
    public string? Username { get; set; }
    [StringLength(256)]
    public string NotificationType { get; set; } = string.Empty;
    [StringLength(256)]
    public string? Title { get; set; }
    [StringLength(1000)]
    public string? Body { get; set; }
    [StringLength(20)]
    public string? DeliveryMethod { get; set; }
    public bool Success { get; set; }
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
    [StringLength(64)]
    public string? IpAddress { get; set; }
    [StringLength(256)]
    public string? UserAgent { get; set; }
    [StringLength(1000)]
    public string? AdditionalDataJson { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
    [StringLength(1000)]
    public string? SecurityFlags { get; set; } // JSON array of security-related flags
}

/// <summary>
/// Audit event types
/// </summary>
public static class AuditEventTypes
{
    public const string NotificationSent = "notification_sent";
    public const string NotificationDelivered = "notification_delivered";
    public const string NotificationFailed = "notification_failed";
    public const string NotificationBlocked = "notification_blocked";
    public const string RateLimitExceeded = "rate_limit_exceeded";
    public const string ContentFiltered = "content_filtered";
    public const string UserBlocked = "user_blocked";
    public const string UserUnblocked = "user_unblocked";
    public const string SecurityViolation = "security_violation";
    public const string ConfigurationChanged = "configuration_changed";
}

/// <summary>
/// Audit query parameters
/// </summary>
public class AuditQueryParams
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? UserId { get; set; }
    public string? EventType { get; set; }
    public string? NotificationType { get; set; }
    public bool? Success { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Service for auditing notification delivery and security events
/// </summary>
public interface INotificationAuditService
{
    Task LogEventAsync(string eventType, int? userId, string? notificationType, object? additionalData = null, bool success = true, string? errorMessage = null);
    Task LogNotificationSentAsync(int userId, string notificationType, string title, string body, string deliveryMethod, bool success, string? errorMessage = null, TimeSpan? processingTime = null);
    Task LogSecurityEventAsync(string eventType, int? userId, string description, object? additionalData = null);
    Task<List<NotificationAuditLog>> GetAuditLogsAsync(AuditQueryParams queryParams);
    Task<Dictionary<string, object>> GetAuditStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<List<NotificationAuditLog>> GetUserAuditLogsAsync(int userId, int count = 100);
    Task<List<NotificationAuditLog>> GetSecurityEventsAsync(DateTime? since = null);
    Task CleanupOldLogsAsync(TimeSpan maxAge);
}

public class NotificationAuditService : INotificationAuditService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<NotificationAuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationAuditService(
        YapplrDbContext context,
        ILogger<NotificationAuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogEventAsync(string eventType, int? userId, string? notificationType, object? additionalData = null, bool success = true, string? errorMessage = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            var auditLog = new NotificationAuditLog
            {
                EventType = eventType,
                UserId = userId,
                NotificationType = notificationType ?? "unknown",
                Success = success,
                ErrorMessage = errorMessage,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
                AdditionalDataJson = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            // Get username if userId is provided
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                auditLog.Username = user?.Username;
            }

            _context.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Logged audit event {EventType} for user {UserId}", eventType, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event {EventType} for user {UserId}", eventType, userId);
            // Don't throw - audit logging should not break the main flow
        }
    }

    public async Task LogNotificationSentAsync(int userId, string notificationType, string title, string body, string deliveryMethod, bool success, string? errorMessage = null, TimeSpan? processingTime = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            var auditLog = new NotificationAuditLog
            {
                EventType = success ? AuditEventTypes.NotificationSent : AuditEventTypes.NotificationFailed,
                UserId = userId,
                NotificationType = notificationType,
                Title = title,
                Body = body,
                DeliveryMethod = deliveryMethod,
                Success = success,
                ErrorMessage = errorMessage,
                ProcessingTime = processingTime,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString()
            };

            // Get username
            var user = await _context.Users.FindAsync(userId);
            auditLog.Username = user?.Username;

            _context.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Logged notification {EventType} for user {UserId} (type: {NotificationType})", 
                auditLog.EventType, userId, notificationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log notification event for user {UserId}", userId);
        }
    }

    public async Task LogSecurityEventAsync(string eventType, int? userId, string description, object? additionalData = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            var securityFlags = new List<string> { "security_event" };
            
            // Add specific security flags based on event type
            switch (eventType)
            {
                case AuditEventTypes.RateLimitExceeded:
                    securityFlags.Add("rate_limit");
                    break;
                case AuditEventTypes.ContentFiltered:
                    securityFlags.Add("content_filter");
                    break;
                case AuditEventTypes.UserBlocked:
                    securityFlags.Add("user_blocked");
                    break;
                case AuditEventTypes.SecurityViolation:
                    securityFlags.Add("violation");
                    break;
            }

            var auditLog = new NotificationAuditLog
            {
                EventType = eventType,
                UserId = userId,
                NotificationType = "security",
                Body = description,
                Success = false, // Security events are typically failures/violations
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
                AdditionalDataJson = additionalData != null ? JsonSerializer.Serialize(additionalData) : null,
                SecurityFlags = JsonSerializer.Serialize(securityFlags)
            };

            // Get username if userId is provided
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                auditLog.Username = user?.Username;
            }

            _context.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Logged security event {EventType} for user {UserId}: {Description}", 
                eventType, userId, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event {EventType} for user {UserId}", eventType, userId);
        }
    }

    public async Task<List<NotificationAuditLog>> GetAuditLogsAsync(AuditQueryParams queryParams)
    {
        var query = _context.Set<NotificationAuditLog>().AsQueryable();

        // Apply filters
        if (queryParams.StartDate.HasValue)
            query = query.Where(log => log.Timestamp >= queryParams.StartDate.Value);

        if (queryParams.EndDate.HasValue)
            query = query.Where(log => log.Timestamp <= queryParams.EndDate.Value);

        if (queryParams.UserId.HasValue)
            query = query.Where(log => log.UserId == queryParams.UserId.Value);

        if (!string.IsNullOrEmpty(queryParams.EventType))
            query = query.Where(log => log.EventType == queryParams.EventType);

        if (!string.IsNullOrEmpty(queryParams.NotificationType))
            query = query.Where(log => log.NotificationType == queryParams.NotificationType);

        if (queryParams.Success.HasValue)
            query = query.Where(log => log.Success == queryParams.Success.Value);

        // Apply pagination
        var skip = (queryParams.Page - 1) * queryParams.PageSize;
        
        return await query
            .OrderByDescending(log => log.Timestamp)
            .Skip(skip)
            .Take(queryParams.PageSize)
            .ToListAsync();
    }

    public async Task<Dictionary<string, object>> GetAuditStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Set<NotificationAuditLog>().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(log => log.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(log => log.Timestamp <= endDate.Value);

        var totalEvents = await query.CountAsync();
        var successfulEvents = await query.CountAsync(log => log.Success);
        var failedEvents = totalEvents - successfulEvents;

        var eventTypeStats = await query
            .GroupBy(log => log.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync();

        var notificationTypeStats = await query
            .Where(log => !string.IsNullOrEmpty(log.NotificationType) && log.NotificationType != "security")
            .GroupBy(log => log.NotificationType)
            .Select(g => new { NotificationType = g.Key, Count = g.Count() })
            .ToListAsync();

        var deliveryMethodStats = await query
            .Where(log => !string.IsNullOrEmpty(log.DeliveryMethod))
            .GroupBy(log => log.DeliveryMethod)
            .Select(g => new { DeliveryMethod = g.Key, Count = g.Count() })
            .ToListAsync();

        var securityEvents = await query
            .CountAsync(log => log.SecurityFlags != null && log.SecurityFlags.Contains("security_event"));

        var averageProcessingTime = await query
            .Where(log => log.ProcessingTime.HasValue)
            .AverageAsync(log => log.ProcessingTime!.Value.TotalMilliseconds);

        return new Dictionary<string, object>
        {
            ["total_events"] = totalEvents,
            ["successful_events"] = successfulEvents,
            ["failed_events"] = failedEvents,
            ["success_rate"] = totalEvents > 0 ? (double)successfulEvents / totalEvents * 100 : 0,
            ["security_events"] = securityEvents,
            ["average_processing_time_ms"] = averageProcessingTime,
            ["event_type_breakdown"] = eventTypeStats.ToDictionary(s => s.EventType, s => s.Count),
            ["notification_type_breakdown"] = notificationTypeStats.ToDictionary(s => s.NotificationType!, s => s.Count),
            ["delivery_method_breakdown"] = deliveryMethodStats.ToDictionary(s => s.DeliveryMethod!, s => s.Count),
            ["date_range"] = new
            {
                start = startDate?.ToString("yyyy-MM-dd") ?? "all_time",
                end = endDate?.ToString("yyyy-MM-dd") ?? "now"
            }
        };
    }

    public async Task<List<NotificationAuditLog>> GetUserAuditLogsAsync(int userId, int count = 100)
    {
        return await _context.Set<NotificationAuditLog>()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<NotificationAuditLog>> GetSecurityEventsAsync(DateTime? since = null)
    {
        var query = _context.Set<NotificationAuditLog>()
            .Where(log => log.SecurityFlags != null && log.SecurityFlags.Contains("security_event"));

        if (since.HasValue)
            query = query.Where(log => log.Timestamp >= since.Value);

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Take(1000) // Limit to prevent large queries
            .ToListAsync();
    }

    public async Task CleanupOldLogsAsync(TimeSpan maxAge)
    {
        var cutoffDate = DateTime.UtcNow - maxAge;
        
        var oldLogs = await _context.Set<NotificationAuditLog>()
            .Where(log => log.Timestamp < cutoffDate)
            .ToListAsync();

        if (oldLogs.Any())
        {
            _context.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} audit logs older than {CutoffDate}", 
                oldLogs.Count, cutoffDate);
        }
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        // Check for forwarded IP first (in case of proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
