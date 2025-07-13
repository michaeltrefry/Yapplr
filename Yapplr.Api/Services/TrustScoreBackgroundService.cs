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

/// <summary>
/// Configuration options for the trust score background service
/// </summary>
public class TrustScoreBackgroundOptions
{
    public const string SectionName = "TrustScoreBackground";

    /// <summary>
    /// How often to run the background service
    /// </summary>
    public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Whether to apply inactivity decay
    /// </summary>
    public bool EnableInactivityDecay { get; set; } = true;

    /// <summary>
    /// Number of days of inactivity before decay starts
    /// </summary>
    public int InactivityDays { get; set; } = 30;

    /// <summary>
    /// Daily decay rate for inactive users
    /// </summary>
    public float DecayRate { get; set; } = 0.005f;

    /// <summary>
    /// Whether to periodically recalculate trust scores
    /// </summary>
    public bool EnablePeriodicRecalculation { get; set; } = true;

    /// <summary>
    /// Batch size for recalculation
    /// </summary>
    public int RecalculationBatchSize { get; set; } = 50;

    /// <summary>
    /// Whether to log trust score statistics
    /// </summary>
    public bool EnableStatisticsLogging { get; set; } = true;

    /// <summary>
    /// Whether to alert on low trust scores
    /// </summary>
    public bool EnableLowTrustScoreAlerts { get; set; } = true;

    /// <summary>
    /// Threshold for low trust score alerts
    /// </summary>
    public float LowTrustScoreThreshold { get; set; } = 0.3f;

    /// <summary>
    /// Maximum number of low trust score users to report
    /// </summary>
    public int LowTrustScoreAlertLimit { get; set; } = 10;
}
