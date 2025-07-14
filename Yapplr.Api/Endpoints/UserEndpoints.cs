using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;
using Yapplr.Api.Extensions;
using Yapplr.Api.Common;

namespace Yapplr.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/api/users").WithTags("Users");

        users.MapGet("/me", async (ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = user.GetUserId(true);
            var userDto = await userService.GetUserByIdAsync(userId);

            return userDto == null ? Results.NotFound() : Results.Ok(userDto);
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get current user profile")
        .RequireAuthorization("User")
        .Produces<UserDto>(200)
        .Produces(401)
        .Produces(404);

        users.MapPut("/me", async ([FromBody] UpdateUserDto updateDto, ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = user.GetUserId(true);
            var userDto = await userService.UpdateUserAsync(userId, updateDto);

            return userDto == null ? Results.NotFound() : Results.Ok(userDto);
        })
        .WithName("UpdateCurrentUser")
        .WithSummary("Update current user profile")
        .RequireAuthorization("ActiveUser")
        .Produces<UserDto>(200)
        .Produces(401)
        .Produces(404);

        users.MapGet("/{username}", async (string username, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userProfile = await userService.GetUserProfileAsync(username, currentUserId);

            return userProfile == null ? Results.NotFound() : Results.Ok(userProfile);
        })
        .WithName("GetUserProfile")
        .WithSummary("Get user profile by username")
        .RequireAuthorization("User")
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

        users.MapPost("/me/profile-image", async (IFormFile file, ClaimsPrincipal user, IUserService userService, IImageService imageService) =>
        {
            try
            {
                var userId = user.GetUserId(true);
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
        .RequireAuthorization("ActiveUser")
        .Produces<UserDto>(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .DisableAntiforgery();

        users.MapDelete("/me/profile-image", async (ClaimsPrincipal user, IUserService userService, IImageService imageService) =>
        {
            var userId = user.GetUserId(true);
            var userDto = await userService.RemoveProfileImageAsync(userId, imageService);

            return userDto == null ? Results.NotFound() : Results.Ok(userDto);
        })
        .WithName("RemoveProfileImage")
        .WithSummary("Remove profile image for current user")
        .RequireAuthorization("ActiveUser")
        .Produces<UserDto>(200)
        .Produces(401)
        .Produces(404);

        users.MapPost("/{userId}/follow", async (int userId, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (currentUserId == userId)
                return Results.BadRequest("Cannot follow yourself");

            var result = await userService.FollowUserAsync(currentUserId, userId);
            return Results.Ok(result);
        })
        .WithName("FollowUser")
        .WithSummary("Follow a user")
        .RequireAuthorization("ActiveUser")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        users.MapDelete("/{userId}/follow", async (int userId, ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await userService.UnfollowUserAsync(currentUserId, userId);
            return Results.Ok(result);
        })
        .WithName("UnfollowUser")
        .WithSummary("Unfollow a user")
        .RequireAuthorization("ActiveUser")
        .Produces<FollowResponseDto>(200)
        .Produces(401);

        users.MapGet("/me/following", async (ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var following = await userService.GetFollowingAsync(currentUserId);
            return Results.Ok(following);
        })
        .WithName("GetFollowing")
        .WithSummary("Get users that the current user is following")
        .RequireAuthorization("User")
        .Produces<IEnumerable<UserDto>>(200)
        .Produces(401);

        users.MapGet("/me/followers", async (ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var followers = await userService.GetFollowersAsync(currentUserId);
            return Results.Ok(followers);
        })
        .WithName("GetFollowers")
        .WithSummary("Get users that are following the current user")
        .RequireAuthorization("User")
        .Produces<IEnumerable<UserDto>>(200)
        .Produces(401);

        users.MapGet("/me/following/online-status", async (ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var following = await userService.GetFollowingWithOnlineStatusAsync(currentUserId);
            return Results.Ok(following);
        })
        .WithName("GetFollowingWithOnlineStatus")
        .WithSummary("Get users that the current user is following with their online status")
        .RequireAuthorization("User")
        .Produces<IEnumerable<UserWithOnlineStatusDto>>(200)
        .Produces(401);

        users.MapGet("/me/following/top", async (ClaimsPrincipal user, IUserService userService, int limit = 10) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var topFollowing = await userService.GetTopFollowingWithOnlineStatusAsync(currentUserId, limit);
            return Results.Ok(topFollowing);
        })
        .WithName("GetTopFollowingWithOnlineStatus")
        .WithSummary("Get top most interacted with users that the current user is following")
        .RequireAuthorization("User")
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

        users.MapPost("/me/fcm-token", async ([FromBody] UpdateFcmTokenDto tokenDto, ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = user.GetUserId(true);
            var success = await userService.UpdateFcmTokenAsync(userId, tokenDto.Token);

            return success ? Results.Ok() : Results.BadRequest("Failed to update FCM token");
        })
        .WithName("UpdateFcmToken")
        .WithSummary("Update user's FCM token for push notifications")
        .RequireAuthorization("ActiveUser");

        users.MapDelete("/me/fcm-token", async (ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = user.GetUserId(true);
            var success = await userService.UpdateFcmTokenAsync(userId, null);

            return success ? Results.Ok(new { message = "FCM token cleared successfully" }) : Results.BadRequest("Failed to clear FCM token");
        })
        .WithName("ClearFcmToken")
        .WithSummary("Clear user's FCM token")
        .RequireAuthorization("ActiveUser");

        users.MapPost("/me/expo-push-token", async ([FromBody] UpdateExpoPushTokenDto tokenDto, ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = user.GetUserId(true);
            var success = await userService.UpdateExpoPushTokenAsync(userId, tokenDto.Token);

            return success ? Results.Ok() : Results.BadRequest("Failed to update Expo push token");
        })
        .WithName("UpdateExpoPushToken")
        .WithSummary("Update user's Expo push token for push notifications")
        .RequireAuthorization("ActiveUser");

        users.MapDelete("/me/expo-push-token", async (ClaimsPrincipal user, IUserService userService) =>
        {
            var userId = user.GetUserId(true);
            var success = await userService.UpdateExpoPushTokenAsync(userId, null);

            return success ? Results.Ok(new { message = "Expo push token cleared successfully" }) : Results.BadRequest("Failed to clear Expo push token");
        })
        .WithName("ClearExpoPushToken")
        .WithSummary("Clear user's Expo push token")
        .RequireAuthorization("User");

        users.MapGet("/me/follow-requests", async (ClaimsPrincipal user, IUserService userService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var requests = await userService.GetPendingFollowRequestsAsync(currentUserId);
            return Results.Ok(requests);
        })
        .WithName("GetPendingFollowRequests")
        .WithSummary("Get pending follow requests for current user")
        .RequireAuthorization("User")
        .Produces<IEnumerable<FollowRequestDto>>(200)
        .Produces(401);

        users.MapPost("/follow-requests/{requestId}/approve", async (int requestId, ClaimsPrincipal user, IUserService userService) =>
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
        .RequireAuthorization("User")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        users.MapPost("/follow-requests/{requestId}/deny", async (int requestId, ClaimsPrincipal user, IUserService userService) =>
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
        .RequireAuthorization("User")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        users.MapPost("/follow-requests/approve-by-user/{requesterId}", async (int requesterId, ClaimsPrincipal user, IUserService userService) =>
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
        .RequireAuthorization("User")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        users.MapPost("/follow-requests/deny-by-user/{requesterId}", async (int requesterId, ClaimsPrincipal user, IUserService userService) =>
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
        .RequireAuthorization("User")
        .Produces<FollowResponseDto>(200)
        .Produces(400)
        .Produces(401);

        // Debug endpoints - only available in development
        if (app.Environment.IsDevelopment())
        {
            users.MapGet("/debug-follow-status/{username}", async (string username, ClaimsPrincipal user, IUserService userService) =>
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
            .RequireAuthorization("ActiveUser")
            .Produces(200)
            .Produces(401)
            .Produces(404);
        }

        // Cache management and demonstration endpoints - only available in development
        if (app.Environment.IsDevelopment())
        {
            // Cache management endpoints (for testing and monitoring)
            users.MapPost("/cache/clear", async (ICachingService cachingService) =>
            {
                await cachingService.ClearUserCacheAsync();
                return Results.Ok(new { message = "User cache cleared successfully" });
            })
            .WithName("ClearUserCache")
            .WithSummary("Clear all cached users")
            .Produces(200);

            users.MapDelete("/cache/{userId}", async (int userId, ICachingService cachingService) =>
            {
                await cachingService.InvalidateUserByIdAsync(userId);
                return Results.Ok(new { message = $"User {userId} removed from cache" });
            })
            .WithName("InvalidateUserCacheById")
            .WithSummary("Remove a specific user from cache by ID")
            .Produces(200);

            users.MapDelete("/cache/username/{username}", async (string username, ICachingService cachingService) =>
            {
                await cachingService.InvalidateUserByUsernameAsync(username);
                return Results.Ok(new { message = $"User {username} removed from cache" });
            })
            .WithName("InvalidateUserCacheByUsername")
            .WithSummary("Remove a specific user from cache by username")
            .Produces(200);

            // Demonstration endpoints showing cached vs non-cached lookups
            users.MapGet("/cached/{userId}", async (int userId, ICachingService cachingService, IServiceScopeFactory serviceScopeFactory) =>
            {
                var user = await cachingService.GetUserByIdAsync(userId, serviceScopeFactory);
                return user == null ? Results.NotFound() : Results.Ok(user.ToDto());
            })
            .WithName("GetUserByIdCached")
            .WithSummary("Get user by ID using cache service")
            .Produces<UserDto>(200)
            .Produces(404);

            users.MapGet("/cached/username/{username}", async (string username, ICachingService cachingService, IServiceScopeFactory serviceScopeFactory) =>
            {
                var user = await cachingService.GetUserByUsernameAsync(username, serviceScopeFactory);
                return user == null ? Results.NotFound() : Results.Ok(user.ToDto());
            })
            .WithName("GetUserByUsernameCached")
            .WithSummary("Get user by username using cache service")
            .Produces<UserDto>(200)
            .Produces(404);
        }
    }
}
