using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;
using Yapplr.Api.Common;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var subscriptions = app.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        // Public endpoints - get available subscription tiers
        subscriptions.MapGet("/tiers", async (ISubscriptionService subscriptionService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var tiers = await subscriptionService.GetActiveSubscriptionTiersAsync();
                return tiers;
            });
        })
        .WithName("GetActiveSubscriptionTiers")
        .WithSummary("Get all active subscription tiers")
        .Produces<IEnumerable<SubscriptionTierDto>>(200);

        subscriptions.MapGet("/tiers/{id:int}", async (int id, ISubscriptionService subscriptionService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var tier = await subscriptionService.GetSubscriptionTierAsync(id);
                return tier == null ? Results.NotFound() : Results.Ok(tier);
            });
        })
        .WithName("GetSubscriptionTier")
        .WithSummary("Get subscription tier by ID")
        .Produces<SubscriptionTierDto>(200)
        .Produces(404);

        // User subscription management - requires authentication
        subscriptions.MapGet("/my-subscription", async (ISubscriptionService subscriptionService, ClaimsPrincipal user) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId(true);
                var subscription = await subscriptionService.GetUserSubscriptionAsync(userId);
                return subscription;
            });
        })
        .WithName("GetMySubscription")
        .WithSummary("Get current user's subscription")
        .RequireAuthorization("User")
        .Produces<UserSubscriptionDto>(200);

        subscriptions.MapPost("/assign-tier", async ([FromBody] AssignSubscriptionTierDto assignDto, ISubscriptionService subscriptionService, ClaimsPrincipal user) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId(true);
                var success = await subscriptionService.AssignSubscriptionTierAsync(userId, assignDto.SubscriptionTierId);

                if (!success)
                {
                    return Results.BadRequest("Failed to assign subscription tier. Tier may not exist or be inactive.");
                }

                return Results.Ok(new { message = "Subscription tier assigned successfully" });
            });
        })
        .WithName("AssignSubscriptionTier")
        .WithSummary("Assign subscription tier to current user")
        .RequireAuthorization("User")
        .Produces(200)
        .Produces(400);

        subscriptions.MapDelete("/remove-subscription", async (ISubscriptionService subscriptionService, ClaimsPrincipal user) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId(true);
                var success = await subscriptionService.RemoveUserSubscriptionAsync(userId);

                if (!success)
                {
                    return Results.BadRequest("Failed to remove subscription");
                }

                return Results.Ok(new { message = "Subscription removed successfully" });
            });
        })
        .WithName("RemoveUserSubscription")
        .WithSummary("Remove current user's subscription")
        .RequireAuthorization("User")
        .Produces(200)
        .Produces(400);

        // Admin endpoints for subscription tier management
        var adminSubscriptions = app.MapGroup("/api/admin/subscriptions").WithTags("Admin Subscriptions");

        adminSubscriptions.MapGet("/tiers", async (ISubscriptionService subscriptionService, bool includeInactive = false) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var tiers = await subscriptionService.GetAllSubscriptionTiersAsync(includeInactive);
                return tiers;
            });
        })
        .WithName("GetAllSubscriptionTiersAdmin")
        .WithSummary("Get all subscription tiers (admin)")
        .RequireAuthorization("Admin")
        .Produces<IEnumerable<SubscriptionTierDto>>(200);

        adminSubscriptions.MapPost("/tiers", async ([FromBody] CreateSubscriptionTierDto createDto, ISubscriptionService subscriptionService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var tier = await subscriptionService.CreateSubscriptionTierAsync(createDto);
                return Results.Created($"/api/admin/subscriptions/tiers/{tier.Id}", tier);
            });
        })
        .WithName("CreateSubscriptionTier")
        .WithSummary("Create new subscription tier")
        .RequireAuthorization("Admin")
        .Produces<SubscriptionTierDto>(201)
        .Produces(400);

        adminSubscriptions.MapPut("/tiers/{id:int}", async (int id, [FromBody] UpdateSubscriptionTierDto updateDto, ISubscriptionService subscriptionService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var tier = await subscriptionService.UpdateSubscriptionTierAsync(id, updateDto);
                return tier == null ? Results.NotFound() : Results.Ok(tier);
            });
        })
        .WithName("UpdateSubscriptionTier")
        .WithSummary("Update subscription tier")
        .RequireAuthorization("Admin")
        .Produces<SubscriptionTierDto>(200)
        .Produces(404);

        adminSubscriptions.MapDelete("/tiers/{id:int}", async (int id, ISubscriptionService subscriptionService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var success = await subscriptionService.DeleteSubscriptionTierAsync(id);
                
                if (!success)
                {
                    return Results.BadRequest("Cannot delete subscription tier. It may have users assigned to it or not exist.");
                }
                
                return Results.Ok(new { message = "Subscription tier deleted successfully" });
            });
        })
        .WithName("DeleteSubscriptionTier")
        .WithSummary("Delete subscription tier")
        .RequireAuthorization("Admin")
        .Produces(200)
        .Produces(400);

        adminSubscriptions.MapGet("/users/{userId:int}/subscription", async (int userId, ISubscriptionService subscriptionService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var subscription = await subscriptionService.GetUserSubscriptionAsync(userId);
                return subscription;
            });
        })
        .WithName("GetUserSubscriptionAdmin")
        .WithSummary("Get user's subscription (admin)")
        .RequireAuthorization("Admin")
        .Produces<UserSubscriptionDto>(200);

        adminSubscriptions.MapPost("/users/{userId:int}/assign-tier", async (int userId, [FromBody] AssignSubscriptionTierDto assignDto, ISubscriptionService subscriptionService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var success = await subscriptionService.AssignSubscriptionTierAsync(userId, assignDto.SubscriptionTierId);
                
                if (!success)
                {
                    return Results.BadRequest("Failed to assign subscription tier. User or tier may not exist, or tier may be inactive.");
                }
                
                return Results.Ok(new { message = "Subscription tier assigned successfully" });
            });
        })
        .WithName("AssignSubscriptionTierAdmin")
        .WithSummary("Assign subscription tier to user (admin)")
        .RequireAuthorization("Admin")
        .Produces(200)
        .Produces(400);

        adminSubscriptions.MapGet("/tiers/{tierId:int}/users/count", async (int tierId, ISubscriptionService subscriptionService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var count = await subscriptionService.GetUserCountByTierAsync(tierId);
                return new { tierId, userCount = count };
            });
        })
        .WithName("GetSubscriptionTierUserCount")
        .WithSummary("Get number of users assigned to subscription tier")
        .RequireAuthorization("Admin")
        .Produces(200);
    }
}
