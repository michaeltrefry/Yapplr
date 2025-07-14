using System.Net;

namespace Yapplr.Api.Services;

public class SmartRetryService : ISmartRetryService
{
    private readonly ILogger<SmartRetryService> _logger;
    private readonly Random _random = new();
    
    // Retry strategies for different error types
    private readonly Dictionary<NotificationErrorType, RetryStrategy> _retryStrategies = new()
    {
        [NotificationErrorType.NetworkTimeout] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromMinutes(5),
            MaxAttempts = 5,
            BackoffMultiplier = 2.0,
            UseJitter = true
        },
        [NotificationErrorType.NetworkUnavailable] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(5),
            MaxDelay = TimeSpan.FromMinutes(10),
            MaxAttempts = 3,
            BackoffMultiplier = 3.0,
            UseJitter = true
        },
        [NotificationErrorType.ServiceUnavailable] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(10),
            MaxDelay = TimeSpan.FromMinutes(15),
            MaxAttempts = 4,
            BackoffMultiplier = 2.5,
            UseJitter = true
        },
        [NotificationErrorType.RateLimited] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromMinutes(1),
            MaxDelay = TimeSpan.FromHours(1),
            MaxAttempts = 3,
            BackoffMultiplier = 4.0,
            UseJitter = false // Rate limiting usually has specific windows
        },
        [NotificationErrorType.ServerError] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromMinutes(5),
            MaxAttempts = 3,
            BackoffMultiplier = 2.0,
            UseJitter = true
        },
        [NotificationErrorType.InvalidToken] = new()
        {
            ShouldRetry = false, // Token issues need manual intervention
            MaxAttempts = 0
        },
        [NotificationErrorType.PermissionDenied] = new()
        {
            ShouldRetry = false, // Permission issues need user action
            MaxAttempts = 0
        },
        [NotificationErrorType.InvalidPayload] = new()
        {
            ShouldRetry = false, // Payload issues need code fixes
            MaxAttempts = 0
        },
        [NotificationErrorType.ClientError] = new()
        {
            ShouldRetry = false, // Client errors (4xx) usually don't benefit from retries
            MaxAttempts = 0
        },
        [NotificationErrorType.QuotaExceeded] = new()
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromHours(1),
            MaxDelay = TimeSpan.FromHours(24),
            MaxAttempts = 2,
            BackoffMultiplier = 24.0, // Wait for quota reset
            UseJitter = false
        }
    };

    public SmartRetryService(ILogger<SmartRetryService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var attempts = new List<RetryAttempt>();
        Exception? lastException = null;

        for (int attempt = 1; attempt <= 10; attempt++) // Max 10 attempts across all strategies
        {
            try
            {
                var result = await operation();
                
                if (attempts.Any())
                {
                    _logger.LogInformation("Operation {OperationName} succeeded after {AttemptCount} attempts",
                        operationName, attempt);
                }

                return result;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                var errorType = ClassifyError(ex);
                var strategy = GetRetryStrategy(errorType);

                var retryAttempt = new RetryAttempt
                {
                    AttemptNumber = attempt,
                    AttemptTime = DateTime.UtcNow,
                    ErrorType = errorType,
                    ErrorMessage = ex.Message,
                    IsSuccessful = false
                };

                attempts.Add(retryAttempt);

                if (!strategy.ShouldRetry || attempt >= strategy.MaxAttempts)
                {
                    _logger.LogWarning("Operation {OperationName} failed after {AttemptCount} attempts. Error type: {ErrorType}. Final error: {Error}",
                        operationName, attempt, errorType, ex.Message);
                    break;
                }

                var shouldRetry = await ShouldRetryAsync(errorType, attempt, ex);
                if (!shouldRetry)
                {
                    _logger.LogWarning("Operation {OperationName} retry skipped based on dynamic conditions. Error type: {ErrorType}",
                        operationName, errorType);
                    break;
                }

                var delay = CalculateDelay(strategy, attempt);
                retryAttempt.Delay = delay;

                _logger.LogWarning("Operation {OperationName} failed (attempt {AttemptNumber}), retrying in {Delay}ms. Error: {Error}",
                    operationName, attempt, delay.TotalMilliseconds, ex.Message);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // If we get here, all retries failed
        throw lastException ?? new InvalidOperationException($"Operation {operationName} failed with unknown error");
    }

    public NotificationErrorType ClassifyError(Exception exception)
    {
        return exception switch
        {
            TaskCanceledException or TimeoutException => NotificationErrorType.NetworkTimeout,
            HttpRequestException httpEx when httpEx.Message.Contains("timeout") => NotificationErrorType.NetworkTimeout,
            HttpRequestException httpEx when httpEx.Message.Contains("network") => NotificationErrorType.NetworkUnavailable,
            HttpRequestException httpEx when httpEx.Data.Contains("StatusCode") => 
                ClassifyHttpStatusCode((HttpStatusCode)httpEx.Data["StatusCode"]!),
            UnauthorizedAccessException => NotificationErrorType.PermissionDenied,
            ArgumentException or ArgumentNullException => NotificationErrorType.InvalidPayload,
            InvalidOperationException invEx when invEx.Message.Contains("quota") => NotificationErrorType.QuotaExceeded,
            InvalidOperationException invEx when invEx.Message.Contains("rate") => NotificationErrorType.RateLimited,
            _ => NotificationErrorType.Unknown
        };
    }

    private NotificationErrorType ClassifyHttpStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout => NotificationErrorType.NetworkTimeout,
            HttpStatusCode.TooManyRequests => NotificationErrorType.RateLimited,
            HttpStatusCode.Unauthorized => NotificationErrorType.InvalidToken,
            HttpStatusCode.Forbidden => NotificationErrorType.PermissionDenied,
            HttpStatusCode.BadRequest => NotificationErrorType.InvalidPayload,
            HttpStatusCode.ServiceUnavailable => NotificationErrorType.ServiceUnavailable,
            HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout => NotificationErrorType.ServiceUnavailable,
            >= HttpStatusCode.InternalServerError => NotificationErrorType.ServerError,
            >= HttpStatusCode.BadRequest => NotificationErrorType.ClientError,
            _ => NotificationErrorType.Unknown
        };
    }

    public RetryStrategy GetRetryStrategy(NotificationErrorType errorType)
    {
        if (_retryStrategies.TryGetValue(errorType, out var strategy))
        {
            return strategy;
        }

        // Default strategy for unknown errors
        return new RetryStrategy
        {
            ShouldRetry = true,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromMinutes(2),
            MaxAttempts = 2,
            BackoffMultiplier = 2.0,
            UseJitter = true
        };
    }

    public async Task<bool> ShouldRetryAsync(NotificationErrorType errorType, int attemptNumber, Exception exception)
    {
        // Add dynamic retry logic based on current conditions
        
        // For rate limiting, check if we have information about when to retry
        if (errorType == NotificationErrorType.RateLimited)
        {
            // In a real implementation, you might check rate limit headers
            // or maintain a rate limit tracker
            return true;
        }

        // For service unavailable, you might check a health endpoint
        if (errorType == NotificationErrorType.ServiceUnavailable)
        {
            // Could implement a circuit breaker pattern here
            return true;
        }

        // For network issues, you might check connectivity
        if (errorType == NotificationErrorType.NetworkUnavailable)
        {
            // Could ping a reliable endpoint to check connectivity
            return true;
        }

        await Task.CompletedTask; // Placeholder for async operations
        return true;
    }

    private TimeSpan CalculateDelay(RetryStrategy strategy, int attemptNumber)
    {
        if (!strategy.ShouldRetry)
            return TimeSpan.Zero;

        var delay = TimeSpan.FromMilliseconds(
            strategy.InitialDelay.TotalMilliseconds * Math.Pow(strategy.BackoffMultiplier, attemptNumber - 1));

        // Cap at max delay
        if (delay > strategy.MaxDelay)
            delay = strategy.MaxDelay;

        // Add jitter to prevent thundering herd
        if (strategy.UseJitter)
        {
            var jitterMs = _random.Next(0, (int)(delay.TotalMilliseconds * 0.1)); // Up to 10% jitter
            delay = delay.Add(TimeSpan.FromMilliseconds(jitterMs));
        }

        return delay;
    }

    // Add a simpler overload for common use cases
    public async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync<bool>(
            async () => {
                await operation();
                return true;
            },
            operationName,
            cancellationToken);
    }
}
