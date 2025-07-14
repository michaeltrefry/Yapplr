namespace Yapplr.Api.Services;

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