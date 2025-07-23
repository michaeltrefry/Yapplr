using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Payment;

public class PaymentAdminService : IPaymentAdminService
{
    private readonly YapplrDbContext _context;
    private readonly IPaymentGatewayService _paymentService;
    private readonly ILogger<PaymentAdminService> _logger;

    public PaymentAdminService(
        YapplrDbContext context,
        IPaymentGatewayService paymentService,
        ILogger<PaymentAdminService> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<PagedResult<AdminSubscriptionDto>> GetSubscriptionsAsync(int page = 1, int pageSize = 25, string? status = null, string? provider = null)
    {
        var query = _context.UserSubscriptions
            .Include(s => s.User)
            .Include(s => s.SubscriptionTier)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<SubscriptionStatus>(status, true, out var statusEnum))
            {
                query = query.Where(s => s.Status == statusEnum);
            }
        }

        if (!string.IsNullOrEmpty(provider))
        {
            query = query.Where(s => s.PaymentProvider == provider);
        }

        var totalCount = await query.CountAsync();
        var subscriptions = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<AdminSubscriptionDto>();
        foreach (var subscription in subscriptions)
        {
            var totalPaid = await _context.PaymentTransactions
                .Where(t => t.UserId == subscription.UserId && 
                           t.Status == PaymentStatus.Completed)
                .SumAsync(t => t.Amount);

            items.Add(new AdminSubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                Username = subscription.User.Username,
                Email = subscription.User.Email,
                SubscriptionTierId = subscription.SubscriptionTierId,
                SubscriptionTierName = subscription.SubscriptionTier.Name,
                PaymentProvider = subscription.PaymentProvider,
                ExternalSubscriptionId = subscription.ExternalSubscriptionId,
                Status = subscription.Status.ToString(),
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                NextBillingDate = subscription.NextBillingDate,
                CancelledAt = subscription.CancelledAt,
                CancellationReason = subscription.CancellationReason,
                IsTrialPeriod = subscription.IsTrialPeriod,
                TrialEndDate = subscription.TrialEndDate,
                PaymentMethodType = subscription.PaymentMethodType,
                PaymentMethodLast4 = subscription.PaymentMethodLast4,
                BillingCycleCount = subscription.BillingCycleCount,
                GracePeriodEndDate = subscription.GracePeriodEndDate,
                RetryCount = subscription.RetryCount,
                LastRetryDate = subscription.LastRetryDate,
                LastSyncDate = subscription.LastSyncDate,
                TotalPaid = totalPaid,
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt
            });
        }

        return new PagedResult<AdminSubscriptionDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminSubscriptionDto?> GetSubscriptionAsync(int subscriptionId)
    {
        var subscription = await _context.UserSubscriptions
            .Include(s => s.User)
            .Include(s => s.SubscriptionTier)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);

        if (subscription == null)
            return null;

        var totalPaid = await _context.PaymentTransactions
            .Where(t => t.UserId == subscription.UserId && 
                       t.Status == PaymentStatus.Completed)
            .SumAsync(t => t.Amount);

        return new AdminSubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            Username = subscription.User.Username,
            Email = subscription.User.Email,
            SubscriptionTierId = subscription.SubscriptionTierId,
            SubscriptionTierName = subscription.SubscriptionTier.Name,
            PaymentProvider = subscription.PaymentProvider,
            ExternalSubscriptionId = subscription.ExternalSubscriptionId,
            Status = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            NextBillingDate = subscription.NextBillingDate,
            CancelledAt = subscription.CancelledAt,
            CancellationReason = subscription.CancellationReason,
            IsTrialPeriod = subscription.IsTrialPeriod,
            TrialEndDate = subscription.TrialEndDate,
            PaymentMethodType = subscription.PaymentMethodType,
            PaymentMethodLast4 = subscription.PaymentMethodLast4,
            BillingCycleCount = subscription.BillingCycleCount,
            GracePeriodEndDate = subscription.GracePeriodEndDate,
            RetryCount = subscription.RetryCount,
            LastRetryDate = subscription.LastRetryDate,
            LastSyncDate = subscription.LastSyncDate,
            TotalPaid = totalPaid,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
    }

    public async Task<PaymentServiceResult<bool>> CancelSubscriptionAsync(int subscriptionId, CancelSubscriptionRequest request)
    {
        try
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
            {
                return PaymentServiceResult<bool>.ErrorResult("Subscription not found");
            }

            var result = await _paymentService.CancelSubscriptionAsync(subscription.UserId, request);
            return PaymentServiceResult<bool>.SuccessResult(result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            return PaymentServiceResult<bool>.ErrorResult("Internal error cancelling subscription");
        }
    }

    public async Task<PaymentServiceResult<AdminSubscriptionDto>> SyncSubscriptionAsync(int subscriptionId)
    {
        try
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
            {
                return PaymentServiceResult<AdminSubscriptionDto>.ErrorResult("Subscription not found");
            }

            var syncResult = await _paymentService.SyncSubscriptionStatusAsync(subscription.UserId);
            if (!syncResult.Success)
            {
                return PaymentServiceResult<AdminSubscriptionDto>.ErrorResult(syncResult.ErrorMessage ?? "Sync failed");
            }

            var updatedSubscription = await GetSubscriptionAsync(subscriptionId);
            if (updatedSubscription == null)
            {
                return PaymentServiceResult<AdminSubscriptionDto>.ErrorResult("Failed to retrieve updated subscription");
            }

            return PaymentServiceResult<AdminSubscriptionDto>.SuccessResult(updatedSubscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing subscription {SubscriptionId}", subscriptionId);
            return PaymentServiceResult<AdminSubscriptionDto>.ErrorResult("Internal error syncing subscription");
        }
    }

    public async Task<PagedResult<AdminTransactionDto>> GetTransactionsAsync(int page = 1, int pageSize = 25, string? status = null, string? provider = null, int? userId = null)
    {
        var query = _context.PaymentTransactions
            .Include(t => t.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<PaymentStatus>(status, true, out var statusEnum))
            {
                query = query.Where(t => t.Status == statusEnum);
            }
        }

        if (!string.IsNullOrEmpty(provider))
        {
            query = query.Where(t => t.PaymentProvider == provider);
        }

        if (userId.HasValue)
        {
            query = query.Where(t => t.UserId == userId.Value);
        }

        var totalCount = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = transactions.Select(t => new AdminTransactionDto
        {
            Id = t.Id,
            UserId = t.UserId,
            Username = t.User.Username,
            PaymentProvider = t.PaymentProvider,
            ExternalTransactionId = t.ExternalTransactionId,
            ExternalSubscriptionId = t.ExternalSubscriptionId,
            Amount = t.Amount,
            Currency = t.Currency,
            Status = t.Status.ToString(),
            Type = t.Type.ToString(),
            Description = t.Description,
            BillingPeriodStart = t.BillingPeriodStart,
            BillingPeriodEnd = t.BillingPeriodEnd,
            ProcessedAt = t.ProcessedAt,
            FailedAt = t.FailedAt,
            FailureReason = t.FailureReason,
            WebhookEventId = t.WebhookEventId,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();

        return new PagedResult<AdminTransactionDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminTransactionDto?> GetTransactionAsync(int transactionId)
    {
        var transaction = await _context.PaymentTransactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
            return null;

        return new AdminTransactionDto
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            Username = transaction.User.Username,
            PaymentProvider = transaction.PaymentProvider,
            ExternalTransactionId = transaction.ExternalTransactionId,
            ExternalSubscriptionId = transaction.ExternalSubscriptionId,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Status = transaction.Status.ToString(),
            Type = transaction.Type.ToString(),
            Description = transaction.Description,
            BillingPeriodStart = transaction.BillingPeriodStart,
            BillingPeriodEnd = transaction.BillingPeriodEnd,
            ProcessedAt = transaction.ProcessedAt,
            FailedAt = transaction.FailedAt,
            FailureReason = transaction.FailureReason,
            WebhookEventId = transaction.WebhookEventId,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }

    public async Task<PaymentServiceResult<RefundPaymentResult>> RefundTransactionAsync(int transactionId, RefundPaymentRequest request)
    {
        try
        {
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return PaymentServiceResult<RefundPaymentResult>.ErrorResult("Transaction not found");
            }

            // Add transaction ID to metadata for provider processing
            request.Metadata ??= new Dictionary<string, string>();
            request.Metadata["transaction_id"] = transaction.ExternalTransactionId;

            var result = await _paymentService.RefundPaymentAsync(transaction.UserId, transaction.Id, request);
            if (result.Success && result.Data != null)
            {
                // Convert PaymentTransactionDto to RefundPaymentResult
                var refundResult = new RefundPaymentResult
                {
                    Success = true,
                    ExternalRefundId = result.Data.ExternalTransactionId,
                    ExternalTransactionId = transaction.ExternalTransactionId,
                    RefundedAmount = result.Data.Amount,
                    Currency = result.Data.Currency,
                    RefundedAt = result.Data.ProcessedAt ?? DateTime.UtcNow,
                    Status = result.Data.Status.ToString()
                };
                return PaymentServiceResult<RefundPaymentResult>.SuccessResult(refundResult);
            }
            return PaymentServiceResult<RefundPaymentResult>.ErrorResult(result.ErrorMessage ?? "Refund failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding transaction {TransactionId}", transactionId);
            return PaymentServiceResult<RefundPaymentResult>.ErrorResult("Internal error processing refund");
        }
    }

    public async Task<PaymentAnalyticsDto> GetPaymentAnalyticsAsync(int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        // Get subscription counts
        var totalSubscriptions = await _context.UserSubscriptions.CountAsync();
        var activeSubscriptions = await _context.UserSubscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active);
        var cancelledSubscriptions = await _context.UserSubscriptions.CountAsync(s => s.Status == SubscriptionStatus.Cancelled);
        var trialSubscriptions = await _context.UserSubscriptions.CountAsync(s => s.Status == SubscriptionStatus.Trial);
        var pastDueSubscriptions = await _context.UserSubscriptions.CountAsync(s => s.Status == SubscriptionStatus.PastDue);
        var suspendedSubscriptions = await _context.UserSubscriptions.CountAsync(s => s.Status == SubscriptionStatus.Suspended);

        // Get revenue data
        var totalRevenue = await _context.PaymentTransactions
            .Where(t => t.Status == PaymentStatus.Completed && t.CreatedAt >= startDate)
            .SumAsync(t => t.Amount);

        var monthlyRevenue = await _context.PaymentTransactions
            .Where(t => t.Status == PaymentStatus.Completed &&
                       t.Type == PaymentType.Subscription &&
                       t.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(t => t.Amount);

        var averageRevenuePerUser = activeSubscriptions > 0 ? monthlyRevenue / activeSubscriptions : 0;

        // Calculate churn rate (simplified)
        var previousMonthActive = await _context.UserSubscriptions
            .CountAsync(s => s.CreatedAt <= DateTime.UtcNow.AddDays(-30) &&
                            (s.EndDate == null || s.EndDate > DateTime.UtcNow.AddDays(-30)));
        var churnRate = previousMonthActive > 0 ? (double)cancelledSubscriptions / previousMonthActive * 100 : 0;

        // Get transaction stats
        var totalTransactions = await _context.PaymentTransactions
            .CountAsync(t => t.CreatedAt >= startDate);
        var successfulTransactions = await _context.PaymentTransactions
            .CountAsync(t => t.Status == PaymentStatus.Completed && t.CreatedAt >= startDate);
        var failedTransactions = await _context.PaymentTransactions
            .CountAsync(t => t.Status == PaymentStatus.Failed && t.CreatedAt >= startDate);
        var successRate = totalTransactions > 0 ? (double)successfulTransactions / totalTransactions * 100 : 0;

        // Get daily revenue
        var dailyRevenue = await _context.PaymentTransactions
            .Where(t => t.Status == PaymentStatus.Completed && t.CreatedAt >= startDate)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        // Get provider stats
        var providerStats = await _context.UserSubscriptions
            .GroupBy(s => s.PaymentProvider)
            .Select(g => new ProviderStatsDto
            {
                ProviderName = g.Key,
                SubscriptionCount = g.Count(),
                Revenue = _context.PaymentTransactions
                    .Where(t => t.PaymentProvider == g.Key &&
                               t.Status == PaymentStatus.Completed &&
                               t.CreatedAt >= startDate)
                    .Sum(t => t.Amount),
                SuccessRate = _context.PaymentTransactions
                    .Where(t => t.PaymentProvider == g.Key && t.CreatedAt >= startDate)
                    .Count() > 0 ?
                    (double)_context.PaymentTransactions
                        .Where(t => t.PaymentProvider == g.Key &&
                                   t.Status == PaymentStatus.Completed &&
                                   t.CreatedAt >= startDate)
                        .Count() /
                    _context.PaymentTransactions
                        .Where(t => t.PaymentProvider == g.Key && t.CreatedAt >= startDate)
                        .Count() * 100 : 0
            })
            .ToListAsync();

        return new PaymentAnalyticsDto
        {
            TotalSubscriptions = totalSubscriptions,
            ActiveSubscriptions = activeSubscriptions,
            CancelledSubscriptions = cancelledSubscriptions,
            TrialSubscriptions = trialSubscriptions,
            PastDueSubscriptions = pastDueSubscriptions,
            SuspendedSubscriptions = suspendedSubscriptions,
            TotalRevenue = totalRevenue,
            MonthlyRecurringRevenue = monthlyRevenue,
            AverageRevenuePerUser = averageRevenuePerUser,
            ChurnRate = churnRate,
            TotalTransactions = totalTransactions,
            SuccessfulTransactions = successfulTransactions,
            FailedTransactions = failedTransactions,
            SuccessRate = successRate,
            DailyRevenue = dailyRevenue,
            ProviderStats = providerStats
        };
    }

    public async Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(int days = 30, string? provider = null)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var previousStartDate = DateTime.UtcNow.AddDays(-days * 2);

        var query = _context.PaymentTransactions
            .Where(t => t.Status == PaymentStatus.Completed);

        if (!string.IsNullOrEmpty(provider))
        {
            query = query.Where(t => t.PaymentProvider == provider);
        }

        var totalRevenue = await query
            .Where(t => t.CreatedAt >= startDate)
            .SumAsync(t => t.Amount);

        var previousPeriodRevenue = await query
            .Where(t => t.CreatedAt >= previousStartDate && t.CreatedAt < startDate)
            .SumAsync(t => t.Amount);

        var growthRate = previousPeriodRevenue > 0 ?
            (double)(totalRevenue - previousPeriodRevenue) / (double)previousPeriodRevenue * 100 : 0;

        // Get daily revenue
        var dailyRevenue = await query
            .Where(t => t.CreatedAt >= startDate)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        // Get monthly revenue (last 12 months)
        var monthlyRevenue = await query
            .Where(t => t.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
            .Select(g => new MonthlyRevenueDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(t => t.Amount),
                SubscriptionCount = g.Select(t => t.UserId).Distinct().Count()
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        // Get revenue by tier
        var revenueByTier = await _context.PaymentTransactions
            .Include(t => t.SubscriptionTier)
            .Where(t => t.Status == PaymentStatus.Completed && t.CreatedAt >= startDate)
            .GroupBy(t => t.SubscriptionTier.Name)
            .Select(g => new TierRevenueDto
            {
                TierName = g.Key,
                Revenue = g.Sum(t => t.Amount),
                SubscriptionCount = g.Select(t => t.UserId).Distinct().Count()
            })
            .ToListAsync();

        // Calculate percentages
        foreach (var tier in revenueByTier)
        {
            tier.Percentage = totalRevenue > 0 ? (double)tier.Revenue / (double)totalRevenue * 100 : 0;
        }

        return new RevenueAnalyticsDto
        {
            TotalRevenue = totalRevenue,
            PreviousPeriodRevenue = previousPeriodRevenue,
            GrowthRate = growthRate,
            DailyRevenue = dailyRevenue,
            MonthlyRevenue = monthlyRevenue,
            RevenueByTier = revenueByTier
        };
    }

    public async Task<IEnumerable<PaymentProviderStatusDto>> GetPaymentProvidersAsync()
    {
        var providers = await _paymentService.GetAvailableProvidersAsync();
        var result = new List<PaymentProviderStatusDto>();

        foreach (var provider in providers)
        {
            var activeSubscriptions = await _context.UserSubscriptions
                .CountAsync(s => s.PaymentProvider == provider.Name && s.Status == SubscriptionStatus.Active);

            var totalRevenue = await _context.PaymentTransactions
                .Where(t => t.PaymentProvider == provider.Name && t.Status == PaymentStatus.Completed)
                .SumAsync(t => t.Amount);

            result.Add(new PaymentProviderStatusDto
            {
                Name = provider.Name,
                IsEnabled = true, // Would come from configuration
                IsAvailable = provider.IsAvailable,
                Environment = "Production", // Would come from configuration
                SupportedCurrencies = provider.SupportedCurrencies,
                SupportedPaymentMethods = provider.SupportedPaymentMethods,
                LastHealthCheck = DateTime.UtcNow, // Would be stored in a real implementation
                HealthStatus = provider.IsAvailable ? "Healthy" : "Unavailable",
                ActiveSubscriptions = activeSubscriptions,
                TotalRevenue = totalRevenue
            });
        }

        return result;
    }

    public async Task<PaymentServiceResult<bool>> TestPaymentProviderAsync(string providerName)
    {
        try
        {
            var provider = await _paymentService.GetBestProviderAsync(providerName);
            if (provider == null)
            {
                return PaymentServiceResult<bool>.ErrorResult("Provider not found");
            }

            var isAvailable = await provider.IsAvailableAsync();
            return PaymentServiceResult<bool>.SuccessResult(isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing payment provider {ProviderName}", providerName);
            return PaymentServiceResult<bool>.ErrorResult("Error testing provider");
        }
    }

    public async Task<PagedResult<FailedPaymentDto>> GetFailedPaymentsAsync(int page = 1, int pageSize = 25)
    {
        var query = _context.UserSubscriptions
            .Include(s => s.User)
            .Where(s => s.Status == SubscriptionStatus.PastDue || s.RetryCount > 0);

        var totalCount = await query.CountAsync();
        var subscriptions = await query
            .OrderByDescending(s => s.LastRetryDate ?? s.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = subscriptions.Select(s => new FailedPaymentDto
        {
            Id = s.Id,
            UserId = s.UserId,
            Username = s.User.Username,
            PaymentProvider = s.PaymentProvider,
            ExternalSubscriptionId = s.ExternalSubscriptionId,
            Amount = 0, // Would need to calculate from subscription tier
            Currency = "USD", // Would come from subscription tier
            FailureReason = "Payment failed", // Would come from last transaction
            RetryCount = s.RetryCount,
            LastRetryDate = s.LastRetryDate,
            NextRetryDate = s.LastRetryDate?.AddDays(3), // Based on retry logic
            FailedAt = s.LastRetryDate ?? s.UpdatedAt,
            CanRetry = s.RetryCount < 3 // Based on max retry configuration
        }).ToList();

        return new PagedResult<FailedPaymentDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaymentServiceResult<bool>> RetryFailedPaymentAsync(int failedPaymentId)
    {
        try
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.Id == failedPaymentId);

            if (subscription == null)
            {
                return PaymentServiceResult<bool>.ErrorResult("Failed payment not found");
            }

            var result = await _paymentService.SyncSubscriptionStatusAsync(subscription.UserId);
            return PaymentServiceResult<bool>.SuccessResult(result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed payment {FailedPaymentId}", failedPaymentId);
            return PaymentServiceResult<bool>.ErrorResult("Error retrying payment");
        }
    }

    public async Task<IEnumerable<AdminSubscriptionTierDto>> GetSubscriptionTiersAsync()
    {
        var tiers = await _context.SubscriptionTiers.ToListAsync();
        var result = new List<AdminSubscriptionTierDto>();

        foreach (var tier in tiers)
        {
            var activeSubscriptions = await _context.UserSubscriptions
                .CountAsync(s => s.SubscriptionTierId == tier.Id && s.Status == SubscriptionStatus.Active);

            var totalRevenue = await _context.PaymentTransactions
                .Where(t => t.SubscriptionTierId == tier.Id && t.Status == PaymentStatus.Completed)
                .SumAsync(t => t.Amount);

            result.Add(new AdminSubscriptionTierDto
            {
                Id = tier.Id,
                Name = tier.Name,
                Description = tier.Description,
                Price = tier.Price,
                Currency = tier.Currency,
                BillingCycleMonths = tier.BillingCycleMonths,
                IsActive = tier.IsActive,
                ActiveSubscriptions = activeSubscriptions,
                TotalRevenue = totalRevenue,
                CreatedAt = tier.CreatedAt,
                UpdatedAt = tier.UpdatedAt
            });
        }

        return result;
    }

    public async Task<PaymentServiceResult<AdminSubscriptionTierDto>> UpdateSubscriptionTierAsync(int tierId, UpdateSubscriptionTierRequest request)
    {
        try
        {
            var tier = await _context.SubscriptionTiers.FirstOrDefaultAsync(t => t.Id == tierId);
            if (tier == null)
            {
                return PaymentServiceResult<AdminSubscriptionTierDto>.ErrorResult("Subscription tier not found");
            }

            if (!string.IsNullOrEmpty(request.Name))
                tier.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Description))
                tier.Description = request.Description;

            if (request.Price.HasValue)
                tier.Price = request.Price.Value;

            if (request.IsActive.HasValue)
                tier.IsActive = request.IsActive.Value;

            tier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updatedTier = (await GetSubscriptionTiersAsync()).FirstOrDefault(t => t.Id == tierId);
            if (updatedTier == null)
            {
                return PaymentServiceResult<AdminSubscriptionTierDto>.ErrorResult("Failed to retrieve updated tier");
            }

            return PaymentServiceResult<AdminSubscriptionTierDto>.SuccessResult(updatedTier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription tier {TierId}", tierId);
            return PaymentServiceResult<AdminSubscriptionTierDto>.ErrorResult("Error updating subscription tier");
        }
    }

    public async Task<PagedResult<WebhookLogDto>> GetWebhookLogsAsync(int page = 1, int pageSize = 25, string? provider = null)
    {
        // This would require a WebhookLog table in a real implementation
        // For now, return empty result
        return new PagedResult<WebhookLogDto>
        {
            Items = new List<WebhookLogDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaymentServiceResult<bool>> ReplayWebhookAsync(int webhookLogId)
    {
        // This would require webhook replay functionality in a real implementation
        await Task.CompletedTask;
        return PaymentServiceResult<bool>.ErrorResult("Webhook replay not implemented");
    }
}
