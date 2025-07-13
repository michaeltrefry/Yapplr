using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class UserPreferencesEndpoints
{
    public static void MapUserPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var preferences = app.MapGroup("/api/preferences")
            .WithTags("User Preferences");

        preferences.MapGet("/", async (ClaimsPrincipal user, IUserPreferencesService preferencesService) =>
        {
            var userId = user.GetUserId(true);
            var userPreferences = await preferencesService.GetUserPreferencesAsync(userId);
            return Results.Ok(userPreferences);
        })
        .WithName("GetUserPreferences")
        .WithSummary("Get current user's preferences")
        .RequireAuthorization("ActiveUser")
        .Produces<UserPreferencesDto>(200)
        .Produces(401);

        preferences.MapPut("/", async (ClaimsPrincipal user, UpdateUserPreferencesDto updateDto, IUserPreferencesService preferencesService) =>
        {
            var userId = user.GetUserId(true);
            var updatedPreferences = await preferencesService.UpdateUserPreferencesAsync(userId, updateDto);
            return Results.Ok(updatedPreferences);
        })
        .WithName("UpdateUserPreferences")
        .WithSummary("Update current user's preferences")
        .RequireAuthorization("ActiveUser")
        .Produces<UserPreferencesDto>(200)
        .Produces(401)
        .Produces(400);
    }
}
