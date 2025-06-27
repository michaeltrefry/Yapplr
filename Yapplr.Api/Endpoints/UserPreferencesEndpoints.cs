using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class UserPreferencesEndpoints
{
    public static void MapUserPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var preferences = app.MapGroup("/api/preferences")
            .WithTags("User Preferences");

        preferences.MapGet("/", [Authorize] async (ClaimsPrincipal user, IUserPreferencesService preferencesService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userPreferences = await preferencesService.GetUserPreferencesAsync(userId);
            return Results.Ok(userPreferences);
        })
        .WithName("GetUserPreferences")
        .WithSummary("Get current user's preferences")
        .Produces<UserPreferencesDto>(200)
        .Produces(401);

        preferences.MapPut("/", [Authorize] async (ClaimsPrincipal user, UpdateUserPreferencesDto updateDto, IUserPreferencesService preferencesService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var updatedPreferences = await preferencesService.UpdateUserPreferencesAsync(userId, updateDto);
            return Results.Ok(updatedPreferences);
        })
        .WithName("UpdateUserPreferences")
        .WithSummary("Update current user's preferences")
        .Produces<UserPreferencesDto>(200)
        .Produces(401)
        .Produces(400);
    }
}
