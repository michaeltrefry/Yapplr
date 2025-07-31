using Microsoft.Extensions.Options;
using System.Security.Claims;
using Yapplr.Api.Configuration;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Unified;

namespace Yapplr.Api.Endpoints;

public static class NotificationConfigurationEndpoints
{
    public static void MapNotificationConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notification-config")
            .WithTags("Notification Configuration")
            .RequireAuthorization();

        group.MapGet("/", GetNotificationConfiguration)
            .WithName("GetNotificationConfiguration")
            .WithSummary("Get current notification provider configuration")
            .WithDescription("Returns the current configuration for notification providers (Firebase, SignalR)");

        group.MapGet("/status", GetNotificationProviderStatus)
            .WithName("GetNotificationProviderStatus")
            .WithSummary("Get notification provider status")
            .WithDescription("Returns the availability status of each notification provider");

        group.MapPost("/test/{userId:int}", SendTestNotificationToUser)
            .WithName("SendTestNotificationToUser")
            .WithSummary("Send test notification to specific user")
            .WithDescription("Sends a test notification to the specified user using the configured providers");

        group.MapPost("/test/current-user", SendTestNotificationToCurrentUser)
            .WithName("SendTestNotificationToCurrentUser")
            .WithSummary("Send test notification to current user")
            .WithDescription("Sends a test notification to the currently authenticated user");
    }

    private static IResult GetNotificationConfiguration(
        IOptions<NotificationProvidersConfiguration> notificationOptions)
    {
        var config = notificationOptions.Value;

        var response = new
        {
            Firebase = new
            {
                Enabled = config.Firebase.Enabled,
                ProjectId = config.Firebase.ProjectId
            },
            SignalR = new
            {
                Enabled = config.SignalR.Enabled,
                MaxConnectionsPerUser = config.SignalR.MaxConnectionsPerUser,
                MaxTotalConnections = config.SignalR.MaxTotalConnections
            }
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetNotificationProviderStatus(
        IEnumerable<IRealtimeNotificationProvider> providers)
    {
        var providerStatuses = new List<object>();

        foreach (var provider in providers)
        {
            var isAvailable = await provider.IsAvailableAsync();
            providerStatuses.Add(new
            {
                Name = provider.ProviderName,
                IsAvailable = isAvailable
            });
        }

        var response = new
        {
            Providers = providerStatuses,
            TotalProviders = providerStatuses.Count,
            AvailableProviders = providerStatuses.Count(p => (bool)p.GetType().GetProperty("IsAvailable")!.GetValue(p)!)
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> SendTestNotificationToUser(
        int userId,
        INotificationService notificationService)
    {
        try
        {
            var result = await notificationService.SendTestNotificationAsync(userId);

            if (result)
            {
                return Results.Ok(new { success = true, message = $"Test notification sent to user {userId}" });
            }
            else
            {
                return Results.BadRequest(new { success = false, message = "Failed to send test notification" });
            }
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error sending test notification",
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> SendTestNotificationToCurrentUser(
        ClaimsPrincipal user,
        INotificationService notificationService)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Results.BadRequest(new { success = false, message = "Invalid user ID" });
        }

        try
        {
            var result = await notificationService.SendTestNotificationAsync(userId);

            if (result)
            {
                return Results.Ok(new { success = true, message = "Test notification sent successfully" });
            }
            else
            {
                return Results.BadRequest(new { success = false, message = "Failed to send test notification" });
            }
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Error sending test notification",
                statusCode: 500
            );
        }
    }
}
