using System.Security.Claims;
using System.Text.Json;
using Yapplr.Api.Services;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Middleware;

public class ApiRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiRateLimitMiddleware> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    // Mapping of HTTP methods and paths to API operations
    private readonly Dictionary<string, ApiOperation> _endpointMappings = new()
    {
        // Posts
        ["POST:/api/posts"] = ApiOperation.CreatePost,
        ["POST:/api/posts/{id}/comments"] = ApiOperation.CreateComment,
        ["POST:/api/posts/{id}/like"] = ApiOperation.LikePost,
        ["DELETE:/api/posts/{id}/like"] = ApiOperation.UnlikePost,
        
        // Users and follows
        ["POST:/api/users/{id}/follow"] = ApiOperation.FollowUser,
        ["DELETE:/api/users/{id}/follow"] = ApiOperation.UnfollowUser,
        ["PUT:/api/users/profile"] = ApiOperation.UpdateProfile,
        ["PATCH:/api/users/profile"] = ApiOperation.UpdateProfile,
        
        // Content moderation
        ["POST:/api/reports"] = ApiOperation.ReportContent,
        
        // Messages
        ["POST:/api/messages"] = ApiOperation.SendMessage,
        
        // Media uploads
        ["POST:/api/media/upload"] = ApiOperation.UploadMedia,
        ["POST:/api/posts/upload"] = ApiOperation.UploadMedia,
        
        // Search
        ["GET:/api/search"] = ApiOperation.Search,
        ["GET:/api/posts/search"] = ApiOperation.Search,
        ["GET:/api/users/search"] = ApiOperation.Search
    };

    public ApiRateLimitMiddleware(
        RequestDelegate next, 
        ILogger<ApiRateLimitMiddleware> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply rate limiting to API endpoints
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Skip rate limiting for certain endpoints
        if (ShouldSkipRateLimit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Only apply to authenticated users
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            await _next(context);
            return;
        }

        // Determine the API operation
        var operation = GetApiOperation(context.Request.Method, context.Request.Path);

        // Check rate limit using scoped service
        using var scope = _serviceScopeFactory.CreateScope();
        var rateLimitService = scope.ServiceProvider.GetRequiredService<IApiRateLimitService>();

        try
        {
            var rateLimitResult = await rateLimitService.CheckRateLimitAsync(userId, operation);

            if (!rateLimitResult.IsAllowed)
            {
                await HandleRateLimitExceeded(context, rateLimitResult, operation);
                return;
            }

            // Record the request
            await rateLimitService.RecordRequestAsync(userId, operation);

            // Add rate limit headers to response
            AddRateLimitHeaders(context, rateLimitResult);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in API rate limiting for user {UserId}, operation {Operation}", 
                userId, operation);
            
            // Continue with request if rate limiting fails
            await _next(context);
        }
    }

    private bool ShouldSkipRateLimit(PathString path)
    {
        var skipPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/refresh",
            "/api/auth/logout",
            "/api/health",
            "/api/security/rate-limits", // Allow checking rate limit status
            "/api/users/me" // Allow checking own profile
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
    }

    private ApiOperation GetApiOperation(string method, PathString path)
    {
        var key = $"{method.ToUpper()}:{path.Value}";
        
        // Try exact match first
        if (_endpointMappings.TryGetValue(key, out var operation))
        {
            return operation;
        }

        // Try pattern matching for parameterized routes
        foreach (var mapping in _endpointMappings)
        {
            if (IsPathMatch(mapping.Key, key))
            {
                return mapping.Value;
            }
        }

        // Default to general operation
        return ApiOperation.General;
    }

    private bool IsPathMatch(string pattern, string actual)
    {
        // Simple pattern matching for routes with {id} parameters
        var patternParts = pattern.Split('/');
        var actualParts = actual.Split('/');

        if (patternParts.Length != actualParts.Length)
            return false;

        for (int i = 0; i < patternParts.Length; i++)
        {
            var patternPart = patternParts[i];
            var actualPart = actualParts[i];

            // Skip parameter placeholders
            if (patternPart.StartsWith("{") && patternPart.EndsWith("}"))
                continue;

            if (!string.Equals(patternPart, actualPart, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitResult result, ApiOperation operation)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        // Add retry-after header
        if (result.RetryAfter.HasValue)
        {
            context.Response.Headers["Retry-After"] = ((int)result.RetryAfter.Value.TotalSeconds).ToString();
        }

        // Add rate limit headers
        AddRateLimitHeaders(context, result);

        var response = new
        {
            error = "Rate limit exceeded",
            message = $"Too many {operation} requests. Please try again later.",
            violationType = result.ViolationType,
            retryAfter = result.RetryAfter?.TotalSeconds,
            resetTime = result.ResetTime?.ToString("O")
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);

        _logger.LogWarning("Rate limit exceeded for user {UserId}, operation {Operation}, violation type {ViolationType}",
            context.User.GetUserId(), operation, result.ViolationType);
    }

    private void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
    {
        if (result.RemainingRequests >= 0)
        {
            context.Response.Headers["X-RateLimit-Remaining"] = result.RemainingRequests.ToString();
        }

        if (result.ResetTime.HasValue)
        {
            var resetTimeUnix = ((DateTimeOffset)result.ResetTime.Value).ToUnixTimeSeconds();
            context.Response.Headers["X-RateLimit-Reset"] = resetTimeUnix.ToString();
        }

        // Add custom header to indicate this is trust-based rate limiting
        context.Response.Headers["X-RateLimit-Type"] = "trust-based";
    }
}
