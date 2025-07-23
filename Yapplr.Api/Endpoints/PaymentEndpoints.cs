using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Services.Payment;
using Yapplr.Api.Common;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var payments = app.MapGroup("/api/payments").WithTags("Payments");

        // Public endpoints - get available payment providers
        payments.MapGet("/providers", async (IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var providers = await paymentService.GetAvailableProvidersAsync();
                return providers;
            });
        })
        .WithName("GetPaymentProviders")
        .WithSummary("Get available payment providers")
        .WithDescription("Returns a list of available payment providers and their capabilities");

        // Subscription endpoints - require authentication
        payments.MapPost("/subscriptions", async (
            CreateSubscriptionRequest request,
            [FromQuery] int subscriptionTierId,
            ClaimsPrincipal user,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId();
                var result = await paymentService.CreateSubscriptionAsync(userId, subscriptionTierId, request);
                
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
                }
                
                return Results.Ok(result.Data);
            });
        })
        .RequireAuthorization("User")
        .WithName("CreateSubscription")
        .WithSummary("Create a new subscription")
        .WithDescription("Creates a new subscription for the authenticated user");

        payments.MapGet("/subscriptions/current", async (
            ClaimsPrincipal user,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId();
                var result = await paymentService.GetUserSubscriptionAsync(userId);
                
                if (!result.Success)
                {
                    if (result.ErrorCode == "NO_SUBSCRIPTION")
                    {
                        return Results.NotFound(new { error = result.ErrorMessage });
                    }
                    return Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
                }
                
                return Results.Ok(result.Data);
            });
        })
        .RequireAuthorization("User")
        .WithName("GetCurrentSubscription")
        .WithSummary("Get current user subscription")
        .WithDescription("Returns the current subscription for the authenticated user");

        payments.MapPost("/subscriptions/cancel", async (
            CancelSubscriptionRequest request,
            ClaimsPrincipal user,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId();
                var result = await paymentService.CancelSubscriptionAsync(userId, request);
                
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
                }
                
                return Results.Ok(result.Data);
            });
        })
        .RequireAuthorization("User")
        .WithName("CancelSubscription")
        .WithSummary("Cancel current subscription")
        .WithDescription("Cancels the current subscription for the authenticated user");

        payments.MapPut("/subscriptions", async (
            UpdateSubscriptionRequest request,
            ClaimsPrincipal user,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId();
                var result = await paymentService.UpdateSubscriptionAsync(userId, request);
                
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
                }
                
                return Results.Ok(result.Data);
            });
        })
        .RequireAuthorization("User")
        .WithName("UpdateSubscription")
        .WithSummary("Update current subscription")
        .WithDescription("Updates the current subscription for the authenticated user");

        // Payment method endpoints
        payments.MapGet("/payment-methods", async (
            ClaimsPrincipal user,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId();
                var result = await paymentService.GetUserPaymentMethodsAsync(userId);
                
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
                }
                
                return Results.Ok(result.Data);
            });
        })
        .RequireAuthorization("User")
        .WithName("GetPaymentMethods")
        .WithSummary("Get user payment methods")
        .WithDescription("Returns all payment methods for the authenticated user");

        payments.MapPost("/payment-methods", async (
            CreatePaymentMethodRequest request,
            ClaimsPrincipal user,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId();
                var result = await paymentService.AddPaymentMethodAsync(userId, request);
                
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
                }
                
                return Results.Ok(result.Data);
            });
        })
        .RequireAuthorization("User")
        .WithName("AddPaymentMethod")
        .WithSummary("Add payment method")
        .WithDescription("Adds a new payment method for the authenticated user");

        payments.MapDelete("/payment-methods/{paymentMethodId:int}", async (
            int paymentMethodId,
            ClaimsPrincipal user,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId();
                var result = await paymentService.RemovePaymentMethodAsync(userId, paymentMethodId);
                
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
                }
                
                return Results.Ok(new { success = result.Data });
            });
        })
        .RequireAuthorization("User")
        .WithName("RemovePaymentMethod")
        .WithSummary("Remove payment method")
        .WithDescription("Removes a payment method for the authenticated user");

        payments.MapPut("/payment-methods/{paymentMethodId:int}/default", async (
            int paymentMethodId,
            ClaimsPrincipal user,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId();
                var result = await paymentService.SetDefaultPaymentMethodAsync(userId, paymentMethodId);
                
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
                }
                
                return Results.Ok(result.Data);
            });
        })
        .RequireAuthorization("User")
        .WithName("SetDefaultPaymentMethod")
        .WithSummary("Set default payment method")
        .WithDescription("Sets a payment method as the default for the authenticated user");

        // Payment history endpoint
        payments.MapGet("/transactions", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ClaimsPrincipal user,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                var userId = user.GetUserId();
                var result = await paymentService.GetPaymentHistoryAsync(userId, page, pageSize);
                
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.ErrorMessage, code = result.ErrorCode });
                }
                
                return Results.Ok(result.Data);
            });
        })
        .RequireAuthorization("User")
        .WithName("GetPaymentHistory")
        .WithSummary("Get payment history")
        .WithDescription("Returns payment transaction history for the authenticated user");

        // Webhook endpoints - no authentication required (verified by provider signature)
        payments.MapPost("/webhooks/{providerName}", async (
            string providerName,
            HttpRequest request,
            IPaymentGatewayService paymentService) =>
        {
            return await EndpointUtilities.HandleAsync(async () =>
            {
                // Read the raw body
                using var reader = new StreamReader(request.Body);
                var rawBody = await reader.ReadToEndAsync();
                
                // Get headers
                var signature = request.Headers["PayPal-Transmission-Sig"].FirstOrDefault() ?? 
                               request.Headers["Stripe-Signature"].FirstOrDefault() ?? "";
                
                var webhookRequest = new WebhookRequest
                {
                    ProviderName = providerName,
                    RawBody = rawBody,
                    Signature = signature,
                    Timestamp = DateTime.UtcNow
                };
                
                var result = await paymentService.HandleWebhookAsync(providerName, webhookRequest);
                
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.ErrorMessage });
                }
                
                return Results.Ok(new { processed = result.Data });
            });
        })
        .WithName("HandlePaymentWebhook")
        .WithSummary("Handle payment provider webhook")
        .WithDescription("Handles webhook events from payment providers")
        .AllowAnonymous();
    }
}
