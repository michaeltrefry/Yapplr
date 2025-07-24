using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Payment;

public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly YapplrDbContext _context;
    private readonly IPaymentGatewayManager _gatewayManager;
    private readonly ILogger<PaymentGatewayService> _logger;
    private readonly IDynamicPaymentConfigurationService _configService;

    public PaymentGatewayService(
        YapplrDbContext context,
        IPaymentGatewayManager gatewayManager,
        ILogger<PaymentGatewayService> logger,
        IDynamicPaymentConfigurationService configService)
    {
        _context = context;
        _gatewayManager = gatewayManager;
        _logger = logger;
        _configService = configService;
    }

    public async Task<PaymentServiceResult<SubscriptionDto>> CreateSubscriptionAsync(
        int userId, 
        int subscriptionTierId, 
        CreateSubscriptionRequest request)
    {
        try
        {
            // Validate user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("User not found", "USER_NOT_FOUND");
            }

            // Validate subscription tier exists
            var subscriptionTier = await _context.SubscriptionTiers.FindAsync(subscriptionTierId);
            if (subscriptionTier == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("Subscription tier not found", "TIER_NOT_FOUND");
            }

            // Check if user already has an active subscription
            var existingSubscription = await _context.Set<UserSubscription>()
                .FirstOrDefaultAsync(s => s.UserId == userId && 
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial));

            if (existingSubscription != null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("User already has an active subscription", "SUBSCRIPTION_EXISTS");
            }

            // Get payment provider
            var provider = await _gatewayManager.GetBestProviderAsync(request.PaymentProvider);
            if (provider == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("No payment provider available", "NO_PROVIDER");
            }

            // Create subscription with provider
            var providerResult = await provider.CreateSubscriptionAsync(request);
            if (!providerResult.Success)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult(
                    providerResult.ErrorMessage ?? "Payment provider error", 
                    providerResult.ErrorCode ?? "PROVIDER_ERROR");
            }

            // Get global settings for trial configuration
            var globalSettings = await _configService.GetGlobalSettingsAsync();

            // Create user subscription record
            var userSubscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionTierId = subscriptionTierId,
                PaymentProvider = provider.ProviderName,
                ExternalSubscriptionId = providerResult.ExternalSubscriptionId,
                Status = providerResult.RequiresAction ? SubscriptionStatus.Active : SubscriptionStatus.Active, // Will be updated by webhook
                StartDate = DateTime.UtcNow,
                NextBillingDate = providerResult.NextBillingDate ?? DateTime.UtcNow.AddMonths(subscriptionTier.BillingCycleMonths),
                IsTrialPeriod = request.StartTrial && globalSettings.EnableTrialPeriods,
                TrialEndDate = request.StartTrial && globalSettings.EnableTrialPeriods
                    ? DateTime.UtcNow.AddDays(globalSettings.DefaultTrialDays)
                    : null,
                PaymentMethodId = providerResult.PaymentMethodId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<UserSubscription>().Add(userSubscription);

            // Update user's subscription tier
            user.SubscriptionTierId = subscriptionTierId;

            // Create initial payment transaction record
            var transaction = new PaymentTransaction
            {
                UserId = userId,
                SubscriptionTierId = subscriptionTierId,
                PaymentProvider = provider.ProviderName,
                ExternalTransactionId = providerResult.ExternalSubscriptionId ?? Guid.NewGuid().ToString(),
                ExternalSubscriptionId = providerResult.ExternalSubscriptionId,
                Amount = subscriptionTier.Price,
                Currency = subscriptionTier.Currency,
                Status = providerResult.RequiresAction ? PaymentStatus.Pending : PaymentStatus.Processing,
                Type = PaymentType.Subscription,
                Description = $"Subscription to {subscriptionTier.Name}",
                BillingPeriodStart = DateTime.UtcNow,
                BillingPeriodEnd = DateTime.UtcNow.AddMonths(subscriptionTier.BillingCycleMonths),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            // Map to DTO
            var subscriptionDto = MapToSubscriptionDto(userSubscription, subscriptionTier, user);

            // If requires action, include authorization URL in metadata
            if (providerResult.RequiresAction && !string.IsNullOrEmpty(providerResult.AuthorizationUrl))
            {
                // You might want to store this URL temporarily or return it in the response
                _logger.LogInformation("Subscription created but requires user action: {AuthUrl}", providerResult.AuthorizationUrl);
            }

            return PaymentServiceResult<SubscriptionDto>.SuccessResult(subscriptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for user {UserId}", userId);
            return PaymentServiceResult<SubscriptionDto>.ErrorResult("Internal error creating subscription", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<SubscriptionDto>> GetUserSubscriptionAsync(int userId)
    {
        try
        {
            var subscription = await _context.Set<UserSubscription>()
                .Include(s => s.SubscriptionTier)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && 
                    (s.Status == SubscriptionStatus.Active || 
                     s.Status == SubscriptionStatus.Trial ||
                     s.Status == SubscriptionStatus.PendingCancellation));

            if (subscription == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("No active subscription found", "NO_SUBSCRIPTION");
            }

            var subscriptionDto = MapToSubscriptionDto(subscription, subscription.SubscriptionTier, subscription.User);
            return PaymentServiceResult<SubscriptionDto>.SuccessResult(subscriptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription for user {UserId}", userId);
            return PaymentServiceResult<SubscriptionDto>.ErrorResult("Internal error getting subscription", "INTERNAL_ERROR");
        }
    }

    public async Task<List<PaymentProviderInfo>> GetAvailableProvidersAsync()
    {
        return await _gatewayManager.GetProviderInfoAsync();
    }

    public async Task<IPaymentProvider?> GetBestProviderAsync(string? preferredProvider = null)
    {
        return await _gatewayManager.GetBestProviderAsync(preferredProvider);
    }

    private SubscriptionDto MapToSubscriptionDto(UserSubscription subscription, SubscriptionTier tier, User user)
    {
        return new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            Username = user.Username,
            SubscriptionTierId = subscription.SubscriptionTierId,
            SubscriptionTierName = tier.Name,
            Price = tier.Price,
            Currency = tier.Currency,
            BillingCycleMonths = tier.BillingCycleMonths,
            PaymentProvider = subscription.PaymentProvider,
            ExternalSubscriptionId = subscription.ExternalSubscriptionId,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            NextBillingDate = subscription.NextBillingDate,
            CancelledAt = subscription.CancelledAt,
            CancellationReason = subscription.CancellationReason,
            IsTrialPeriod = subscription.IsTrialPeriod,
            TrialEndDate = subscription.TrialEndDate,
            BillingCycleCount = subscription.BillingCycleCount,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
    }

    // Placeholder implementations for remaining methods
    public Task<PaymentServiceResult<SubscriptionDto>> CancelSubscriptionAsync(int userId, CancelSubscriptionRequest request)
    {
        throw new NotImplementedException("CancelSubscriptionAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<SubscriptionDto>> UpdateSubscriptionAsync(int userId, UpdateSubscriptionRequest request)
    {
        throw new NotImplementedException("UpdateSubscriptionAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<PaymentTransactionDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request)
    {
        throw new NotImplementedException("ProcessPaymentAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<PaymentTransactionDto>> RefundPaymentAsync(int userId, int transactionId, RefundPaymentRequest request)
    {
        throw new NotImplementedException("RefundPaymentAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<PaymentMethodDto>> AddPaymentMethodAsync(int userId, CreatePaymentMethodRequest request)
    {
        throw new NotImplementedException("AddPaymentMethodAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<bool>> RemovePaymentMethodAsync(int userId, int paymentMethodId)
    {
        throw new NotImplementedException("RemovePaymentMethodAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<List<PaymentMethodDto>>> GetUserPaymentMethodsAsync(int userId)
    {
        throw new NotImplementedException("GetUserPaymentMethodsAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<PaymentMethodDto>> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId)
    {
        throw new NotImplementedException("SetDefaultPaymentMethodAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<List<PaymentTransactionDto>>> GetPaymentHistoryAsync(int userId, int page = 1, int pageSize = 20)
    {
        throw new NotImplementedException("GetPaymentHistoryAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<bool>> HandleWebhookAsync(string providerName, WebhookRequest request)
    {
        throw new NotImplementedException("HandleWebhookAsync will be implemented in the next iteration");
    }

    public Task<PaymentServiceResult<SubscriptionDto>> SyncSubscriptionStatusAsync(int userId)
    {
        throw new NotImplementedException("SyncSubscriptionStatusAsync will be implemented in the next iteration");
    }
}
