using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Yapplr.Api.Services;

/// <summary>
/// Background service for processing notification queues and maintenance tasks
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30); // Process every 30 seconds
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Cleanup every hour
    private DateTime _lastCleanup = DateTime.UtcNow;

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationQueueAsync();
                await PerformMaintenanceTasksAsync();
                
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification background service");
                
                // Wait a bit longer before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Notification background service stopped");
    }

    private async Task ProcessNotificationQueueAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<INotificationQueueService>();

        try
        {
            await queueService.ProcessPendingNotificationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification queue");
        }
    }

    private async Task PerformMaintenanceTasksAsync()
    {
        if (DateTime.UtcNow - _lastCleanup < _cleanupInterval)
        {
            return; // Not time for cleanup yet
        }

        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            // Cleanup old notifications
            var queueService = scope.ServiceProvider.GetRequiredService<INotificationQueueService>();
            await queueService.CleanupOldNotificationsAsync(TimeSpan.FromDays(7)); // Keep 7 days

            // Cleanup inactive SignalR connections
            var connectionPool = scope.ServiceProvider.GetRequiredService<ISignalRConnectionPool>();
            await connectionPool.CleanupInactiveConnectionsAsync(TimeSpan.FromHours(24)); // 24 hours inactive

            _lastCleanup = DateTime.UtcNow;
            _logger.LogInformation("Completed maintenance tasks");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing maintenance tasks");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification background service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
