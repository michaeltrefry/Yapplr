namespace Yapplr.Api.Services;

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