using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;
using Yapplr.Api.Extensions;
using Yapplr.Api.Common;

namespace Yapplr.Api.Endpoints;

public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this WebApplication app)
    {
        var groups = app.MapGroup("/api/groups").WithTags("Groups");

        // Create group
        groups.MapPost("/", async ([FromBody] CreateGroupDto createDto, ClaimsPrincipal user, IGroupService groupService) =>
        {
            var userId = user.GetUserId(true);
            var result = await groupService.CreateGroupAsync(userId, createDto);
            
            return result.IsSuccess 
                ? Results.Created($"/api/groups/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("CreateGroup")
        .WithSummary("Create a new group")
        .WithDescription("Create a new group. The creator automatically becomes the group owner and first member.")
        .RequireAuthorization("ActiveUser")
        .Produces<GroupDto>(201)
        .Produces(400)
        .Produces(401);

        // Get all groups (paginated)
        groups.MapGet("/", async (ClaimsPrincipal user, IGroupService groupService, int page = 1, int pageSize = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var result = await groupService.GetGroupsAsync(currentUserId, page, pageSize);
            
            return Results.Ok(result);
        })
        .WithName("GetGroups")
        .WithSummary("Get all groups")
        .WithDescription("Get a paginated list of all groups.")
        .Produces<PaginatedResult<GroupListDto>>(200);

        // Search groups
        groups.MapGet("/search", async (string query, ClaimsPrincipal user, IGroupService groupService, int page = 1, int pageSize = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var result = await groupService.SearchGroupsAsync(query, currentUserId, page, pageSize);
            
            return Results.Ok(result);
        })
        .WithName("SearchGroups")
        .WithSummary("Search groups")
        .WithDescription("Search groups by name or description.")
        .Produces<PaginatedResult<GroupListDto>>(200);

        // Get group by ID
        groups.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, IGroupService groupService) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var group = await groupService.GetGroupByIdAsync(id, currentUserId);
            
            return group == null ? Results.NotFound() : Results.Ok(group);
        })
        .WithName("GetGroupById")
        .WithSummary("Get group by ID")
        .WithDescription("Get detailed information about a specific group.")
        .Produces<GroupDto>(200)
        .Produces(404);

        // Get group by name
        groups.MapGet("/name/{name}", async (string name, ClaimsPrincipal user, IGroupService groupService) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var group = await groupService.GetGroupByNameAsync(name, currentUserId);
            
            return group == null ? Results.NotFound() : Results.Ok(group);
        })
        .WithName("GetGroupByName")
        .WithSummary("Get group by name")
        .WithDescription("Get detailed information about a specific group by its name.")
        .Produces<GroupDto>(200)
        .Produces(404);

        // Update group
        groups.MapPut("/{id:int}", async (int id, [FromBody] UpdateGroupDto updateDto, ClaimsPrincipal user, IGroupService groupService) =>
        {
            var userId = user.GetUserId(true);
            var result = await groupService.UpdateGroupAsync(id, userId, updateDto);
            
            return result.IsSuccess 
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("UpdateGroup")
        .WithSummary("Update group")
        .WithDescription("Update group information. Only the group owner can update the group.")
        .RequireAuthorization("ActiveUser")
        .Produces<GroupDto>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);

        // Delete group
        groups.MapDelete("/{id:int}", async (int id, ClaimsPrincipal user, IGroupService groupService) =>
        {
            var userId = user.GetUserId(true);
            var result = await groupService.DeleteGroupAsync(id, userId);
            
            return result.IsSuccess 
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("DeleteGroup")
        .WithSummary("Delete group")
        .WithDescription("Delete a group. Only the group owner can delete the group.")
        .RequireAuthorization("ActiveUser")
        .Produces(204)
        .Produces(400)
        .Produces(401)
        .Produces(403);

        // Join group
        groups.MapPost("/{id:int}/join", async (int id, ClaimsPrincipal user, IGroupService groupService) =>
        {
            var userId = user.GetUserId(true);
            var result = await groupService.JoinGroupAsync(id, userId);
            
            return result.IsSuccess 
                ? Results.Ok(new { message = "Successfully joined the group" })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("JoinGroup")
        .WithSummary("Join group")
        .WithDescription("Join a group. All groups are currently open, so anyone can join.")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Leave group
        groups.MapPost("/{id:int}/leave", async (int id, ClaimsPrincipal user, IGroupService groupService) =>
        {
            var userId = user.GetUserId(true);
            var result = await groupService.LeaveGroupAsync(id, userId);
            
            return result.IsSuccess 
                ? Results.Ok(new { message = "Successfully left the group" })
                : Results.BadRequest(new { error = result.ErrorMessage });
        })
        .WithName("LeaveGroup")
        .WithSummary("Leave group")
        .WithDescription("Leave a group. Group owners cannot leave their own group.")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Get group members
        groups.MapGet("/{id:int}/members", async (int id, IGroupService groupService, int page = 1, int pageSize = 20) =>
        {
            var result = await groupService.GetGroupMembersAsync(id, page, pageSize);

            return Results.Ok(result);
        })
        .WithName("GetGroupMembers")
        .WithSummary("Get group members")
        .WithDescription("Get a paginated list of group members.")
        .Produces<PaginatedResult<GroupMemberDto>>(200);

        // Get group posts
        groups.MapGet("/{id:int}/posts", async (int id, ClaimsPrincipal user, IGroupService groupService, int page = 1, int pageSize = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var result = await groupService.GetGroupPostsAsync(id, currentUserId, page, pageSize);

            return Results.Ok(result);
        })
        .WithName("GetGroupPosts")
        .WithSummary("Get group posts")
        .WithDescription("Get a paginated list of posts in a group. Posts in groups are always public.")
        .Produces<PaginatedResult<PostDto>>(200);

        // Get user's groups
        groups.MapGet("/user/{userId:int}", async (int userId, ClaimsPrincipal user, IGroupService groupService, int page = 1, int pageSize = 20) =>
        {
            var currentUserId = user.GetUserIdOrNull();
            var result = await groupService.GetUserGroupsAsync(userId, currentUserId, page, pageSize);

            return Results.Ok(result);
        })
        .WithName("GetUserGroups")
        .WithSummary("Get user's groups")
        .WithDescription("Get a paginated list of groups that a user is a member of.")
        .Produces<PaginatedResult<GroupListDto>>(200);

        // Get current user's groups
        groups.MapGet("/me", async (ClaimsPrincipal user, IGroupService groupService, int page = 1, int pageSize = 20) =>
        {
            var userId = user.GetUserId(true);
            var result = await groupService.GetUserGroupsAsync(userId, userId, page, pageSize);

            return Results.Ok(result);
        })
        .WithName("GetMyGroups")
        .WithSummary("Get current user's groups")
        .WithDescription("Get a paginated list of groups that the current user is a member of.")
        .RequireAuthorization("User")
        .Produces<PaginatedResult<GroupListDto>>(200)
        .Produces(401);
    }
}
