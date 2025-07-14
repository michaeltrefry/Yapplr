using Microsoft.Extensions.Options;

namespace Yapplr.Api.Services;

/// <summary>
/// Background service that periodically recalculates trust scores and applies decay
/// </summary>
public class TrustScoreBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrustScoreBackgroundService> _logger;
    private readonly TrustScoreBackgroundOptions _options;

    public TrustScoreBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TrustScoreBackgroundService> logger,
        IOptions<TrustScoreBackgroundOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trust Score Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTrustScoreMaintenanceAsync();
                
                // Wait for the configured interval before next run
                await Task.Delay(_options.RunInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in trust score background service");
                
                // Wait a shorter time before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Trust Score Background Service stopped");
    }

    private async Task ProcessTrustScoreMaintenanceAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var trustScoreService = scope.ServiceProvider.GetRequiredService<ITrustScoreService>();

        _logger.LogInformation("Starting trust score maintenance cycle");

        try
        {
            // Apply inactivity decay
            if (_options.EnableInactivityDecay)
            {
                var decayedUsers = await trustScoreService.ApplyInactivityDecayAsync(
                    _options.InactivityDays, 
                    _options.DecayRate);
                
                if (decayedUsers > 0)
                {
                    _logger.LogInformation("Applied inactivity decay to {Count} users", decayedUsers);
                }
            }

            // Recalculate trust scores for a batch of users
            if (_options.EnablePeriodicRecalculation)
            {
                var recalculatedUsers = await trustScoreService.RecalculateAllTrustScoresAsync(
                    _options.RecalculationBatchSize);
                
                if (recalculatedUsers > 0)
                {
                    _logger.LogInformation("Recalculated trust scores for {Count} users", recalculatedUsers);
                }
            }

            // Log statistics
            if (_options.EnableStatisticsLogging)
            {
                var stats = await trustScoreService.GetTrustScoreStatisticsAsync();
                _logger.LogInformation("Trust Score Statistics: {Stats}", 
                    System.Text.Json.JsonSerializer.Serialize(stats));
            }

            // Check for users with critically low trust scores
            if (_options.EnableLowTrustScoreAlerts)
            {
                var lowTrustUsers = await trustScoreService.GetUsersWithLowTrustScoresAsync(
                    _options.LowTrustScoreThreshold, 
                    _options.LowTrustScoreAlertLimit);
                
                if (lowTrustUsers.Any())
                {
                    _logger.LogWarning("Found {Count} users with trust scores below {Threshold}", 
                        lowTrustUsers.Count, _options.LowTrustScoreThreshold);
                }
            }

            _logger.LogDebug("Trust score maintenance cycle completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during trust score maintenance cycle");
            throw;
        }
    }
}