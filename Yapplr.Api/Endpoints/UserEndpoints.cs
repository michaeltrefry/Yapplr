using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/api/users").WithTags("Users");

        users.MapGet("/me", [Authorize] async (ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userDto = await userService.GetUserByIdAsync(userId);
            
            return userDto == null ? Results.NotFound() : Results.Ok(userDto);
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get current user profile")
        .Produces<UserDto>(200)
        .Produces(401)
        .Produces(404);

        users.MapPut("/me", [Authorize] async ([FromBody] UpdateUserDto updateDto, ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userDto = await userService.UpdateUserAsync(userId, updateDto);
            
            return userDto == null ? Results.NotFound() : Results.Ok(userDto);
        })
        .WithName("UpdateCurrentUser")
        .WithSummary("Update current user profile")
        .Produces<UserDto>(200)
        .Produces(401)
        .Produces(404);

        users.MapGet("/{username}", [Authorize] async (string username, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = await userService.GetUserProfileAsync(username, currentUserId);

            return userProfile == null ? Results.NotFound() : Results.Ok(userProfile);
        })
        .WithName("GetUserProfile")
        .WithSummary("Get user profile by username")
        .Produces<UserProfileDto>(200)
        .Produces(401)
        .Produces(404);

        users.MapGet("/search/{query}", async (string query, ClaimsPrincipal? user, IUserService userService) =>
        {
            var currentUserId = user?.Identity?.IsAuthenticated == true
                ? int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (int?)null;

            var users = await userService.SearchUsersAsync(query, currentUserId);
            return Results.Ok(users);
        })
        .WithName("SearchUsers")
        .WithSummary("Search users by username or bio")
        .Produces<IEnumerable<UserDto>>(200);

        users.MapPost("/me/profile-image", [Authorize] async (IFormFile file, ClaimsPrincipal user, IUserService userService, IImageService imageService) =>
        {
            try
            {
                var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var fileName = await imageService.SaveImageAsync(file);
                var userDto = await userService.UpdateProfileImageAsync(userId, fileName);

                return userDto == null ? Results.NotFound() : Results.Ok(userDto);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UploadProfileImage")
        .WithSummary("Upload profile image for current user")
        .Produces<UserDto>(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .DisableAntiforgery();

        users.MapDelete("/me/profile-image", [Authorize] async (ClaimsPrincipal user, IUserService userService, IImageService imageService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userDto = await userService.RemoveProfileImageAsync(userId, imageService);

            return userDto == null ? Results.NotFound() : Results.Ok(userDto);
        })
        .WithName("RemoveProfileImage")
        .WithSummary("Remove profile image for current user")
        .Produces<UserDto>(200)
        .Produces(401)
        .Produces(404);

        users.MapPost("/{userId}/follow", [Authorize] async (int userId, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (currentUserId == userId)
                return Results.BadRequest("Cannot follow yourself");

            var result = await userService.FollowUserAsync(currentUserId, userId);
            return Results.Ok(result);
        })
        .WithName("FollowUser")
        .WithSummary("Follow a user")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        users.MapDelete("/{userId}/follow", [Authorize] async (int userId, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await userService.UnfollowUserAsync(currentUserId, userId);
            return Results.Ok(result);
        })
        .WithName("UnfollowUser")
        .WithSummary("Unfollow a user")
        .Produces<FollowResponseDto>(200)
        .Produces(401);

        users.MapGet("/me/following", [Authorize] async (ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var following = await userService.GetFollowingAsync(currentUserId);
            return Results.Ok(following);
        })
        .WithName("GetFollowing")
        .WithSummary("Get users that the current user is following")
        .Produces<IEnumerable<UserDto>>(200)
        .Produces(401);

        users.MapGet("/me/followers", [Authorize] async (ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var followers = await userService.GetFollowersAsync(currentUserId);
            return Results.Ok(followers);
        })
        .WithName("GetFollowers")
        .WithSummary("Get users that are following the current user")
        .Produces<IEnumerable<UserDto>>(200)
        .Produces(401);

        users.MapGet("/me/following/online-status", [Authorize] async (ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var following = await userService.GetFollowingWithOnlineStatusAsync(currentUserId);
            return Results.Ok(following);
        })
        .WithName("GetFollowingWithOnlineStatus")
        .WithSummary("Get users that the current user is following with their online status")
        .Produces<IEnumerable<UserWithOnlineStatusDto>>(200)
        .Produces(401);

        users.MapGet("/{userId}/following", async (int userId, IUserService userService) =>
        {
            var following = await userService.GetFollowingAsync(userId);
            return Results.Ok(following);
        })
        .WithName("GetUserFollowing")
        .WithSummary("Get users that a specific user is following")
        .Produces<IEnumerable<UserDto>>(200);

        users.MapGet("/{userId}/followers", async (int userId, IUserService userService) =>
        {
            var followers = await userService.GetFollowersAsync(userId);
            return Results.Ok(followers);
        })
        .WithName("GetUserFollowers")
        .WithSummary("Get users that are following a specific user")
        .Produces<IEnumerable<UserDto>>(200);

        users.MapPost("/me/fcm-token", [Authorize] async ([FromBody] UpdateFcmTokenDto tokenDto, ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await userService.UpdateFcmTokenAsync(userId, tokenDto.Token);

            return success ? Results.Ok() : Results.BadRequest("Failed to update FCM token");
        })
        .WithName("UpdateFcmToken")
        .WithSummary("Update user's FCM token for push notifications");

        users.MapDelete("/me/fcm-token", [Authorize] async (ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await userService.UpdateFcmTokenAsync(userId, null);

            return success ? Results.Ok(new { message = "FCM token cleared successfully" }) : Results.BadRequest("Failed to clear FCM token");
        })
        .WithName("ClearFcmToken")
        .WithSummary("Clear user's FCM token");

        users.MapGet("/me/follow-requests", [Authorize] async (ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var requests = await userService.GetPendingFollowRequestsAsync(currentUserId);
            return Results.Ok(requests);
        })
        .WithName("GetPendingFollowRequests")
        .WithSummary("Get pending follow requests for current user")
        .Produces<IEnumerable<FollowRequestDto>>(200)
        .Produces(401);

        users.MapPost("/follow-requests/{requestId}/approve", [Authorize] async (int requestId, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var result = await userService.ApproveFollowRequestAsync(requestId, currentUserId);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("ApproveFollowRequest")
        .WithSummary("Approve a follow request")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        users.MapPost("/follow-requests/{requestId}/deny", [Authorize] async (int requestId, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var result = await userService.DenyFollowRequestAsync(requestId, currentUserId);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("DenyFollowRequest")
        .WithSummary("Deny a follow request")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        users.MapPost("/follow-requests/approve-by-user/{requesterId}", [Authorize] async (int requesterId, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var result = await userService.ApproveFollowRequestByUserIdAsync(requesterId, currentUserId);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("ApproveFollowRequestByUserId")
        .WithSummary("Approve a follow request by requester user ID")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        users.MapPost("/follow-requests/deny-by-user/{requesterId}", [Authorize] async (int requesterId, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var result = await userService.DenyFollowRequestByUserIdAsync(requesterId, currentUserId);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("DenyFollowRequestByUserId")
        .WithSummary("Deny a follow request by requester user ID")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        users.MapGet("/debug-follow-status/{username}", [Authorize] async (string username, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var profile = await userService.GetUserProfileAsync(username, currentUserId);

            if (profile == null)
                return Results.NotFound();

            return Results.Ok(new {
                username = profile.Username,
                isFollowedByCurrentUser = profile.IsFollowedByCurrentUser,
                hasPendingFollowRequest = profile.HasPendingFollowRequest,
                requiresFollowApproval = profile.RequiresFollowApproval
            });
        })
        .WithName("DebugFollowStatus")
        .WithSummary("Debug follow status for a user")
        .Produces(200)
        .Produces(401)
        .Produces(404);
    }
}
