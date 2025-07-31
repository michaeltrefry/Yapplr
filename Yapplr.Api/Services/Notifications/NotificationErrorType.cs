namespace Yapplr.Api.Services.Notifications;

/// <summary>
/// Error types for smart retry logic
/// </summary>
public enum NotificationErrorType
{
    Unknown,
    NetworkTimeout,
    NetworkUnavailable,
    ServiceUnavailable,
    RateLimited,
    InvalidToken,
    PermissionDenied,
    InvalidPayload,
    QuotaExceeded,
    ServerError,
    ClientError
}