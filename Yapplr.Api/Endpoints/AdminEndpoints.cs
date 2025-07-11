using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.Authorization;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class AdminEndpoints
{
    private static int? GetCurrentUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            return null;

        if (int.TryParse(userIdClaim.Value, out var userId))
            return userId;

        return null;
    }

    public static void MapAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin").WithTags("Admin");

        // System Tags Management
        admin.MapGet("/system-tags", async (IAdminService adminService, SystemTagCategory? category = null, bool? isActive = null) =>
        {
            var tags = await adminService.GetSystemTagsAsync(category, isActive);
            return Results.Ok(tags);
        })
        .WithName("GetSystemTags")
        .WithSummary("Get system tags")
        .Produces<IEnumerable<SystemTagDto>>(200);

        admin.MapGet("/system-tags/{id:int}", async (int id, IAdminService adminService) =>
        {
            var tag = await adminService.GetSystemTagAsync(id);
            return tag == null ? Results.NotFound() : Results.Ok(tag);
        })
        .WithName("GetSystemTag")
        .WithSummary("Get system tag by ID")
        .Produces<SystemTagDto>(200)
        .Produces(404);

        admin.MapPost("/system-tags", async ([FromBody] CreateSystemTagDto createDto, IAdminService adminService) =>
        {
            var tag = await adminService.CreateSystemTagAsync(createDto);
            return Results.Created($"/api/admin/system-tags/{tag.Id}", tag);
        })
        .WithName("CreateSystemTag")
        .WithSummary("Create a new system tag")
        .RequireAuthorization("Admin")
        .Produces<SystemTagDto>(201);

        admin.MapPut("/system-tags/{id:int}", async (int id, [FromBody] UpdateSystemTagDto updateDto, IAdminService adminService) =>
        {
            var tag = await adminService.UpdateSystemTagAsync(id, updateDto);
            return tag == null ? Results.NotFound() : Results.Ok(tag);
        })
        .WithName("UpdateSystemTag")
        .WithSummary("Update a system tag")
        .RequireAuthorization("Admin")
        .Produces<SystemTagDto>(200)
        .Produces(404);

        admin.MapDelete("/system-tags/{id:int}", async (int id, IAdminService adminService) =>
        {
            var success = await adminService.DeleteSystemTagAsync(id);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSystemTag")
        .WithSummary("Delete a system tag")
        .RequireAuthorization("Admin")
        .Produces(204)
        .Produces(404);

        // User Management
        admin.MapGet("/users", async (IUserService userService, int page = 1, int pageSize = 25, UserStatus? status = null, UserRole? role = null) =>
        {
            var users = await userService.GetUsersForAdminAsync(page, pageSize, status, role);
            return Results.Ok(users);
        })
        .WithName("GetUsersForAdmin")
        .WithSummary("Get users for admin management")
        .Produces<IEnumerable<AdminUserDto>>(200);

        admin.MapGet("/users/{id:int}", async (int id, IUserService userService) =>
        {
            var user = await userService.GetUserForAdminAsync(id);
            return user == null ? Results.NotFound() : Results.Ok(user);
        })
        .WithName("GetUserForAdmin")
        .WithSummary("Get user details for admin")
        .Produces<AdminUserDto>(200)
        .Produces(404);

        admin.MapPost("/users/{id:int}/suspend", async (int id, [FromBody] SuspendUserDto suspendDto, ClaimsPrincipal user, IUserService userService, IAuditService auditService, IModerationMessageService moderationMessageService, YapplrDbContext context) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var success = await userService.SuspendUserAsync(id, currentUserId.Value, suspendDto.Reason, suspendDto.SuspendedUntil);

            if (success)
            {
                await auditService.LogUserSuspendedAsync(id, currentUserId.Value, suspendDto.Reason, suspendDto.SuspendedUntil);

                // Enhancement 2: Send detailed system message with appeal instructions
                var moderator = await context.Users.FindAsync(currentUserId.Value);
                if (moderator != null)
                {
                    await moderationMessageService.SendUserSuspensionMessageAsync(id, suspendDto.Reason, suspendDto.SuspendedUntil, moderator.Username);
                }
            }

            return success ? Results.Ok(new { message = "User suspended successfully" }) : Results.BadRequest(new { message = "Failed to suspend user" });
        })
        .WithName("SuspendUser")
        .WithSummary("Suspend a user")
        .Produces(200)
        .Produces(400);

        admin.MapPost("/users/{id:int}/unsuspend", async (int id, ClaimsPrincipal user, IUserService userService, IAuditService auditService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var success = await userService.UnsuspendUserAsync(id);

            if (success)
            {
                await auditService.LogUserUnsuspendedAsync(id, currentUserId.Value);
            }
            
            return success ? Results.Ok(new { message = "User unsuspended successfully" }) : Results.BadRequest(new { message = "Failed to unsuspend user" });
        })
        .WithName("UnsuspendUser")
        .WithSummary("Unsuspend a user")
        .Produces(200)
        .Produces(400);

        admin.MapPost("/users/{id:int}/ban", async (int id, [FromBody] BanUserDto banDto, ClaimsPrincipal user, IUserService userService, IAuditService auditService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var success = await userService.BanUserAsync(id, currentUserId.Value, banDto.Reason, banDto.IsShadowBan);

            if (success)
            {
                await auditService.LogUserBannedAsync(id, currentUserId.Value, banDto.Reason, banDto.IsShadowBan);
            }
            
            return success ? Results.Ok(new { message = banDto.IsShadowBan ? "User shadow banned successfully" : "User banned successfully" }) : Results.BadRequest(new { message = "Failed to ban user" });
        })
        .WithName("BanUser")
        .WithSummary("Ban or shadow ban a user")
        .Produces(200)
        .Produces(400);

        admin.MapPost("/users/{id:int}/unban", async (int id, ClaimsPrincipal user, IUserService userService, IAuditService auditService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var success = await userService.UnbanUserAsync(id);

            if (success)
            {
                await auditService.LogUserUnbannedAsync(id, currentUserId.Value);
            }
            
            return success ? Results.Ok(new { message = "User unbanned successfully" }) : Results.BadRequest(new { message = "Failed to unban user" });
        })
        .WithName("UnbanUser")
        .WithSummary("Unban a user")
        .Produces(200)
        .Produces(400);

        admin.MapPost("/users/{id:int}/change-role", async (int id, [FromBody] ChangeUserRoleDto roleDto, ClaimsPrincipal user, IUserService userService, IAuditService auditService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();
            
            // Get current user role for audit log
            var targetUser = await userService.GetUserEntityByIdAsync(id);
            if (targetUser == null)
            {
                return Results.NotFound();
            }
            
            var oldRole = targetUser.Role;
            var success = await userService.ChangeUserRoleAsync(id, currentUserId.Value, roleDto.Role, roleDto.Reason);

            if (success)
            {
                await auditService.LogUserRoleChangedAsync(id, currentUserId.Value, oldRole, roleDto.Role, roleDto.Reason);
            }
            
            return success ? Results.Ok(new { message = "User role changed successfully" }) : Results.BadRequest(new { message = "Failed to change user role" });
        })
        .WithName("ChangeUserRole")
        .WithSummary("Change a user's role")
        .RequireAuthorization("Admin")
        .Produces(200)
        .Produces(400)
        .Produces(404);

        // Content Moderation
        admin.MapGet("/posts", async (IAdminService adminService, int page = 1, int pageSize = 25, bool? isHidden = null) =>
        {
            var posts = await adminService.GetPostsForModerationAsync(page, pageSize, isHidden);
            return Results.Ok(posts);
        })
        .WithName("GetPostsForModeration")
        .WithSummary("Get posts for moderation")
        .Produces<IEnumerable<AdminPostDto>>(200);

        admin.MapGet("/posts/{id:int}", async (int id, IAdminService adminService) =>
        {
            var post = await adminService.GetPostForModerationAsync(id);
            return post == null ? Results.NotFound() : Results.Ok(post);
        })
        .WithName("GetPostForModeration")
        .WithSummary("Get post details for moderation")
        .Produces<AdminPostDto>(200)
        .Produces(404);

        admin.MapPost("/posts/{id:int}/hide", async (int id, [FromBody] HideContentDto hideDto, ClaimsPrincipal user, IAdminService adminService) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return Results.Unauthorized();
            }

            var currentUserId = int.Parse(userIdClaim.Value);
            var success = await adminService.HidePostAsync(id, currentUserId, hideDto.Reason);
            return success ? Results.Ok(new { message = "Post hidden successfully" }) : Results.BadRequest(new { message = "Failed to hide post" });
        })
        .WithName("HidePost")
        .WithSummary("Hide a post")
        .Produces(200)
        .Produces(400);

        admin.MapPost("/posts/{id:int}/unhide", async (int id, IAdminService adminService) =>
        {
            var success = await adminService.UnhidePostAsync(id);
            return success ? Results.Ok(new { message = "Post unhidden successfully" }) : Results.BadRequest(new { message = "Failed to unhide post" });
        })
        .WithName("UnhidePost")
        .WithSummary("Unhide a post")
        .Produces(200)
        .Produces(400);



        admin.MapPost("/posts/{id:int}/system-tags", async (int id, [FromBody] ApplySystemTagDto tagDto, ClaimsPrincipal user, IAdminService adminService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var success = await adminService.ApplySystemTagToPostAsync(id, tagDto.SystemTagId, currentUserId.Value, tagDto.Reason);
            return success ? Results.Ok(new { message = "System tag applied successfully" }) : Results.BadRequest(new { message = "Failed to apply system tag" });
        })
        .WithName("ApplySystemTagToPost")
        .WithSummary("Apply a system tag to a post")
        .Produces(200)
        .Produces(400);

        admin.MapDelete("/posts/{id:int}/system-tags/{tagId:int}", async (int id, int tagId, ClaimsPrincipal user, IAdminService adminService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var success = await adminService.RemoveSystemTagFromPostAsync(id, tagId, currentUserId.Value);
            return success ? Results.Ok(new { message = "System tag removed successfully" }) : Results.BadRequest(new { message = "Failed to remove system tag" });
        })
        .WithName("RemoveSystemTagFromPost")
        .WithSummary("Remove a system tag from a post")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(400);

        // Comment Moderation
        admin.MapGet("/comments", async (IAdminService adminService, int page = 1, int pageSize = 25, bool? isHidden = null) =>
        {
            var comments = await adminService.GetCommentsForModerationAsync(page, pageSize, isHidden);
            return Results.Ok(comments);
        })
        .WithName("GetCommentsForModeration")
        .WithSummary("Get comments for moderation")
        .RequireAuthorization("Moderator")
        .Produces<IEnumerable<AdminCommentDto>>(200);

        admin.MapGet("/comments/{id:int}", async (int id, IAdminService adminService) =>
        {
            var comment = await adminService.GetCommentForModerationAsync(id);
            return comment == null ? Results.NotFound() : Results.Ok(comment);
        })
        .WithName("GetCommentForModeration")
        .WithSummary("Get comment details for moderation")
        .RequireAuthorization("Moderator")
        .Produces<AdminCommentDto>(200)
        .Produces(404);

        admin.MapPost("/comments/{id:int}/hide", async (int id, [FromBody] HideContentDto hideDto, ClaimsPrincipal user, IAdminService adminService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var success = await adminService.HideCommentAsync(id, currentUserId.Value, hideDto.Reason);
            return success ? Results.Ok(new { message = "Comment hidden successfully" }) : Results.BadRequest(new { message = "Failed to hide comment" });
        })
        .WithName("HideComment")
        .WithSummary("Hide a comment")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(400);

        admin.MapPost("/comments/{id:int}/unhide", async (int id, IAdminService adminService) =>
        {
            var success = await adminService.UnhideCommentAsync(id);
            return success ? Results.Ok(new { message = "Comment unhidden successfully" }) : Results.BadRequest(new { message = "Failed to unhide comment" });
        })
        .WithName("UnhideComment")
        .WithSummary("Unhide a comment")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(400);

        admin.MapPost("/comments/{id:int}/system-tags", async (int id, [FromBody] ApplySystemTagDto tagDto, ClaimsPrincipal user, IAdminService adminService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var success = await adminService.ApplySystemTagToCommentAsync(id, tagDto.SystemTagId, currentUserId.Value, tagDto.Reason);
            return success ? Results.Ok(new { message = "System tag applied successfully" }) : Results.BadRequest(new { message = "Failed to apply system tag" });
        })
        .WithName("ApplySystemTagToComment")
        .WithSummary("Apply a system tag to a comment")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(400);

        // Analytics and Reporting
        admin.MapGet("/stats", async (IAdminService adminService) =>
        {
            var stats = await adminService.GetModerationStatsAsync();
            return Results.Ok(stats);
        })
        .WithName("GetModerationStats")
        .WithSummary("Get moderation statistics")
        .RequireAuthorization("Moderator")
        .Produces<ModerationStatsDto>(200);

        admin.MapGet("/queue", async (IAdminService adminService) =>
        {
            var queue = await adminService.GetContentQueueAsync();
            return Results.Ok(queue);
        })
        .WithName("GetContentQueue")
        .WithSummary("Get content moderation queue")
        .RequireAuthorization("Moderator")
        .Produces<ContentQueueDto>(200);

        admin.MapGet("/audit-logs", async (IAdminService adminService, int page = 1, int pageSize = 25, AuditAction? action = null, int? performedByUserId = null, int? targetUserId = null) =>
        {
            var logs = await adminService.GetAuditLogsAsync(page, pageSize, action, performedByUserId, targetUserId);
            return Results.Ok(logs);
        })
        .WithName("GetAdminAuditLogs")
        .WithSummary("Get admin audit logs")
        .RequireAuthorization("Moderator")
        .Produces<IEnumerable<AuditLogDto>>(200);

        // User Appeals
        admin.MapGet("/appeals", async (IAdminService adminService, int page = 1, int pageSize = 25, AppealStatus? status = null, AppealType? type = null, int? userId = null) =>
        {
            var appeals = await adminService.GetUserAppealsAsync(page, pageSize, status, type, userId);
            return Results.Ok(appeals);
        })
        .WithName("GetUserAppeals")
        .WithSummary("Get user appeals")
        .RequireAuthorization("Moderator")
        .Produces<IEnumerable<UserAppealDto>>(200);

        admin.MapGet("/appeals/{id:int}", async (int id, IAdminService adminService) =>
        {
            var appeal = await adminService.GetUserAppealAsync(id);
            return appeal == null ? Results.NotFound() : Results.Ok(appeal);
        })
        .WithName("GetUserAppeal")
        .WithSummary("Get user appeal by ID")
        .RequireAuthorization("Moderator")
        .Produces<UserAppealDto>(200)
        .Produces(404);

        admin.MapPost("/appeals/{id:int}/review", async (int id, [FromBody] ReviewAppealDto reviewDto, ClaimsPrincipal user, IAdminService adminService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var appeal = await adminService.ReviewUserAppealAsync(id, currentUserId.Value, reviewDto);
            return appeal == null ? Results.NotFound() : Results.Ok(appeal);
        })
        .WithName("ReviewUserAppeal")
        .WithSummary("Review a user appeal")
        .RequireAuthorization("Moderator")
        .Produces<UserAppealDto>(200)
        .Produces(404);

        // Bulk Actions
        admin.MapPost("/bulk/hide-posts", async ([FromBody] BulkActionDto bulkDto, ClaimsPrincipal user, IAdminService adminService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var count = await adminService.BulkHidePostsAsync(bulkDto.PostIds, currentUserId.Value, bulkDto.Reason);
            return Results.Ok(new { message = $"{count} posts hidden successfully", count });
        })
        .WithName("BulkHidePosts")
        .WithSummary("Bulk hide posts")
        .RequireAuthorization("Moderator")
        .Produces(200);



        admin.MapPost("/bulk/apply-system-tag", async ([FromBody] BulkSystemTagDto bulkTagDto, ClaimsPrincipal user, IAdminService adminService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var count = await adminService.BulkApplySystemTagAsync(bulkTagDto.PostIds, bulkTagDto.SystemTagId, currentUserId.Value, bulkTagDto.Reason);
            return Results.Ok(new { message = $"System tag applied to {count} posts successfully", count });
        })
        .WithName("BulkApplySystemTag")
        .WithSummary("Bulk apply system tag to posts")
        .RequireAuthorization("Moderator")
        .Produces(200);

        // User Appeals (accessible to all authenticated users)
        admin.MapPost("/appeals", async ([FromBody] CreateAppealDto createDto, ClaimsPrincipal user, IAdminService adminService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var appeal = await adminService.CreateUserAppealAsync(currentUserId.Value, createDto);
            return Results.Created($"/api/admin/appeals/{appeal.Id}", appeal);
        })
        .WithName("CreateUserAppeal")
        .WithSummary("Create a user appeal")
        .Produces<UserAppealDto>(201)
        .RequireAuthorization(); // Only require authentication, not admin role

        // User Report Management
        admin.MapGet("/reports", async (IUserReportService userReportService, int page = 1, int pageSize = 50) =>
        {
            var reports = await userReportService.GetAllReportsAsync(page, pageSize);
            return Results.Ok(reports);
        })
        .WithName("GetAllUserReports")
        .WithSummary("Get all user reports")
        .RequireAuthorization("Moderator")
        .Produces<IEnumerable<UserReportDto>>(200);

        admin.MapGet("/reports/{id:int}", async (int id, IUserReportService userReportService) =>
        {
            var report = await userReportService.GetReportByIdAsync(id);
            return report == null ? Results.NotFound() : Results.Ok(report);
        })
        .WithName("GetUserReport")
        .WithSummary("Get a specific user report")
        .RequireAuthorization("Moderator")
        .Produces<UserReportDto>(200)
        .Produces(404);

        admin.MapPost("/reports/{id:int}/review", async (int id, ReviewUserReportDto dto, ClaimsPrincipal user, IUserReportService userReportService) =>
        {
            var userId = user.GetUserId(true);
            var report = await userReportService.ReviewReportAsync(id, userId, dto);
            return report == null ? Results.NotFound() : Results.Ok(report);
        })
        .WithName("ReviewUserReport")
        .WithSummary("Review a user report")
        .RequireAuthorization("Moderator")
        .Produces<UserReportDto>(200)
        .Produces(404);

        admin.MapPost("/reports/{id:int}/hide-content", async (int id, HideContentFromReportDto dto, ClaimsPrincipal user, IUserReportService userReportService) =>
        {
            var userId = user.GetUserId(true);
            var result = await userReportService.HideContentFromReportAsync(id, userId, dto.Reason);
            return result ? Results.Ok(new { message = "Content hidden and users notified" }) : Results.BadRequest(new { message = "Failed to hide content" });
        })
        .WithName("HideContentFromReport")
        .WithSummary("Hide content from a user report and notify both users")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(400);

        // Enhanced Analytics Endpoints
        admin.MapGet("/analytics/user-growth", async ([FromQuery] int days, IAdminService adminService) =>
        {
            var stats = await adminService.GetUserGrowthStatsAsync(days);
            return Results.Ok(stats);
        })
        .WithName("GetUserGrowthStats")
        .WithSummary("Get user growth analytics")
        .RequireAuthorization("Moderator")
        .Produces<UserGrowthStatsDto>();

        admin.MapGet("/analytics/content-stats", async ([FromQuery] int days, IAdminService adminService) =>
        {
            var stats = await adminService.GetContentStatsAsync(days);
            return Results.Ok(stats);
        })
        .WithName("GetContentStats")
        .WithSummary("Get content creation analytics")
        .RequireAuthorization("Moderator")
        .Produces<ContentStatsDto>();

        admin.MapGet("/analytics/moderation-trends", async ([FromQuery] int days, IAdminService adminService) =>
        {
            var stats = await adminService.GetModerationTrendsAsync(days);
            return Results.Ok(stats);
        })
        .WithName("GetModerationTrends")
        .WithSummary("Get moderation trends analytics")
        .RequireAuthorization("Moderator")
        .Produces<ModerationTrendsDto>();

        admin.MapGet("/analytics/system-health", async (IAdminService adminService) =>
        {
            var health = await adminService.GetSystemHealthAsync();
            return Results.Ok(health);
        })
        .WithName("GetSystemHealth")
        .WithSummary("Get system health metrics")
        .RequireAuthorization("Moderator")
        .Produces<SystemHealthDto>();

        admin.MapGet("/analytics/top-moderators", async ([FromQuery] int days, [FromQuery] int limit, IAdminService adminService) =>
        {
            var stats = await adminService.GetTopModeratorsAsync(days, limit);
            return Results.Ok(stats);
        })
        .WithName("GetTopModerators")
        .WithSummary("Get top moderators analytics")
        .RequireAuthorization("Moderator")
        .Produces<TopModeratorsDto>();

        admin.MapGet("/analytics/content-trends", async ([FromQuery] int days, IAdminService adminService) =>
        {
            var trends = await adminService.GetContentTrendsAsync(days);
            return Results.Ok(trends);
        })
        .WithName("GetContentTrends")
        .WithSummary("Get content trends analytics")
        .RequireAuthorization("Moderator")
        .Produces<ContentTrendsDto>();

        admin.MapGet("/analytics/user-engagement", async ([FromQuery] int days, IAdminService adminService) =>
        {
            var engagement = await adminService.GetUserEngagementStatsAsync(days);
            return Results.Ok(engagement);
        })
        .WithName("GetUserEngagement")
        .WithSummary("Get user engagement analytics")
        .RequireAuthorization("Moderator")
        .Produces<UserEngagementStatsDto>();

        // AI Suggested Tags Management
        admin.MapGet("/ai-suggestions", async ([FromQuery] int? postId, [FromQuery] int? commentId, [FromQuery] int page, [FromQuery] int pageSize, IAdminService adminService) =>
        {
            var suggestions = await adminService.GetPendingAiSuggestionsAsync(postId, commentId, page, pageSize);
            return Results.Ok(suggestions);
        })
        .WithName("GetPendingAiSuggestions")
        .WithSummary("Get pending AI suggested tags")
        .RequireAuthorization("Moderator")
        .Produces<IEnumerable<AiSuggestedTagDto>>();

        admin.MapPost("/ai-suggestions/{id:int}/approve", async (int id, [FromBody] ApprovalDto approvalDto, IAdminService adminService, ClaimsPrincipal user) =>
        {
            var userId = GetCurrentUserId(user);
            if (userId == null)
                return Results.Unauthorized();

            var success = await adminService.ApproveAiSuggestedTagAsync(id, userId.Value, approvalDto.Reason);
            return success ? Results.Ok() : Results.NotFound();
        })
        .WithName("ApproveAiSuggestedTag")
        .WithSummary("Approve an AI suggested tag")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(404);

        admin.MapPost("/ai-suggestions/{id:int}/reject", async (int id, [FromBody] ApprovalDto approvalDto, IAdminService adminService, ClaimsPrincipal user) =>
        {
            var userId = GetCurrentUserId(user);
            if (userId == null)
                return Results.Unauthorized();

            var success = await adminService.RejectAiSuggestedTagAsync(id, userId.Value, approvalDto.Reason);
            return success ? Results.Ok() : Results.NotFound();
        })
        .WithName("RejectAiSuggestedTag")
        .WithSummary("Reject an AI suggested tag")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(404);

        admin.MapPost("/ai-suggestions/bulk-approve", async ([FromBody] BulkApprovalDto bulkApprovalDto, IAdminService adminService, ClaimsPrincipal user) =>
        {
            var userId = GetCurrentUserId(user);
            if (userId == null)
                return Results.Unauthorized();

            var success = await adminService.BulkApproveAiSuggestedTagsAsync(bulkApprovalDto.SuggestedTagIds, userId.Value, bulkApprovalDto.Reason);
            return success ? Results.Ok() : Results.BadRequest();
        })
        .WithName("BulkApproveAiSuggestedTags")
        .WithSummary("Bulk approve AI suggested tags")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(400);

        admin.MapPost("/ai-suggestions/bulk-reject", async ([FromBody] BulkApprovalDto bulkApprovalDto, IAdminService adminService, ClaimsPrincipal user) =>
        {
            var userId = GetCurrentUserId(user);
            if (userId == null)
                return Results.Unauthorized();

            var success = await adminService.BulkRejectAiSuggestedTagsAsync(bulkApprovalDto.SuggestedTagIds, userId.Value, bulkApprovalDto.Reason);
            return success ? Results.Ok() : Results.BadRequest();
        })
        .WithName("BulkRejectAiSuggestedTags")
        .WithSummary("Bulk reject AI suggested tags")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(400);

        // Content Management Endpoints
        admin.MapGet("/content-pages", async (IContentManagementService contentService) =>
        {
            var pages = await contentService.GetContentPagesAsync();
            return Results.Ok(pages);
        })
        .WithName("GetContentPages")
        .WithSummary("Get all content pages")
        .RequireAuthorization("Moderator")
        .Produces<IEnumerable<ContentPageDto>>(200);

        admin.MapGet("/content-pages/{id:int}", async (int id, IContentManagementService contentService) =>
        {
            var page = await contentService.GetContentPageAsync(id);
            return page == null ? Results.NotFound() : Results.Ok(page);
        })
        .WithName("GetContentPage")
        .WithSummary("Get content page by ID")
        .RequireAuthorization("Moderator")
        .Produces<ContentPageDto>(200)
        .Produces(404);

        admin.MapGet("/content-pages/{id:int}/versions", async (int id, IContentManagementService contentService) =>
        {
            var versions = await contentService.GetContentPageVersionsAsync(id);
            return Results.Ok(versions);
        })
        .WithName("GetContentPageVersions")
        .WithSummary("Get all versions of a content page")
        .RequireAuthorization("Moderator")
        .Produces<IEnumerable<ContentPageVersionDto>>(200);

        admin.MapGet("/content-pages/versions/{versionId:int}", async (int versionId, IContentManagementService contentService) =>
        {
            var version = await contentService.GetContentPageVersionAsync(versionId);
            return version == null ? Results.NotFound() : Results.Ok(version);
        })
        .WithName("GetContentPageVersion")
        .WithSummary("Get specific content page version")
        .RequireAuthorization("Moderator")
        .Produces<ContentPageVersionDto>(200)
        .Produces(404);

        admin.MapPost("/content-pages/{id:int}/versions", async (int id, [FromBody] CreateContentPageVersionDto createDto, ClaimsPrincipal user, IContentManagementService contentService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            try
            {
                var version = await contentService.CreateContentPageVersionAsync(id, createDto, currentUserId.Value);
                return Results.Created($"/api/admin/content-pages/versions/{version.Id}", version);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("CreateContentPageVersion")
        .WithSummary("Create a new version of a content page")
        .RequireAuthorization("Moderator")
        .Produces<ContentPageVersionDto>(201)
        .Produces(400)
        .Produces(401);

        admin.MapPost("/content-pages/{id:int}/publish", async (int id, [FromBody] PublishContentVersionDto publishDto, ClaimsPrincipal user, IContentManagementService contentService) =>
        {
            var currentUserId = GetCurrentUserId(user);
            if (currentUserId == null)
                return Results.Unauthorized();

            var success = await contentService.PublishContentPageVersionAsync(id, publishDto.VersionId, currentUserId.Value);
            return success ? Results.Ok(new { message = "Version published successfully" }) : Results.BadRequest(new { message = "Failed to publish version" });
        })
        .WithName("PublishContentPageVersion")
        .WithSummary("Publish a specific version of a content page")
        .RequireAuthorization("Moderator")
        .Produces(200)
        .Produces(400)
        .Produces(401);
    }

    // Additional DTOs for bulk actions
    public class BulkActionDto
    {
        public IEnumerable<int> PostIds { get; set; } = new List<int>();
        public string Reason { get; set; } = string.Empty;
    }

    public class BulkSystemTagDto
    {
        public IEnumerable<int> PostIds { get; set; } = new List<int>();
        public int SystemTagId { get; set; }
        public string? Reason { get; set; }
    }

    public class ApprovalDto
    {
        public string? Reason { get; set; }
    }

    public class BulkApprovalDto
    {
        public IEnumerable<int> SuggestedTagIds { get; set; } = new List<int>();
        public string? Reason { get; set; }
    }
}
