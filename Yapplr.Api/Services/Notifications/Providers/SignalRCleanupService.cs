namespace Yapplr.Api.Services.Notifications.Providers;

/// <summary>
/// Background service to periodically clean up inactive SignalR connections
/// </summary>
public class SignalRCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SignalRCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _inactivityThreshold = TimeSpan.FromHours(2);

    public SignalRCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SignalRCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SignalR cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var connectionPool = scope.ServiceProvider.GetRequiredService<ISignalRConnectionPool>();

                _logger.LogInformation("Starting SignalR connection cleanup...");
                
                var statsBefore = await connectionPool.GetStatsAsync();
                await connectionPool.CleanupInactiveConnectionsAsync(_inactivityThreshold);
                var statsAfter = await connectionPool.GetStatsAsync();

                var cleaned = statsBefore.TotalConnections - statsAfter.TotalConnections;
                if (cleaned > 0)
                {
                    _logger.LogInformation("Cleaned up {CleanedConnections} inactive connections. " +
                        "Active connections: {ActiveConnections}, Active users: {ActiveUsers}",
                        cleaned, statsAfter.TotalConnections, statsAfter.ActiveUsers);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SignalR connection cleanup");
            }
        }

        _logger.LogInformation("SignalR cleanup service stopped");
    }
}
