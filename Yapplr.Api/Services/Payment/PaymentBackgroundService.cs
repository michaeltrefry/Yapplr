using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services.Payment;

/// <summary>
/// Background service to handle payment-related tasks like subscription renewals,
/// failed payment retries, and subscription status synchronization
/// </summary>
public class PaymentBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

    public PaymentBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PaymentBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPaymentTasks();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing payment tasks");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Payment Background Service stopped");
    }

    private async Task ProcessPaymentTasks()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentGatewayService>();
        var config = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<PaymentProvidersConfiguration>>().CurrentValue;

        var currentTime = DateTime.UtcNow;

        _logger.LogDebug("Processing payment tasks at {CurrentTime}", currentTime);

        // Process different payment tasks
        await ProcessExpiredTrials(context, paymentService, currentTime);
        await ProcessFailedPaymentRetries(context, paymentService, config, currentTime);
        await ProcessSubscriptionStatusSync(context, paymentService, currentTime);
        await ProcessGracePeriodExpired(context, config, currentTime);
    }

    private async Task ProcessExpiredTrials(YapplrDbContext context, IPaymentGatewayService paymentService, DateTime currentTime)
    {
        try
        {
            // Find subscriptions with expired trials that haven't been processed
            var expiredTrials = await context.UserSubscriptions
                .Where(s => s.Status == Models.SubscriptionStatus.Trial &&
                           s.TrialEndDate.HasValue &&
                           s.TrialEndDate.Value <= currentTime &&
                           !s.TrialProcessed)
                .ToListAsync();

            foreach (var subscription in expiredTrials)
            {
                try
                {
                    _logger.LogInformation("Processing expired trial for user {UserId}, subscription {SubscriptionId}", 
                        subscription.UserId, subscription.Id);

                    // Attempt to charge the user for the first billing cycle
                    var syncResult = await paymentService.SyncSubscriptionStatusAsync(subscription.UserId);
                    
                    if (syncResult.Success && syncResult.Data != null)
                    {
                        // Status is already an enum, no conversion needed
                        subscription.Status = syncResult.Data.Status;
                        subscription.TrialProcessed = true;
                        subscription.UpdatedAt = currentTime;
                        
                        _logger.LogInformation("Trial processed successfully for user {UserId}", subscription.UserId);
                    }
                    else
                    {
                        // Mark trial as processed but keep in trial status for manual review
                        subscription.TrialProcessed = true;
                        subscription.UpdatedAt = currentTime;
                        
                        _logger.LogWarning("Failed to process trial for user {UserId}: {Error}", 
                            subscription.UserId, syncResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired trial for user {UserId}", subscription.UserId);
                }
            }

            if (expiredTrials.Any())
            {
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired trials");
        }
    }

    private async Task ProcessFailedPaymentRetries(YapplrDbContext context, IPaymentGatewayService paymentService, 
        PaymentProvidersConfiguration config, DateTime currentTime)
    {
        try
        {
            // Find subscriptions with failed payments that need retry
            var failedSubscriptions = await context.UserSubscriptions
                .Where(s => s.Status == Models.SubscriptionStatus.PastDue &&
                           s.RetryCount < config.Global.MaxPaymentRetries &&
                           (!s.LastRetryDate.HasValue ||
                            s.LastRetryDate.Value.AddDays(config.Global.RetryIntervalDays) <= currentTime))
                .ToListAsync();

            foreach (var subscription in failedSubscriptions)
            {
                try
                {
                    _logger.LogInformation("Retrying failed payment for user {UserId}, attempt {RetryCount}/{MaxRetries}", 
                        subscription.UserId, subscription.RetryCount + 1, config.Global.MaxPaymentRetries);

                    // Sync subscription status to trigger payment retry
                    var syncResult = await paymentService.SyncSubscriptionStatusAsync(subscription.UserId);
                    
                    subscription.RetryCount++;
                    subscription.LastRetryDate = currentTime;
                    subscription.UpdatedAt = currentTime;
                    
                    if (syncResult.Success && syncResult.Data != null)
                    {
                        // Status is already an enum, no conversion needed
                        var newStatus = syncResult.Data.Status;

                        subscription.Status = newStatus;

                        if (newStatus == SubscriptionStatus.Active)
                        {
                            // Payment succeeded, reset retry count
                            subscription.RetryCount = 0;
                            subscription.LastRetryDate = null;

                            _logger.LogInformation("Payment retry successful for user {UserId}", subscription.UserId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Payment retry failed for user {UserId}: {Error}", 
                            subscription.UserId, syncResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrying payment for user {UserId}", subscription.UserId);
                }
            }

            if (failedSubscriptions.Any())
            {
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing failed payment retries");
        }
    }

    private async Task ProcessSubscriptionStatusSync(YapplrDbContext context, IPaymentGatewayService paymentService, DateTime currentTime)
    {
        try
        {
            // Find active subscriptions that haven't been synced recently (every 24 hours)
            var subscriptionsToSync = await context.UserSubscriptions
                .Where(s => (s.Status == Models.SubscriptionStatus.Active || s.Status == Models.SubscriptionStatus.PastDue) &&
                           (!s.LastSyncDate.HasValue || s.LastSyncDate.Value.AddHours(24) <= currentTime))
                .OrderBy(s => s.LastSyncDate ?? s.CreatedAt) // Order by last sync date (oldest first) to ensure fair processing
                .Take(50) // Limit to avoid overwhelming the payment providers
                .ToListAsync();

            foreach (var subscription in subscriptionsToSync)
            {
                try
                {
                    _logger.LogDebug("Syncing subscription status for user {UserId}", subscription.UserId);

                    var syncResult = await paymentService.SyncSubscriptionStatusAsync(subscription.UserId);
                    
                    subscription.LastSyncDate = currentTime;
                    subscription.UpdatedAt = currentTime;
                    
                    if (syncResult.Success && syncResult.Data != null)
                    {
                        var oldStatus = subscription.Status;

                        // Status is already an enum, no conversion needed
                        var newStatus = syncResult.Data.Status;

                        subscription.Status = newStatus;

                        if (oldStatus != subscription.Status)
                        {
                            _logger.LogInformation("Subscription status changed for user {UserId}: {OldStatus} -> {NewStatus}",
                                subscription.UserId, oldStatus.ToString(), subscription.Status.ToString());
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to sync subscription for user {UserId}: {Error}", 
                            subscription.UserId, syncResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error syncing subscription for user {UserId}", subscription.UserId);
                }
            }

            if (subscriptionsToSync.Any())
            {
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription status sync");
        }
    }

    private async Task ProcessGracePeriodExpired(YapplrDbContext context, PaymentProvidersConfiguration config, DateTime currentTime)
    {
        try
        {
            // Find subscriptions that have exceeded the grace period
            var expiredGracePeriod = await context.UserSubscriptions
                .Where(s => s.Status == Models.SubscriptionStatus.PastDue &&
                           s.RetryCount >= config.Global.MaxPaymentRetries &&
                           s.LastRetryDate.HasValue &&
                           s.LastRetryDate.Value.AddDays(config.Global.GracePeriodDays) <= currentTime)
                .ToListAsync();

            foreach (var subscription in expiredGracePeriod)
            {
                try
                {
                    _logger.LogInformation("Grace period expired for user {UserId}, suspending subscription", subscription.UserId);

                    subscription.Status = Models.SubscriptionStatus.Suspended;
                    subscription.EndDate = currentTime;
                    subscription.UpdatedAt = currentTime;
                    
                    // TODO: Send notification to user about suspension
                    // TODO: Trigger any necessary cleanup (e.g., disable premium features)
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error suspending subscription for user {UserId}", subscription.UserId);
                }
            }

            if (expiredGracePeriod.Any())
            {
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing grace period expired subscriptions");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Background Service is stopping");
        await base.StopAsync(stoppingToken);
    }
}
