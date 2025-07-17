using System.Security.Claims;
using Serilog.Context;

namespace Yapplr.Api.Services;

/// <summary>
/// Service to enhance logging with structured context and correlation IDs
/// </summary>
public interface ILoggingEnhancementService
{
    /// <summary>
    /// Create a logging scope with user context
    /// </summary>
    IDisposable CreateUserScope(ClaimsPrincipal user);
    
    /// <summary>
    /// Create a logging scope with user ID
    /// </summary>
    IDisposable CreateUserScope(int userId);
    
    /// <summary>
    /// Create a logging scope with request context
    /// </summary>
    IDisposable CreateRequestScope(string? requestId = null, string? correlationId = null);
    
    /// <summary>
    /// Create a logging scope with operation context
    /// </summary>
    IDisposable CreateOperationScope(string operationName, object? parameters = null);
    
    /// <summary>
    /// Create a logging scope with business context
    /// </summary>
    IDisposable CreateBusinessScope(string entityType, int entityId, string operation);
}

public class LoggingEnhancementService : ILoggingEnhancementService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggingEnhancementService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IDisposable CreateUserScope(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        var properties = new List<IDisposable>();
        
        if (!string.IsNullOrEmpty(userId))
            properties.Add(LogContext.PushProperty("UserId", userId));
        
        if (!string.IsNullOrEmpty(username))
            properties.Add(LogContext.PushProperty("Username", username));
        
        if (!string.IsNullOrEmpty(role))
            properties.Add(LogContext.PushProperty("UserRole", role));

        return new CompositeDisposable(properties);
    }

    public IDisposable CreateUserScope(int userId)
    {
        return LogContext.PushProperty("UserId", userId);
    }

    public IDisposable CreateRequestScope(string? requestId = null, string? correlationId = null)
    {
        var properties = new List<IDisposable>();
        
        // Get request ID from HTTP context if not provided
        if (string.IsNullOrEmpty(requestId))
        {
            requestId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        }
        
        // Generate correlation ID if not provided
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N")[..8]; // Short correlation ID
        }

        if (!string.IsNullOrEmpty(requestId))
            properties.Add(LogContext.PushProperty("RequestId", requestId));
        
        if (!string.IsNullOrEmpty(correlationId))
            properties.Add(LogContext.PushProperty("CorrelationId", correlationId));

        // Add HTTP context information if available
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            properties.Add(LogContext.PushProperty("HttpMethod", httpContext.Request.Method));
            properties.Add(LogContext.PushProperty("RequestPath", httpContext.Request.Path.Value));
            
            var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
            if (!string.IsNullOrEmpty(userAgent))
                properties.Add(LogContext.PushProperty("UserAgent", userAgent));
            
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress))
                properties.Add(LogContext.PushProperty("IpAddress", ipAddress));
        }

        return new CompositeDisposable(properties);
    }

    public IDisposable CreateOperationScope(string operationName, object? parameters = null)
    {
        var properties = new List<IDisposable>
        {
            LogContext.PushProperty("Operation", operationName)
        };

        if (parameters != null)
        {
            properties.Add(LogContext.PushProperty("OperationParameters", parameters, destructureObjects: true));
        }

        return new CompositeDisposable(properties);
    }

    public IDisposable CreateBusinessScope(string entityType, int entityId, string operation)
    {
        var properties = new List<IDisposable>
        {
            LogContext.PushProperty("EntityType", entityType),
            LogContext.PushProperty("EntityId", entityId),
            LogContext.PushProperty("BusinessOperation", operation)
        };

        return new CompositeDisposable(properties);
    }
}

/// <summary>
/// Helper class to dispose multiple IDisposable objects
/// </summary>
public class CompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables;
    private bool _disposed = false;

    public CompositeDisposable(List<IDisposable> disposables)
    {
        _disposables = disposables ?? throw new ArgumentNullException(nameof(disposables));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Extension methods for enhanced logging
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Log a business operation with structured context
    /// </summary>
    public static void LogBusinessOperation(this ILogger logger, string operation, object? parameters = null, 
        string? entityType = null, int? entityId = null)
    {
        using var scope = LogContext.PushProperty("BusinessOperation", operation);
        using var paramScope = parameters != null ? LogContext.PushProperty("Parameters", parameters, destructureObjects: true) : null;
        using var entityScope = entityType != null ? LogContext.PushProperty("EntityType", entityType) : null;
        using var idScope = entityId.HasValue ? LogContext.PushProperty("EntityId", entityId.Value) : null;
        
        logger.LogInformation("Business operation: {Operation}", operation);
    }

    /// <summary>
    /// Log a business operation error with structured context
    /// </summary>
    public static void LogBusinessError(this ILogger logger, Exception exception, string operation, 
        object? parameters = null, string? entityType = null, int? entityId = null)
    {
        using var scope = LogContext.PushProperty("BusinessOperation", operation);
        using var paramScope = parameters != null ? LogContext.PushProperty("Parameters", parameters, destructureObjects: true) : null;
        using var entityScope = entityType != null ? LogContext.PushProperty("EntityType", entityType) : null;
        using var idScope = entityId.HasValue ? LogContext.PushProperty("EntityId", entityId.Value) : null;
        
        logger.LogError(exception, "Business operation failed: {Operation}", operation);
    }

    /// <summary>
    /// Log user action with context
    /// </summary>
    public static void LogUserAction(this ILogger logger, int userId, string action, object? details = null)
    {
        using var userScope = LogContext.PushProperty("UserId", userId);
        using var actionScope = LogContext.PushProperty("UserAction", action);
        using var detailsScope = details != null ? LogContext.PushProperty("ActionDetails", details, destructureObjects: true) : null;
        
        logger.LogInformation("User action: {Action} by user {UserId}", action, userId);
    }

    /// <summary>
    /// Log security event with enhanced context
    /// </summary>
    public static void LogSecurityEvent(this ILogger logger, string eventType, int? userId = null, 
        string? ipAddress = null, object? details = null, LogLevel level = LogLevel.Warning)
    {
        using var eventScope = LogContext.PushProperty("SecurityEvent", eventType);
        using var userScope = userId.HasValue ? LogContext.PushProperty("UserId", userId.Value) : null;
        using var ipScope = ipAddress != null ? LogContext.PushProperty("IpAddress", ipAddress) : null;
        using var detailsScope = details != null ? LogContext.PushProperty("SecurityDetails", details, destructureObjects: true) : null;
        
        logger.Log(level, "Security event: {EventType}", eventType);
    }

    /// <summary>
    /// Log performance metrics
    /// </summary>
    public static void LogPerformance(this ILogger logger, string operation, TimeSpan duration,
        object? metrics = null)
    {
        using var opScope = LogContext.PushProperty("PerformanceOperation", operation);
        using var durationScope = LogContext.PushProperty("Duration", duration.TotalMilliseconds);
        using var metricsScope = metrics != null ? LogContext.PushProperty("Metrics", metrics, destructureObjects: true) : null;

        logger.LogInformation("Performance: {Operation} completed in {Duration}ms", operation, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Create a disposable timer for measuring operation performance
    /// </summary>
    public static IDisposable BeginTimedOperation(this ILogger logger, string operation, object? parameters = null)
    {
        return new TimedOperation(logger, operation, parameters);
    }
}

/// <summary>
/// Disposable timer for measuring operation performance
/// </summary>
public class TimedOperation : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operation;
    private readonly object? _parameters;
    private readonly DateTime _startTime;
    private readonly List<IDisposable> _scopes;
    private bool _disposed = false;

    public TimedOperation(ILogger logger, string operation, object? parameters = null)
    {
        _logger = logger;
        _operation = operation;
        _parameters = parameters;
        _startTime = DateTime.UtcNow;

        _scopes = new List<IDisposable>
        {
            LogContext.PushProperty("TimedOperation", operation)
        };

        if (parameters != null)
        {
            _scopes.Add(LogContext.PushProperty("OperationParameters", parameters, destructureObjects: true));
        }

        _logger.LogDebug("Started timed operation: {Operation}", operation);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            var duration = DateTime.UtcNow - _startTime;

            using var durationScope = LogContext.PushProperty("Duration", duration.TotalMilliseconds);
            _logger.LogInformation("Completed timed operation: {Operation} in {Duration}ms",
                _operation, duration.TotalMilliseconds);

            foreach (var scope in _scopes)
            {
                try
                {
                    scope?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }

            _disposed = true;
        }
    }
}
