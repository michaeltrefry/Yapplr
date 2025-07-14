namespace Yapplr.Api.Common;

/// <summary>
/// Middleware to automatically monitor all database operations
/// </summary>
public class QueryPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IQueryPerformanceMonitor _monitor;
    private readonly ILogger<QueryPerformanceMiddleware> _logger;

    public QueryPerformanceMiddleware(
        RequestDelegate next, 
        IQueryPerformanceMonitor monitor,
        ILogger<QueryPerformanceMiddleware> logger)
    {
        _next = next;
        _monitor = monitor;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;
        var operationName = $"{method} {requestPath}";

        await _monitor.MonitorAsync($"Request:{operationName}", async () =>
        {
            await _next(context);
        });
    }
}