using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.Authorization;
using Yapplr.Api.Extensions;
using Yapplr.Api.CQRS;
using Yapplr.Api.CQRS.Commands;

namespace Yapplr.Api.Endpoints;

public static class CqrsTestEndpoints
{
    public static void MapCqrsTestEndpoints(this WebApplication app)
    {
        var cqrs = app.MapGroup("/api/cqrs-test").WithTags("CQRS Testing");

        // Test email command
        cqrs.MapPost("/test-email", async ([FromBody] TestEmailRequest request, ClaimsPrincipal user, ICommandPublisher commandPublisher) =>
        {
            var userId = user.GetUserId(true);

            var emailCommand = new SendEmailCommand
            {
                UserId = userId,
                ToEmail = request.ToEmail,
                Subject = request.Subject ?? "CQRS Test Email",
                HtmlBody = $"<h1>CQRS Test</h1><p>{request.Message ?? "This is a test email sent via CQRS command."}</p>",
                TextBody = $"CQRS Test\n\n{request.Message ?? "This is a test email sent via CQRS command."}"
            };

            await commandPublisher.PublishAsync(emailCommand);

            return Results.Ok(new { 
                message = "Email command published successfully",
                commandId = emailCommand.CommandId,
                timestamp = emailCommand.CreatedAt
            });
        })
        .WithName("TestEmailCommand")
        .WithSummary("Test email sending via CQRS command")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(401);

        // Test notification command
        cqrs.MapPost("/test-notification", async ([FromBody] TestNotificationRequest request, ClaimsPrincipal user, ICommandPublisher commandPublisher) =>
        {
            var userId = user.GetUserId(true);

            var notificationCommand = new SendNotificationCommand
            {
                UserId = userId,
                TargetUserId = request.TargetUserId ?? userId,
                Title = request.Title ?? "CQRS Test Notification",
                Body = request.Message ?? "This is a test notification sent via CQRS command.",
                NotificationType = "test",
                Data = new Dictionary<string, string>
                {
                    ["source"] = "cqrs-test",
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                }
            };

            await commandPublisher.PublishAsync(notificationCommand);

            return Results.Ok(new { 
                message = "Notification command published successfully",
                commandId = notificationCommand.CommandId,
                timestamp = notificationCommand.CreatedAt
            });
        })
        .WithName("TestNotificationCommand")
        .WithSummary("Test notification sending via CQRS command")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(401);

        // Test analytics command
        cqrs.MapPost("/test-analytics", async ([FromBody] TestAnalyticsRequest request, ClaimsPrincipal user, ICommandPublisher commandPublisher) =>
        {
            var userId = user.GetUserId(true);

            var analyticsCommand = new TrackUserActivityCommand
            {
                UserId = userId,
                TargetUserId = userId,
                ActivityType = request.ActivityType ?? "cqrs_test",
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "cqrs-test-endpoint",
                    ["testData"] = request.TestData ?? "sample test data"
                }
            };

            await commandPublisher.PublishAsync(analyticsCommand);

            return Results.Ok(new { 
                message = "Analytics command published successfully",
                commandId = analyticsCommand.CommandId,
                timestamp = analyticsCommand.CreatedAt
            });
        })
        .WithName("TestAnalyticsCommand")
        .WithSummary("Test analytics tracking via CQRS command")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(401);

        // Test delayed command
        cqrs.MapPost("/test-delayed", async ([FromBody] TestDelayedRequest request, ClaimsPrincipal user, ICommandPublisher commandPublisher) =>
        {
            var userId = user.GetUserId(true);
            var delay = TimeSpan.FromSeconds(request.DelaySeconds ?? 30);

            var emailCommand = new SendEmailCommand
            {
                UserId = userId,
                ToEmail = request.ToEmail,
                Subject = "Delayed CQRS Test Email",
                HtmlBody = $"<h1>Delayed CQRS Test</h1><p>This email was delayed by {delay.TotalSeconds} seconds.</p>",
                TextBody = $"Delayed CQRS Test\n\nThis email was delayed by {delay.TotalSeconds} seconds."
            };

            await commandPublisher.PublishDelayedAsync(emailCommand, delay);

            return Results.Ok(new { 
                message = $"Delayed email command published successfully (delay: {delay.TotalSeconds}s)",
                commandId = emailCommand.CommandId,
                timestamp = emailCommand.CreatedAt,
                estimatedDelivery = DateTime.UtcNow.Add(delay)
            });
        })
        .WithName("TestDelayedCommand")
        .WithSummary("Test delayed command publishing")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(401);
    }
}

// Request DTOs for testing
public record TestEmailRequest(string ToEmail, string? Subject = null, string? Message = null);
public record TestNotificationRequest(int? TargetUserId = null, string? Title = null, string? Message = null);
public record TestAnalyticsRequest(string? ActivityType = null, string? TestData = null);
public record TestDelayedRequest(string ToEmail, int? DelaySeconds = null);
