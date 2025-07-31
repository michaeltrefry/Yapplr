using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Models;
using Yapplr.Api.Services.Payment.Providers;

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

    private PaymentStatus ParsePaymentStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "pending" => PaymentStatus.Pending,
            "processing" => PaymentStatus.Processing,
            "completed" or "succeeded" or "success" => PaymentStatus.Completed,
            "failed" or "error" => PaymentStatus.Failed,
            _ => PaymentStatus.Pending
        };
    }

    private SubscriptionStatus ParseSubscriptionStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "active" => SubscriptionStatus.Active,
            "trial" => SubscriptionStatus.Trial,
            "cancelled" or "canceled" => SubscriptionStatus.Cancelled,
            "pending_cancellation" => SubscriptionStatus.PendingCancellation,
            "expired" => SubscriptionStatus.Expired,
            "suspended" => SubscriptionStatus.Suspended,
            _ => SubscriptionStatus.Active
        };
    }

    // Placeholder implementations for remaining methods
    public async Task<PaymentServiceResult<SubscriptionDto>> CancelSubscriptionAsync(int userId, CancelSubscriptionRequest request)
    {
        try
        {
            // Get user's active subscription
            var subscription = await _context.Set<UserSubscription>()
                .Include(s => s.SubscriptionTier)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && 
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial));

            if (subscription == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("No active subscription found", "NO_SUBSCRIPTION");
            }

            // Get payment provider
            var provider = await _gatewayManager.GetProviderByNameAsync(subscription.PaymentProvider);
            if (provider == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("Payment provider not available", "PROVIDER_UNAVAILABLE");
            }

            // Cancel with provider
            var cancelRequest = new CancelSubscriptionRequest
            {
                Reason = request.Reason,
                CancelImmediately = request.CancelImmediately,
                Metadata = request.Metadata
            };
            var providerResult = await provider.CancelSubscriptionAsync(cancelRequest);
            if (!providerResult.Success)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult(
                    providerResult.ErrorMessage ?? "Payment provider error", 
                    providerResult.ErrorCode ?? "PROVIDER_ERROR");
            }

            // Update subscription status
            if (request.CancelImmediately)
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.EndDate = DateTime.UtcNow;
            }
            else
            {
                subscription.Status = SubscriptionStatus.PendingCancellation;
                subscription.EndDate = subscription.NextBillingDate;
            }
            
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.CancellationReason = request.Reason;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var subscriptionDto = MapToSubscriptionDto(subscription, subscription.SubscriptionTier, subscription.User);
            return PaymentServiceResult<SubscriptionDto>.SuccessResult(subscriptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", userId);
            return PaymentServiceResult<SubscriptionDto>.ErrorResult("Internal error cancelling subscription", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<SubscriptionDto>> UpdateSubscriptionAsync(int userId, UpdateSubscriptionRequest request)
    {
        try
        {
            // Get user's active subscription
            var subscription = await _context.Set<UserSubscription>()
                .Include(s => s.SubscriptionTier)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && 
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial));

            if (subscription == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("No active subscription found", "NO_SUBSCRIPTION");
            }

            // Validate new subscription tier if provided
            SubscriptionTier? newTier = null;
            if (request.NewSubscriptionTierId.HasValue)
            {
                newTier = await _context.SubscriptionTiers.FindAsync(request.NewSubscriptionTierId.Value);
                if (newTier == null)
                {
                    return PaymentServiceResult<SubscriptionDto>.ErrorResult("New subscription tier not found", "TIER_NOT_FOUND");
                }
            }

            // Get payment provider
            var provider = await _gatewayManager.GetProviderByNameAsync(subscription.PaymentProvider);
            if (provider == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("Payment provider not available", "PROVIDER_UNAVAILABLE");
            }

            // Update with provider
            var updateRequest = new UpdateSubscriptionRequest
            {
                NewSubscriptionTierId = request.NewSubscriptionTierId,
                NewPaymentMethodId = request.NewPaymentMethodId,
                ProrateBilling = request.ProrateBilling,
                Metadata = request.Metadata
            };
            var providerResult = await provider.UpdateSubscriptionAsync(updateRequest);
            if (!providerResult.Success)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult(
                    providerResult.ErrorMessage ?? "Payment provider error", 
                    providerResult.ErrorCode ?? "PROVIDER_ERROR");
            }

            // Update local subscription
            if (newTier != null)
            {
                subscription.SubscriptionTierId = newTier.Id;
                subscription.User.SubscriptionTierId = newTier.Id;
            }
            
            if (!string.IsNullOrEmpty(request.NewPaymentMethodId))
            {
                subscription.PaymentMethodId = request.NewPaymentMethodId;
            }
            
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var subscriptionDto = MapToSubscriptionDto(subscription, newTier ?? subscription.SubscriptionTier, subscription.User);
            return PaymentServiceResult<SubscriptionDto>.SuccessResult(subscriptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription for user {UserId}", userId);
            return PaymentServiceResult<SubscriptionDto>.ErrorResult("Internal error updating subscription", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<PaymentTransactionDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request)
    {
        try
        {
            // Validate user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return PaymentServiceResult<PaymentTransactionDto>.ErrorResult("User not found", "USER_NOT_FOUND");
            }

            // Get payment provider
            var provider = await _gatewayManager.GetBestProviderAsync(request.PaymentProvider);
            if (provider == null)
            {
                return PaymentServiceResult<PaymentTransactionDto>.ErrorResult("No payment provider available", "NO_PROVIDER");
            }

            // Process payment with provider
            var providerResult = await provider.ProcessPaymentAsync(request);
            if (!providerResult.Success)
            {
                return PaymentServiceResult<PaymentTransactionDto>.ErrorResult(
                    providerResult.ErrorMessage ?? "Payment provider error", 
                    providerResult.ErrorCode ?? "PROVIDER_ERROR");
            }

            // Create payment transaction record
            var transaction = new PaymentTransaction
            {
                UserId = userId,
                PaymentProvider = provider.ProviderName,
                ExternalTransactionId = providerResult.ExternalTransactionId ?? Guid.NewGuid().ToString(),
                Amount = request.Amount,
                Currency = request.Currency,
                Status = ParsePaymentStatus(providerResult.Status),
                Type = PaymentType.OneTime,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var status = ParsePaymentStatus(providerResult.Status);
            if (status == PaymentStatus.Completed)
            {
                transaction.ProcessedAt = DateTime.UtcNow;
            }
            else if (status == PaymentStatus.Failed)
            {
                transaction.FailedAt = DateTime.UtcNow;
                transaction.FailureReason = providerResult.ErrorMessage;
            }

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Map to DTO
            var transactionDto = new PaymentTransactionDto
            {
                Id = transaction.Id,
                UserId = transaction.UserId,
                Username = user.Username,
                SubscriptionTierId = transaction.SubscriptionTierId,
                PaymentProvider = transaction.PaymentProvider,
                ExternalTransactionId = transaction.ExternalTransactionId,
                ExternalSubscriptionId = transaction.ExternalSubscriptionId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Status = transaction.Status,
                Type = transaction.Type,
                Description = transaction.Description,
                BillingPeriodStart = transaction.BillingPeriodStart,
                BillingPeriodEnd = transaction.BillingPeriodEnd,
                ProcessedAt = transaction.ProcessedAt,
                FailedAt = transaction.FailedAt,
                FailureReason = transaction.FailureReason,
                CreatedAt = transaction.CreatedAt
            };

            return PaymentServiceResult<PaymentTransactionDto>.SuccessResult(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for user {UserId}", userId);
            return PaymentServiceResult<PaymentTransactionDto>.ErrorResult("Internal error processing payment", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<PaymentTransactionDto>> RefundPaymentAsync(int userId, int transactionId, RefundPaymentRequest request)
    {
        try
        {
            // Get the original transaction
            var transaction = await _context.PaymentTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

            if (transaction == null)
            {
                return PaymentServiceResult<PaymentTransactionDto>.ErrorResult("Transaction not found", "TRANSACTION_NOT_FOUND");
            }

            if (transaction.Status != PaymentStatus.Completed)
            {
                return PaymentServiceResult<PaymentTransactionDto>.ErrorResult("Transaction cannot be refunded", "INVALID_TRANSACTION_STATUS");
            }

            // Get payment provider
            var provider = await _gatewayManager.GetProviderByNameAsync(transaction.PaymentProvider);
            if (provider == null)
            {
                return PaymentServiceResult<PaymentTransactionDto>.ErrorResult("Payment provider not available", "PROVIDER_UNAVAILABLE");
            }

            // Process refund with provider
            var refundAmount = request.Amount ?? transaction.Amount;
            var refundRequest = new RefundPaymentRequest
            {
                Amount = refundAmount,
                Reason = request.Reason,
                Metadata = request.Metadata
            };
            var providerResult = await provider.RefundPaymentAsync(refundRequest);
            if (!providerResult.Success)
            {
                return PaymentServiceResult<PaymentTransactionDto>.ErrorResult(
                    providerResult.ErrorMessage ?? "Payment provider error", 
                    providerResult.ErrorCode ?? "PROVIDER_ERROR");
            }

            // Create refund transaction record
            var refundTransaction = new PaymentTransaction
            {
                UserId = userId,
                PaymentProvider = transaction.PaymentProvider,
                ExternalTransactionId = providerResult.ExternalTransactionId ?? Guid.NewGuid().ToString(),
                Amount = -refundAmount, // Negative amount for refunds
                Currency = transaction.Currency,
                Status = PaymentStatus.Completed,
                Type = PaymentType.Refund,
                Description = $"Refund for transaction {transaction.Id}: {request.Reason}",
                ProcessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(refundTransaction);
            await _context.SaveChangesAsync();

            // Map to DTO
            var refundDto = new PaymentTransactionDto
            {
                Id = refundTransaction.Id,
                UserId = refundTransaction.UserId,
                Username = transaction.User?.Username ?? string.Empty,
                SubscriptionTierId = refundTransaction.SubscriptionTierId,
                PaymentProvider = refundTransaction.PaymentProvider,
                ExternalTransactionId = refundTransaction.ExternalTransactionId,
                Amount = refundTransaction.Amount,
                Currency = refundTransaction.Currency,
                Status = refundTransaction.Status,
                Type = refundTransaction.Type,
                Description = refundTransaction.Description,
                ProcessedAt = refundTransaction.ProcessedAt,
                CreatedAt = refundTransaction.CreatedAt
            };

            return PaymentServiceResult<PaymentTransactionDto>.SuccessResult(refundDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment {TransactionId} for user {UserId}", transactionId, userId);
            return PaymentServiceResult<PaymentTransactionDto>.ErrorResult("Internal error processing refund", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<PaymentMethodDto>> AddPaymentMethodAsync(int userId, CreatePaymentMethodRequest request)
    {
        try
        {
            // Validate user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return PaymentServiceResult<PaymentMethodDto>.ErrorResult("User not found", "USER_NOT_FOUND");
            }

            // Get payment provider
            var provider = await _gatewayManager.GetBestProviderAsync(request.PaymentProvider);
            if (provider == null)
            {
                return PaymentServiceResult<PaymentMethodDto>.ErrorResult("No payment provider available", "NO_PROVIDER");
            }

            // Add payment method with provider
            var providerResult = await provider.CreatePaymentMethodAsync(request);
            if (!providerResult.Success)
            {
                return PaymentServiceResult<PaymentMethodDto>.ErrorResult(
                    providerResult.ErrorMessage ?? "Payment provider error", 
                    providerResult.ErrorCode ?? "PROVIDER_ERROR");
            }

            // If setting as default, unset other default payment methods
            if (request.SetAsDefault)
            {
                var existingDefaults = await _context.PaymentMethods
                    .Where(pm => pm.UserId == userId && pm.IsDefault)
                    .ToListAsync();
                
                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }

            // Create payment method record
            var paymentMethod = new PaymentMethod
            {
                UserId = userId,
                PaymentProvider = provider.ProviderName,
                ExternalPaymentMethodId = providerResult.ExternalPaymentMethodId!,
                Type = request.Type,
                Brand = providerResult.Brand,
                Last4 = providerResult.Last4,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                HolderName = request.HolderName,
                IsDefault = request.SetAsDefault,
                IsActive = true,
                IsVerified = providerResult.IsVerified,
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(request.ExpiryMonth) && !string.IsNullOrEmpty(request.ExpiryYear))
            {
                paymentMethod.ExpiresAt = new DateTime(int.Parse(request.ExpiryYear), int.Parse(request.ExpiryMonth), 1).AddMonths(1).AddDays(-1);
            }

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            // Map to DTO
            var paymentMethodDto = new PaymentMethodDto
            {
                Id = paymentMethod.Id,
                UserId = paymentMethod.UserId,
                PaymentProvider = paymentMethod.PaymentProvider,
                ExternalPaymentMethodId = paymentMethod.ExternalPaymentMethodId,
                Type = paymentMethod.Type,
                Brand = paymentMethod.Brand,
                Last4 = paymentMethod.Last4,
                ExpiryMonth = paymentMethod.ExpiryMonth,
                ExpiryYear = paymentMethod.ExpiryYear,
                HolderName = paymentMethod.HolderName,
                IsDefault = paymentMethod.IsDefault,
                IsActive = paymentMethod.IsActive,
                IsVerified = paymentMethod.IsVerified,
                ExpiresAt = paymentMethod.ExpiresAt,
                CreatedAt = paymentMethod.CreatedAt
            };

            return PaymentServiceResult<PaymentMethodDto>.SuccessResult(paymentMethodDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment method for user {UserId}", userId);
            return PaymentServiceResult<PaymentMethodDto>.ErrorResult("Internal error adding payment method", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<bool>> RemovePaymentMethodAsync(int userId, int paymentMethodId)
    {
        try
        {
            // Get the payment method
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.UserId == userId);

            if (paymentMethod == null)
            {
                return PaymentServiceResult<bool>.ErrorResult("Payment method not found", "PAYMENT_METHOD_NOT_FOUND");
            }

            // Get payment provider
            var provider = await _gatewayManager.GetProviderByNameAsync(paymentMethod.PaymentProvider);
            if (provider == null)
            {
                return PaymentServiceResult<bool>.ErrorResult("Payment provider not available", "PROVIDER_UNAVAILABLE");
            }

            // Remove payment method from provider
            var providerResult = await provider.DeletePaymentMethodAsync(paymentMethod.ExternalPaymentMethodId);
            if (!providerResult.Success)
            {
                return PaymentServiceResult<bool>.ErrorResult(
                    providerResult.ErrorMessage ?? "Payment provider error", 
                    providerResult.ErrorCode ?? "PROVIDER_ERROR");
            }

            // Mark as inactive instead of deleting
            paymentMethod.IsActive = false;
            paymentMethod.IsDefault = false;

            await _context.SaveChangesAsync();

            return PaymentServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing payment method {PaymentMethodId} for user {UserId}", paymentMethodId, userId);
            return PaymentServiceResult<bool>.ErrorResult("Internal error removing payment method", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<List<PaymentMethodDto>>> GetUserPaymentMethodsAsync(int userId)
    {
        try
        {
            var paymentMethods = await _context.PaymentMethods
                .Where(pm => pm.UserId == userId && pm.IsActive)
                .OrderByDescending(pm => pm.IsDefault)
                .ThenByDescending(pm => pm.CreatedAt)
                .ToListAsync();

            var paymentMethodDtos = paymentMethods.Select(pm => new PaymentMethodDto
            {
                Id = pm.Id,
                UserId = pm.UserId,
                PaymentProvider = pm.PaymentProvider,
                ExternalPaymentMethodId = pm.ExternalPaymentMethodId,
                Type = pm.Type,
                Brand = pm.Brand,
                Last4 = pm.Last4,
                ExpiryMonth = pm.ExpiryMonth,
                ExpiryYear = pm.ExpiryYear,
                HolderName = pm.HolderName,
                IsDefault = pm.IsDefault,
                IsActive = pm.IsActive,
                IsVerified = pm.IsVerified,
                ExpiresAt = pm.ExpiresAt,
                CreatedAt = pm.CreatedAt
            }).ToList();

            return PaymentServiceResult<List<PaymentMethodDto>>.SuccessResult(paymentMethodDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment methods for user {UserId}", userId);
            return PaymentServiceResult<List<PaymentMethodDto>>.ErrorResult("Internal error getting payment methods", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<PaymentMethodDto>> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId)
    {
        try
        {
            // Get the payment method
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.UserId == userId && pm.IsActive);

            if (paymentMethod == null)
            {
                return PaymentServiceResult<PaymentMethodDto>.ErrorResult("Payment method not found", "PAYMENT_METHOD_NOT_FOUND");
            }

            // Unset other default payment methods
            var existingDefaults = await _context.PaymentMethods
                .Where(pm => pm.UserId == userId && pm.IsDefault && pm.Id != paymentMethodId)
                .ToListAsync();
            
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }

            // Set as default
            paymentMethod.IsDefault = true;

            await _context.SaveChangesAsync();

            // Map to DTO
            var paymentMethodDto = new PaymentMethodDto
            {
                Id = paymentMethod.Id,
                UserId = paymentMethod.UserId,
                PaymentProvider = paymentMethod.PaymentProvider,
                ExternalPaymentMethodId = paymentMethod.ExternalPaymentMethodId,
                Type = paymentMethod.Type,
                Brand = paymentMethod.Brand,
                Last4 = paymentMethod.Last4,
                ExpiryMonth = paymentMethod.ExpiryMonth,
                ExpiryYear = paymentMethod.ExpiryYear,
                HolderName = paymentMethod.HolderName,
                IsDefault = paymentMethod.IsDefault,
                IsActive = paymentMethod.IsActive,
                IsVerified = paymentMethod.IsVerified,
                ExpiresAt = paymentMethod.ExpiresAt,
                CreatedAt = paymentMethod.CreatedAt
            };

            return PaymentServiceResult<PaymentMethodDto>.SuccessResult(paymentMethodDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method {PaymentMethodId} for user {UserId}", paymentMethodId, userId);
            return PaymentServiceResult<PaymentMethodDto>.ErrorResult("Internal error setting default payment method", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<List<PaymentTransactionDto>>> GetPaymentHistoryAsync(int userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var transactions = await _context.PaymentTransactions
                .Include(t => t.User)
                .Include(t => t.SubscriptionTier)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var transactionDtos = transactions.Select(t => new PaymentTransactionDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Username = t.User?.Username ?? string.Empty,
                SubscriptionTierId = t.SubscriptionTierId,
                SubscriptionTierName = t.SubscriptionTier?.Name ?? string.Empty,
                PaymentProvider = t.PaymentProvider,
                ExternalTransactionId = t.ExternalTransactionId,
                ExternalSubscriptionId = t.ExternalSubscriptionId,
                Amount = t.Amount,
                Currency = t.Currency,
                Status = t.Status,
                Type = t.Type,
                Description = t.Description,
                BillingPeriodStart = t.BillingPeriodStart,
                BillingPeriodEnd = t.BillingPeriodEnd,
                ProcessedAt = t.ProcessedAt,
                FailedAt = t.FailedAt,
                FailureReason = t.FailureReason,
                CreatedAt = t.CreatedAt
            }).ToList();

            return PaymentServiceResult<List<PaymentTransactionDto>>.SuccessResult(transactionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history for user {UserId}", userId);
            return PaymentServiceResult<List<PaymentTransactionDto>>.ErrorResult("Internal error getting payment history", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<bool>> HandleWebhookAsync(string providerName, WebhookRequest request)
    {
        try
        {
            // Get payment provider
            var provider = await _gatewayManager.GetProviderByNameAsync(providerName);
            if (provider == null)
            {
                return PaymentServiceResult<bool>.ErrorResult("Payment provider not found", "PROVIDER_NOT_FOUND");
            }

            // Verify webhook signature
            var isValid = await provider.VerifyWebhookAsync(request);
            if (!isValid)
            {
                return PaymentServiceResult<bool>.ErrorResult("Invalid webhook signature", "INVALID_SIGNATURE");
            }

            // Process webhook event
            var result = await provider.HandleWebhookAsync(request);
            if (!result.Success)
            {
                return PaymentServiceResult<bool>.ErrorResult(
                    result.ErrorMessage ?? "Webhook processing failed", 
                    result.ErrorCode ?? "WEBHOOK_PROCESSING_FAILED");
            }

            // Log webhook processing result for now
            // TODO: Implement proper webhook data processing based on event type
            _logger.LogInformation("Webhook processed successfully: {EventType}", result.EventType);

            await _context.SaveChangesAsync();

            return PaymentServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook for provider {ProviderName}", providerName);
            return PaymentServiceResult<bool>.ErrorResult("Internal error handling webhook", "INTERNAL_ERROR");
        }
    }

    public async Task<PaymentServiceResult<SubscriptionDto>> SyncSubscriptionStatusAsync(int userId)
    {
        try
        {
            // Get user's subscription
            var subscription = await _context.Set<UserSubscription>()
                .Include(s => s.SubscriptionTier)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && 
                    (s.Status == SubscriptionStatus.Active || 
                     s.Status == SubscriptionStatus.Trial ||
                     s.Status == SubscriptionStatus.PendingCancellation));

            if (subscription == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("No subscription found to sync", "NO_SUBSCRIPTION");
            }

            // Get payment provider
            var provider = await _gatewayManager.GetProviderByNameAsync(subscription.PaymentProvider);
            if (provider == null)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult("Payment provider not available", "PROVIDER_UNAVAILABLE");
            }

            // Get subscription status from provider
            var providerResult = await provider.GetSubscriptionAsync(subscription.ExternalSubscriptionId!);
            if (!providerResult.Success)
            {
                return PaymentServiceResult<SubscriptionDto>.ErrorResult(
                    providerResult.ErrorMessage ?? "Payment provider error", 
                    providerResult.ErrorCode ?? "PROVIDER_ERROR");
            }

            // Update local subscription with provider data
            var hasChanges = false;
            var providerStatus = ParseSubscriptionStatus(providerResult.Status);
            
            if (subscription.Status != providerStatus)
            {
                subscription.Status = providerStatus;
                hasChanges = true;
                
                if (providerStatus == SubscriptionStatus.Cancelled)
                {
                    subscription.EndDate = DateTime.UtcNow;
                    subscription.CancelledAt = DateTime.UtcNow;
                }
            }
            
            if (providerResult.NextBillingDate.HasValue && subscription.NextBillingDate != providerResult.NextBillingDate.Value)
            {
                subscription.NextBillingDate = providerResult.NextBillingDate.Value;
                hasChanges = true;
            }
            
            if (hasChanges)
            {
                subscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var subscriptionDto = MapToSubscriptionDto(subscription, subscription.SubscriptionTier, subscription.User);
            return PaymentServiceResult<SubscriptionDto>.SuccessResult(subscriptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing subscription status for user {UserId}", userId);
            return PaymentServiceResult<SubscriptionDto>.ErrorResult("Internal error syncing subscription", "INTERNAL_ERROR");
        }
    }
}
