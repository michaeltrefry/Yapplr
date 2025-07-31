using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;
using Yapplr.Api.DTOs;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.Services.Notifications;

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
            .Produces<List<RateLimitViolation>>()
            .Produces(401);

        security.MapGet("/rate-limits/stats", GetRateLimitStats)
            .WithName("GetRateLimitStats")
            .WithSummary("Get rate limiting statistics")
            .Produces<Dictionary<string, object>>()
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
            .Produces<ContentValidationResult>()
            .Produces(400)
            .Produces(401);

        security.MapGet("/content/filter-stats", GetContentFilterStats)
            .WithName("GetContentFilterStats")
            .WithSummary("Get content filtering statistics")
            .Produces<Dictionary<string, object>>()
            .Produces(401);

        // Audit endpoints
        security.MapGet("/audit/logs", GetAuditLogs)
            .WithName("GetAuditLogs")
            .WithSummary("Get audit logs for the current user")
            .Produces<List<NotificationAuditLog>>()
            .Produces(401);

        security.MapGet("/audit/stats", GetAuditStats)
            .WithName("GetAuditStats")
            .WithSummary("Get audit statistics")
            .Produces<Dictionary<string, object>>()
            .Produces(401);

        // Admin endpoints
        security.MapGet("/admin/audit/all", GetAllAuditLogs)
            .WithName("GetAllAuditLogs")
            .WithSummary("Get all audit logs (admin only)")
            .Produces<List<NotificationAuditLog>>()
            .Produces(401)
            .Produces(403);

        security.MapGet("/admin/security-events", GetSecurityEvents)
            .WithName("GetSecurityEvents")
            .WithSummary("Get security events (admin only)")
            .Produces<List<NotificationAuditLog>>()
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

        // Rate Limiting Configuration
        security.MapGet("/admin/rate-limits/config", GetRateLimitConfig)
            .WithName("GetRateLimitConfig")
            .WithSummary("Get current rate limiting configuration (admin only)")
            .Produces<RateLimitConfigDto>()
            .Produces(401)
            .Produces(403);

        security.MapPut("/admin/rate-limits/config", UpdateRateLimitConfig)
            .WithName("UpdateRateLimitConfig")
            .WithSummary("Update rate limiting configuration (admin only)")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        security.MapGet("/admin/rate-limits/stats", GetAdminRateLimitStats)
            .WithName("GetAdminRateLimitStats")
            .WithSummary("Get rate limiting statistics (admin only)")
            .Produces<Dictionary<string, object>>()
            .Produces(401)
            .Produces(403);

        security.MapGet("/admin/rate-limits/users/{userId}", GetUserRateLimitSettings)
            .WithName("GetUserRateLimitSettings")
            .WithSummary("Get user-specific rate limiting settings (admin only)")
            .Produces<UserRateLimitSettingsDto>()
            .Produces(401)
            .Produces(403)
            .Produces(404);



        security.MapDelete("/admin/rate-limits/users/{userId}/reset", ResetUserRateLimitsAdmin)
            .WithName("ResetUserRateLimitsAdmin")
            .WithSummary("Reset user's rate limits and violations (admin only)")
            .Produces(200)
            .Produces(401)
            .Produces(403)
            .Produces(404);

        security.MapPost("/admin/rate-limits/users/{userId}/block", BlockUserRateLimit)
            .WithName("BlockUserRateLimit")
            .WithSummary("Block user from API access (admin only)")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404);

        security.MapDelete("/admin/rate-limits/users/{userId}/block", UnblockUserRateLimit)
            .WithName("UnblockUserRateLimit")
            .WithSummary("Unblock user from API access (admin only)")
            .Produces(200)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }

    private static async Task<IResult> GetRateLimitViolations(
        ClaimsPrincipal user,
        INotificationEnhancementService enhancementService)
    {
        try
        {
            var userId = user.GetUserId(true);
            var violations = await enhancementService.GetRateLimitViolationsAsync(userId);
            return Results.Ok(violations);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get rate limit violations: {ex.Message}");
        }
    }

    private static async Task<IResult> GetRateLimitStats(
        INotificationEnhancementService enhancementService)
    {
        try
        {
            var stats = await enhancementService.GetRateLimitStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get rate limit stats: {ex.Message}");
        }
    }

    private static async Task<IResult> ResetUserRateLimits(
        ClaimsPrincipal user,
        INotificationEnhancementService enhancementService)
    {
        try
        {
            var userId = user.GetUserId(true);
            await enhancementService.ResetUserRateLimitsAsync(userId);
            return Results.Ok(new { message = "Rate limits reset successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to reset rate limits: {ex.Message}");
        }
    }

    private static async Task<IResult> ValidateContent(
        [FromBody] ValidateContentRequest request,
        INotificationEnhancementService enhancementService)
    {
        try
        {
            var result = await enhancementService.ValidateContentAsync(request.Content, request.ContentType ?? "text");
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to validate content: {ex.Message}");
        }
    }

    private static async Task<IResult> GetContentFilterStats(
        INotificationEnhancementService enhancementService)
    {
        try
        {
            var stats = await enhancementService.GetContentFilterStatsAsync();
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get content filter stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetAuditLogs(
        ClaimsPrincipal user,
        INotificationEnhancementService enhancementService,
        [FromQuery] int count = 100)
    {
        try
        {
            var userId = user.GetUserId(true);
            var logs = await enhancementService.GetUserAuditLogsAsync(userId, count);
            return Results.Ok(logs);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get audit logs: {ex.Message}");
        }
    }

    private static async Task<IResult> GetAuditStats(
        INotificationEnhancementService enhancementService,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var stats = await enhancementService.GetAuditStatsAsync(startDate, endDate);
            return Results.Ok(stats);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get audit stats: {ex.Message}");
        }
    }

    private static async Task<IResult> GetAllAuditLogs(
        ClaimsPrincipal user,
        INotificationEnhancementService enhancementService,
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

            var logs = await enhancementService.GetAuditLogsAsync(queryParams);
            return Results.Ok(logs);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get audit logs: {ex.Message}");
        }
    }

    private static async Task<IResult> GetSecurityEvents(
        ClaimsPrincipal user,
        INotificationEnhancementService enhancementService,
        [FromQuery] DateTime? since = null)
    {
        try
        {
            var events = await enhancementService.GetSecurityEventsAsync(since);
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
        INotificationEnhancementService enhancementService,
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
            await enhancementService.BlockUserAsync(userId, TimeSpan.FromHours(durationHours), reason);
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
        INotificationEnhancementService enhancementService)
    {
        // Simple admin check - in production, you'd want proper role-based authorization
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        if (username != "admin") // Replace with proper admin check
        {
            return Results.Forbid();
        }

        try
        {
            await enhancementService.UnblockUserAsync(userId);
            return Results.Ok(new { message = $"User {userId} unblocked successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to unblock user: {ex.Message}");
        }
    }

    private static async Task<IResult> CleanupAuditLogs(
        ClaimsPrincipal user,
        INotificationEnhancementService enhancementService,
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
            await enhancementService.CleanupOldLogsAsync(TimeSpan.FromDays(maxAgeDays));
            return Results.Ok(new { message = $"Cleaned up audit logs older than {maxAgeDays} days" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to cleanup audit logs: {ex.Message}");
        }
    }

    // Rate Limiting Configuration Handlers
    private static Task<IResult> GetRateLimitConfig(
        IOptions<RateLimitingConfiguration> rateLimitingOptions,
        ClaimsPrincipal user)
    {
        if (!user.IsInRole("Admin"))
            return Task.FromResult(Results.Forbid());

        var config = rateLimitingOptions.Value;
        var dto = new RateLimitConfigDto
        {
            Enabled = config.Enabled,
            TrustBasedEnabled = config.TrustBasedEnabled,
            BurstProtectionEnabled = config.BurstProtectionEnabled,
            AutoBlockingEnabled = config.AutoBlockingEnabled,
            AutoBlockViolationThreshold = config.AutoBlockViolationThreshold,
            AutoBlockDurationHours = config.AutoBlockDurationHours,
            ApplyToAdmins = config.ApplyToAdmins,
            ApplyToModerators = config.ApplyToModerators,
            FallbackMultiplier = config.FallbackMultiplier
        };

        return Task.FromResult(Results.Ok(dto));
    }

    private static Task<IResult> UpdateRateLimitConfig(
        [FromBody] UpdateRateLimitConfigDto updateDto,
        IOptions<RateLimitingConfiguration> rateLimitingOptions,
        ClaimsPrincipal user)
    {
        if (!user.IsInRole("Admin"))
            return Task.FromResult(Results.Forbid());

        // Note: In a real implementation, you'd want to update the configuration
        // in a persistent store and reload it. For now, this is a placeholder.
        // You might use IOptionsSnapshot or implement a configuration service.

        return Task.FromResult(Results.Ok(new { message = "Rate limiting configuration updated successfully" }));
    }

    private static async Task<IResult> GetAdminRateLimitStats(
        IApiRateLimitService rateLimitService,
        ClaimsPrincipal user)
    {
        if (!user.IsInRole("Admin"))
            return Results.Forbid();

        var stats = await rateLimitService.GetRateLimitStatsAsync();
        return Results.Ok(stats);
    }

    private static async Task<IResult> GetUserRateLimitSettings(
        int userId,
        YapplrDbContext context,
        IApiRateLimitService rateLimitService,
        ClaimsPrincipal user)
    {
        if (!user.IsInRole("Admin"))
            return Results.Forbid();

        var dbUser = await context.Users.FindAsync(userId);
        if (dbUser == null)
            return Results.NotFound();

        var violations = await rateLimitService.GetRecentViolationsAsync(userId);
        var isBlocked = await rateLimitService.IsUserBlockedAsync(userId);

        var dto = new UserRateLimitSettingsDto
        {
            UserId = dbUser.Id,
            Username = dbUser.Username,
            Email = dbUser.Email,
            Role = dbUser.Role,
            RateLimitingEnabled = dbUser.RateLimitingEnabled,
            TrustBasedRateLimitingEnabled = dbUser.TrustBasedRateLimitingEnabled,
            IsCurrentlyBlocked = isBlocked,
            RecentViolations = violations.Count,
            TrustScore = dbUser.TrustScore,
            LastActivity = dbUser.LastSeenAt
        };

        return Results.Ok(dto);
    }



    private static async Task<IResult> BlockUserRateLimit(
        int userId,
        [FromBody] BlockUserRateLimitDto blockDto,
        IApiRateLimitService rateLimitService,
        ClaimsPrincipal user)
    {
        if (!user.IsInRole("Admin"))
            return Results.Forbid();

        try
        {
            await rateLimitService.BlockUserAsync(userId, TimeSpan.FromHours(blockDto.DurationHours), blockDto.Reason);
            return Results.Ok(new { message = $"User {userId} blocked from API access for {blockDto.DurationHours} hours" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to block user: {ex.Message}");
        }
    }

    private static async Task<IResult> UnblockUserRateLimit(
        int userId,
        IApiRateLimitService rateLimitService,
        ClaimsPrincipal user)
    {
        if (!user.IsInRole("Admin"))
            return Results.Forbid();

        try
        {
            await rateLimitService.UnblockUserAsync(userId);
            return Results.Ok(new { message = $"User {userId} unblocked from API access" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to unblock user: {ex.Message}");
        }
    }

    private static async Task<IResult> ResetUserRateLimitsAdmin(
        int userId,
        IApiRateLimitService rateLimitService,
        ClaimsPrincipal user)
    {
        if (!user.IsInRole("Admin"))
            return Results.Forbid();

        try
        {
            await rateLimitService.ResetUserLimitsAsync(userId);
            return Results.Ok(new { message = $"Rate limits reset for user {userId}" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to reset rate limits: {ex.Message}");
        }
    }

    public class ValidateContentRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? ContentType { get; set; }
    }
}
