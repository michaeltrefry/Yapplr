namespace Yapplr.Api.Services;

/// <summary>
/// Smart retry service that adapts retry behavior based on error types
/// </summary>
public interface ISmartRetryService
{
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default);
    
    NotificationErrorType ClassifyError(Exception exception);
    RetryStrategy GetRetryStrategy(NotificationErrorType errorType);
    Task<bool> ShouldRetryAsync(NotificationErrorType errorType, int attemptNumber, Exception exception);
}