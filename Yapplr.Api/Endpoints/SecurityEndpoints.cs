using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class SecurityEndpoints
{
    public static void MapSecurityEndpoints(this IEndpointRouteBuilder app)
    {
        var security = app.MapGroup("/api/security")
            .WithTags("Security")
            .RequireAuthorization();

        // Rate limiting endpoints
        security.MapGet("/rate-limits/violations", GetRateLimitViolations)
            .WithName("GetRateLimitViolations")
            .WithSummary("Get rate limit violations for the current user")
            .Produces<List<RateLimitViolation>>(200)
            .Produces(401);

        security.MapGet("/rate-limits/stats", GetRateLimitStats)
            .WithName("GetRateLimitStats")
            .WithSummary("Get rate limiting statistics")
            .Produces<Dictionary<string, object>>(200)
            .Produces(401);

        security.MapPost("/rate-limits/reset", ResetUserRateLimits)
            .WithName("ResetUserRateLimits")
            .WithSummary("Reset rate limits for the current user")
            .Produces(200)
            .Produces(401);

        // Content filtering endpoints
        security.MapPost("/content/validate", ValidateContent)
            .WithName("ValidateContent")
            .WithSummary("Validate content against security filters")
            .Produces<ContentValidationResult>(200)
            .Produces(400)
            .Produces(401);

        security.MapGet("/content/filter-stats", GetContentFilterStats)
            .WithName("GetContentFilterStats")
            .WithSummary("Get content filtering statistics")
            .Produces<Dictionary<string, object>>(200)
            .Produces(401);

        // Audit endpoints
        security.MapGet("/audit/logs", GetAuditLogs)
            .WithName("GetAuditLogs")
            .WithSummary("Get audit logs for the current user")
            .Produces<List<NotificationAuditLog>>(200)
            .Produces(401);

        security.MapGet("/audit/stats", GetAuditStats)
            .WithName("GetAuditStats")
            .WithSummary("Get audit statistics")
            .Produces<Dictionary<string, object>>(200)
            .Produces(401);

        // Admin endpoints
        security.MapGet("/admin/audit/all", GetAllAuditLogs)
            .WithName("GetAllAuditLogs")
            .WithSummary("Get all audit logs (admin only)")
            .Produces<List<NotificationAuditLog>>(200)
            .Produces(401)
            .Produces(403);

        security.MapGet("/admin/security-events", GetSecurityEvents)
            .WithName("GetSecurityEvents")
            .WithSummary("Get security events (admin only)")
            .Produces<List<NotificationAuditLog>>(200)
            .Produces(401)
            .Produces(403);

        security.MapPost("/admin/block-user/{userId}", BlockUser)
            .WithName("AdminBlockUser")
            .WithSummary("Block a user (admin only)")
            .Produces(200)
            .Produces(401)
            .Produces(403);

        security.MapPost("/admin/unblock-user/{userId}", UnblockUser)
            .WithName("AdminUnblockUser")
            .WithSummary("Unblock a user (admin only)")
            .Produces(200)
            .Produces(401)
            .Produces(403);

        security.MapPost("/admin/cleanup-logs", CleanupAuditLogs)
            .WithName("CleanupAuditLogs")
            .WithSummary("Cleanup old audit logs (admin only)")
            .Produces(200)
            .Produces(401)
            .Produces(403);
    }

    private static async Task<IResult> GetRateLimitViolations(
        ClaimsPrincipal user,
        INotificationRateLimitService rateLimitService)
    {
        try
        {
            var userId = user.GetUserId(true);
            var violations = await rateLimitService.GetRecentViolationsAsync(userId);
            return Results.Ok(violations);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get rate limit violations: {ex.Message}");
        }
    }

    private static async Task<IResult> GetRateLimitStats(
        INotificationRateLimitService rateLimitService)
    {
        try
        {
            var stats = await rateLimitService.GetRateLimitStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get rate limit stats: {ex.Message}");
        }
    }

    private static async Task<IResult> ResetUserRateLimits(
        ClaimsPrincipal user,
        INotificationRateLimitService rateLimitService)
    {
        try
        {
            var userId = user.GetUserId(true);
            await rateLimitService.ResetUserLimitsAsync(userId);
            return Results.Ok(new { message = "Rate limits reset successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to reset rate limits: {ex.Message}");
        }
    }

    private static async Task<IResult> ValidateContent(
        [FromBody] ValidateContentRequest request,
        INotificationContentFilterService contentFilterService)
    {
        try
        {
            var result = await contentFilterService.ValidateContentAsync(request.Content, request.ContentType ?? "text");
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to validate content: {ex.Message}");
        }
    }

    private static async Task<IResult> GetContentFilterStats(
        INotificationContentFilterService contentFilterService)
    {
        try
        {
            var stats = await contentFilterService.GetFilterStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get content filter stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetAuditLogs(
        ClaimsPrincipal user,
        INotificationAuditService auditService,
        [FromQuery] int count = 100)
    {
        try
        {
            var userId = user.GetUserId(true);
            var logs = await auditService.GetUserAuditLogsAsync(userId, count);
            return Results.Ok(logs);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get audit logs: {ex.Message}");
        }
    }

    private static async Task<IResult> GetAuditStats(
        INotificationAuditService auditService,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var stats = await auditService.GetAuditStatsAsync(startDate, endDate);
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get audit stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetAllAuditLogs(
        ClaimsPrincipal user,
        INotificationAuditService auditService,
        [FromQuery] string? eventType = null,
        [FromQuery] int? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            var queryParams = new AuditQueryParams
            {
                EventType = eventType,
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = pageSize
            };

            var logs = await auditService.GetAuditLogsAsync(queryParams);
            return Results.Ok(logs);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get audit logs: {ex.Message}");
        }
    }

    private static async Task<IResult> GetSecurityEvents(
        ClaimsPrincipal user,
        INotificationAuditService auditService,
        [FromQuery] DateTime? since = null)
    {
        try
        {
            var events = await auditService.GetSecurityEventsAsync(since);
            return Results.Ok(events);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get security events: {ex.Message}");
        }
    }

    private static async Task<IResult> BlockUser(
        int userId,
        ClaimsPrincipal user,
        INotificationRateLimitService rateLimitService,
        [FromQuery] int durationHours = 1,
        [FromQuery] string reason = "Admin action")
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            await rateLimitService.BlockUserAsync(userId, TimeSpan.FromHours(durationHours), reason);
            return Results.Ok(new { message = $"User {userId} blocked for {durationHours} hours" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to block user: {ex.Message}");
        }
    }

    private static async Task<IResult> UnblockUser(
        int userId,
        ClaimsPrincipal user,
        INotificationRateLimitService rateLimitService)
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            await rateLimitService.UnblockUserAsync(userId);
            return Results.Ok(new { message = $"User {userId} unblocked successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to unblock user: {ex.Message}");
        }
    }

    private static async Task<IResult> CleanupAuditLogs(
        ClaimsPrincipal user,
        INotificationAuditService auditService,
        [FromQuery] int maxAgeDays = 90)
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            await auditService.CleanupOldLogsAsync(TimeSpan.FromDays(maxAgeDays));
            return Results.Ok(new { message = $"Cleaned up audit logs older than {maxAgeDays} days" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to cleanup audit logs: {ex.Message}");
        }
    }

    public class ValidateContentRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? ContentType { get; set; }
    }
}
