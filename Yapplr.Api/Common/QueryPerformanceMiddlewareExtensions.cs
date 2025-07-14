namespace Yapplr.Api.Common;

/// <summary>
/// Extension method to register the performance monitoring middleware
/// </summary>
public static class QueryPerformanceMiddlewareExtensions
{
    public static IApplicationBuilder UseQueryPerformanceMonitoring(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<QueryPerformanceMiddleware>();
    }
}