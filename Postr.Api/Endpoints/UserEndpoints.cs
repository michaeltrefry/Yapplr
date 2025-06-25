using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Postr.Api.DTOs;
using Postr.Api.Services;

namespace Postr.Api.Endpoints;

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

        users.MapGet("/search/{query}", async (string query, IUserService userService) =>
        {
            var users = await userService.SearchUsersAsync(query);
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
    }
}
