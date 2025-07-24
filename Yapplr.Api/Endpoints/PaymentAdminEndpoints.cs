using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Services.Payment;
using Yapplr.Api.Services;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using System.Text.Json;

namespace Yapplr.Api.Endpoints;

public static class PaymentAdminEndpoints
{
    public static void MapPaymentAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin/payments").WithTags("Payment Admin");

        // Subscription Management
        admin.MapGet("/subscriptions", async (
            IPaymentAdminService adminService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? status = null,
            [FromQuery] string? provider = null) =>
        {
            var subscriptions = await adminService.GetSubscriptionsAsync(page, pageSize, status, provider);
            return Results.Ok(subscriptions);
        })
        .WithName("GetSubscriptions")
        .WithSummary("Get all user subscriptions")
        .RequireAuthorization("Admin")
        .Produces<PagedResult<AdminSubscriptionDto>>(200);

        admin.MapGet("/subscriptions/{id:int}", async (int id, IPaymentAdminService adminService) =>
        {
            var subscription = await adminService.GetSubscriptionAsync(id);
            return subscription == null ? Results.NotFound() : Results.Ok(subscription);
        })
        .WithName("GetSubscription")
        .WithSummary("Get subscription details")
        .RequireAuthorization("Admin")
        .Produces<AdminSubscriptionDto>(200)
        .Produces(404);

        admin.MapPost("/subscriptions/{id:int}/cancel", async (
            int id, 
            [FromBody] CancelSubscriptionRequest request,
            IPaymentAdminService adminService) =>
        {
            var result = await adminService.CancelSubscriptionAsync(id, request);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("AdminCancelSubscription")
        .WithSummary("Cancel a subscription")
        .RequireAuthorization("Admin")
        .Produces<PaymentServiceResult<bool>>(200)
        .Produces(400);

        admin.MapPost("/subscriptions/{id:int}/sync", async (int id, IPaymentAdminService adminService) =>
        {
            var result = await adminService.SyncSubscriptionAsync(id);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("SyncSubscription")
        .WithSummary("Sync subscription status with payment provider")
        .RequireAuthorization("Admin")
        .Produces<PaymentServiceResult<AdminSubscriptionDto>>(200)
        .Produces(400);

        // Payment Transactions
        admin.MapGet("/transactions", async (
            IPaymentAdminService adminService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? status = null,
            [FromQuery] string? provider = null,
            [FromQuery] int? userId = null) =>
        {
            var transactions = await adminService.GetTransactionsAsync(page, pageSize, status, provider, userId);
            return Results.Ok(transactions);
        })
        .WithName("GetTransactions")
        .WithSummary("Get payment transactions")
        .RequireAuthorization("Admin")
        .Produces<PagedResult<AdminTransactionDto>>(200);

        admin.MapGet("/transactions/{id:int}", async (int id, IPaymentAdminService adminService) =>
        {
            var transaction = await adminService.GetTransactionAsync(id);
            return transaction == null ? Results.NotFound() : Results.Ok(transaction);
        })
        .WithName("GetTransaction")
        .WithSummary("Get transaction details")
        .RequireAuthorization("Admin")
        .Produces<AdminTransactionDto>(200)
        .Produces(404);

        admin.MapPost("/transactions/{id:int}/refund", async (
            int id,
            [FromBody] RefundPaymentRequest request,
            IPaymentAdminService adminService) =>
        {
            var result = await adminService.RefundTransactionAsync(id, request);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("RefundTransaction")
        .WithSummary("Refund a payment transaction")
        .RequireAuthorization("Admin")
        .Produces<PaymentServiceResult<RefundPaymentResult>>(200)
        .Produces(400);

        // Payment Analytics
        admin.MapGet("/analytics/overview", async (
            IPaymentAdminService adminService,
            [FromQuery] int days = 30) =>
        {
            var analytics = await adminService.GetPaymentAnalyticsAsync(days);
            return Results.Ok(analytics);
        })
        .WithName("GetPaymentAnalytics")
        .WithSummary("Get payment analytics overview")
        .RequireAuthorization("Admin")
        .Produces<PaymentAnalyticsDto>(200);

        admin.MapGet("/analytics/revenue", async (
            IPaymentAdminService adminService,
            [FromQuery] int days = 30,
            [FromQuery] string? provider = null) =>
        {
            var revenue = await adminService.GetRevenueAnalyticsAsync(days, provider);
            return Results.Ok(revenue);
        })
        .WithName("GetRevenueAnalytics")
        .WithSummary("Get revenue analytics")
        .RequireAuthorization("Admin")
        .Produces<RevenueAnalyticsDto>(200);

        // Provider Management
        admin.MapGet("/providers", async (IPaymentAdminService adminService) =>
        {
            var providers = await adminService.GetPaymentProvidersAsync();
            return Results.Ok(providers);
        })
        .WithName("AdminGetPaymentProviders")
        .WithSummary("Get payment provider status")
        .RequireAuthorization("Admin")
        .Produces<IEnumerable<PaymentProviderStatusDto>>(200);

        admin.MapPost("/providers/{providerName}/test", async (
            string providerName,
            IPaymentAdminService adminService) =>
        {
            var result = await adminService.TestPaymentProviderAsync(providerName);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("TestPaymentProvider")
        .WithSummary("Test payment provider connectivity")
        .RequireAuthorization("Admin")
        .Produces<PaymentServiceResult<bool>>(200)
        .Produces(400);

        // Failed Payments
        admin.MapGet("/failed-payments", async (
            IPaymentAdminService adminService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25) =>
        {
            var failedPayments = await adminService.GetFailedPaymentsAsync(page, pageSize);
            return Results.Ok(failedPayments);
        })
        .WithName("GetFailedPayments")
        .WithSummary("Get failed payment attempts")
        .RequireAuthorization("Admin")
        .Produces<PagedResult<FailedPaymentDto>>(200);

        admin.MapPost("/failed-payments/{id:int}/retry", async (
            int id,
            IPaymentAdminService adminService) =>
        {
            var result = await adminService.RetryFailedPaymentAsync(id);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("RetryFailedPayment")
        .WithSummary("Retry a failed payment")
        .RequireAuthorization("Admin")
        .Produces<PaymentServiceResult<bool>>(200)
        .Produces(400);

        // Subscription Tiers Management
        admin.MapGet("/subscription-tiers", async (IPaymentAdminService adminService) =>
        {
            var tiers = await adminService.GetSubscriptionTiersAsync();
            return Results.Ok(tiers);
        })
        .WithName("AdminGetSubscriptionTiers")
        .WithSummary("Get subscription tiers")
        .RequireAuthorization("Admin")
        .Produces<IEnumerable<AdminSubscriptionTierDto>>(200);

        admin.MapPut("/subscription-tiers/{id:int}", async (
            int id,
            [FromBody] UpdateSubscriptionTierRequest request,
            IPaymentAdminService adminService) =>
        {
            var result = await adminService.UpdateSubscriptionTierAsync(id, request);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("AdminUpdateSubscriptionTier")
        .WithSummary("Update subscription tier")
        .RequireAuthorization("Admin")
        .Produces<PaymentServiceResult<AdminSubscriptionTierDto>>(200)
        .Produces(400);

        // Webhook Management
        admin.MapGet("/webhooks", async (
            IPaymentAdminService adminService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? provider = null) =>
        {
            var webhooks = await adminService.GetWebhookLogsAsync(page, pageSize, provider);
            return Results.Ok(webhooks);
        })
        .WithName("GetWebhookLogs")
        .WithSummary("Get webhook processing logs")
        .RequireAuthorization("Admin")
        .Produces<PagedResult<WebhookLogDto>>(200);

        admin.MapPost("/webhooks/{id:int}/replay", async (
            int id,
            IPaymentAdminService adminService) =>
        {
            var result = await adminService.ReplayWebhookAsync(id);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("ReplayWebhook")
        .WithSummary("Replay a webhook event")
        .RequireAuthorization("Admin")
        .Produces<PaymentServiceResult<bool>>(200)
        .Produces(400);

        // Payment Configuration Management
        var config = app.MapGroup("/api/admin/payment-configuration").WithTags("Payment Configuration");

        config.MapGet("/global", async (YapplrDbContext context) =>
        {
            try
            {
                var globalConfig = await context.PaymentGlobalConfigurations.FirstOrDefaultAsync();

                if (globalConfig == null)
                {
                    // Return default configuration
                    return Results.Ok(new PaymentGlobalConfigurationDto
                    {
                        DefaultProvider = "PayPal",
                        DefaultCurrency = "USD",
                        GracePeriodDays = 7,
                        MaxPaymentRetries = 3,
                        RetryIntervalDays = 3,
                        EnableTrialPeriods = true,
                        DefaultTrialDays = 14,
                        EnableProration = true,
                        WebhookTimeoutSeconds = 10,
                        VerifyWebhookSignatures = true
                    });
                }

                return Results.Ok(PaymentConfigurationMapper.MapToGlobalDto(globalConfig));
            }
            catch (Exception ex)
            {
                return Results.Problem("Internal server error");
            }
        })
        .WithName("GetGlobalPaymentConfiguration")
        .WithSummary("Get global payment configuration")
        .RequireAuthorization("Admin")
        .Produces<PaymentGlobalConfigurationDto>(200)
        .Produces(500);
    }

}

/// <summary>
/// Helper class for mapping payment configuration entities to DTOs
/// </summary>
public static class PaymentConfigurationMapper
{
    public static PaymentGlobalConfigurationDto MapToGlobalDto(PaymentGlobalConfiguration config)
    {
        return new PaymentGlobalConfigurationDto
        {
            Id = config.Id,
            DefaultProvider = config.DefaultProvider,
            DefaultCurrency = config.DefaultCurrency,
            GracePeriodDays = config.GracePeriodDays,
            MaxPaymentRetries = config.MaxPaymentRetries,
            RetryIntervalDays = config.RetryIntervalDays,
            EnableTrialPeriods = config.EnableTrialPeriods,
            DefaultTrialDays = config.DefaultTrialDays,
            EnableProration = config.EnableProration,
            WebhookTimeoutSeconds = config.WebhookTimeoutSeconds,
            VerifyWebhookSignatures = config.VerifyWebhookSignatures,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }

    public static PaymentProviderConfigurationDto MapToProviderDto(PaymentProviderConfiguration config)
    {
        return new PaymentProviderConfigurationDto
        {
            Id = config.Id,
            ProviderName = config.ProviderName,
            IsEnabled = config.IsEnabled,
            Environment = config.Environment,
            Priority = config.Priority,
            TimeoutSeconds = config.TimeoutSeconds,
            MaxRetries = config.MaxRetries,
            SupportedCurrencies = string.IsNullOrEmpty(config.SupportedCurrencies)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(config.SupportedCurrencies) ?? new List<string>(),
            Settings = config.Settings.Select(s => new PaymentProviderSettingDto
            {
                Id = s.Id,
                Key = s.Key,
                Value = s.IsSensitive ? "***" : s.Value,
                IsSensitive = s.IsSensitive,
                Description = s.Description,
                Category = s.Category,
                IsRequired = s.IsRequired
            }).ToList(),
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }
}
