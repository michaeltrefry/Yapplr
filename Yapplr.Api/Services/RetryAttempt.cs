namespace Yapplr.Api.Services;

/// <summary>
/// Retry attempt information
/// </summary>
public class RetryAttempt
{
    public int AttemptNumber { get; set; }
    public DateTime AttemptTime { get; set; }
    public TimeSpan Delay { get; set; }
    public NotificationErrorType ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccessful { get; set; }
}