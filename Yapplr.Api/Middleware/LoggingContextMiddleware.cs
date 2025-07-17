using Serilog.Context;
using System.Security.Claims;

namespace Yapplr.Api.Middleware;

/// <summary>
/// Middleware that automatically adds request context to all logs
/// </summary>
public class LoggingContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingContextMiddleware> _logger;

    public LoggingContextMiddleware(RequestDelegate next, ILogger<LoggingContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation ID for this request
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        
        // Add correlation ID to response headers for debugging
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        var disposables = new List<IDisposable>();

        try
        {
            // Add basic request context
            disposables.Add(LogContext.PushProperty("RequestId", context.TraceIdentifier));
            disposables.Add(LogContext.PushProperty("CorrelationId", correlationId));
            disposables.Add(LogContext.PushProperty("HttpMethod", context.Request.Method));
            disposables.Add(LogContext.PushProperty("RequestPath", context.Request.Path.Value));
            disposables.Add(LogContext.PushProperty("QueryString", context.Request.QueryString.Value));

            // Add IP address
            var ipAddress = GetClientIpAddress(context);
            if (!string.IsNullOrEmpty(ipAddress))
            {
                disposables.Add(LogContext.PushProperty("IpAddress", ipAddress));
            }

            // Add User-Agent
            var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
            if (!string.IsNullOrEmpty(userAgent))
            {
                disposables.Add(LogContext.PushProperty("UserAgent", userAgent));
            }

            // Add user context if authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = context.User.FindFirst(ClaimTypes.Name)?.Value;
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(userId))
                    disposables.Add(LogContext.PushProperty("UserId", userId));
                
                if (!string.IsNullOrEmpty(username))
                    disposables.Add(LogContext.PushProperty("Username", username));
                
                if (!string.IsNullOrEmpty(role))
                    disposables.Add(LogContext.PushProperty("UserRole", role));
            }

            // Log request start
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("HTTP {HttpMethod} {RequestPath} started", 
                context.Request.Method, context.Request.Path);

            // Process request
            await _next(context);

            // Log request completion
            var duration = DateTime.UtcNow - startTime;
            var statusCode = context.Response.StatusCode;
            
            disposables.Add(LogContext.PushProperty("StatusCode", statusCode));
            disposables.Add(LogContext.PushProperty("Duration", duration.TotalMilliseconds));

            var logLevel = GetLogLevelForStatusCode(statusCode);
            _logger.Log(logLevel, "HTTP {HttpMethod} {RequestPath} completed with {StatusCode} in {Duration}ms",
                context.Request.Method, context.Request.Path, statusCode, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            // Log unhandled exceptions
            _logger.LogError(ex, "Unhandled exception in request {HttpMethod} {RequestPath}",
                context.Request.Method, context.Request.Path);
            throw;
        }
        finally
        {
            // Clean up all logging context
            foreach (var disposable in disposables)
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
        }
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP if there are multiple
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static LogLevel GetLogLevelForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}

/// <summary>
/// Extension methods for registering the logging context middleware
/// </summary>
public static class LoggingContextMiddlewareExtensions
{
    /// <summary>
    /// Adds the logging context middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseLoggingContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LoggingContextMiddleware>();
    }
}
