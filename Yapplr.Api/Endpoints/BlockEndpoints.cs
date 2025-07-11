using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Yapplr.Api.Authorization;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class BlockEndpoints
{
    public static void MapBlockEndpoints(this WebApplication app)
    {
        var blocks = app.MapGroup("/api/blocks").WithTags("Blocks");

        // Block a user
        blocks.MapPost("/users/{userId:int}", [RequireActiveUser] async (int userId, ClaimsPrincipal user, IBlockService blockService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await blockService.BlockUserAsync(currentUserId, userId);

            return success ? Results.Ok(new { message = "User blocked successfully" }) : Results.BadRequest(new { message = "Unable to block user" });
        })
        .WithName("BlockUser")
        .WithSummary("Block a user")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Unblock a user
        blocks.MapDelete("/users/{userId:int}", [RequireActiveUser] async (int userId, ClaimsPrincipal user, IBlockService blockService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await blockService.UnblockUserAsync(currentUserId, userId);

            return success ? Results.Ok(new { message = "User unblocked successfully" }) : Results.BadRequest(new { message = "Unable to unblock user" });
        })
        .WithName("UnblockUser")
        .WithSummary("Unblock a user")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Check if user is blocked
        blocks.MapGet("/users/{userId:int}/status", [RequireActiveUser] async (int userId, ClaimsPrincipal user, IBlockService blockService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var isBlocked = await blockService.IsUserBlockedAsync(currentUserId, userId);

            return Results.Ok(new { isBlocked });
        })
        .WithName("GetBlockStatus")
        .WithSummary("Check if a user is blocked")
        .Produces(200)
        .Produces(401);

        // Get list of blocked users
        blocks.MapGet("/", [RequireActiveUser] async (ClaimsPrincipal user, IBlockService blockService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var blockedUsers = await blockService.GetBlockedUsersAsync(currentUserId);

            return Results.Ok(blockedUsers);
        })
        .WithName("GetBlockedUsers")
        .WithSummary("Get list of blocked users")
        .Produces(200)
        .Produces(401);
    }
}
