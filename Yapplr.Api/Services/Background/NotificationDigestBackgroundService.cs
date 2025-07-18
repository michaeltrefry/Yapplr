namespace Yapplr.Api.Services.Background;

/// <summary>
/// Background service to process notification digest emails at scheduled intervals
/// </summary>
public class NotificationDigestBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationDigestBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // Check every 30 minutes

    public NotificationDigestBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationDigestBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Digest Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDigestEmails();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing digest emails");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Notification Digest Background Service stopped");
    }

    private async Task ProcessDigestEmails()
    {
        using var scope = _serviceProvider.CreateScope();
        var digestService = scope.ServiceProvider.GetRequiredService<INotificationDigestService>();

        var currentTime = DateTime.UtcNow;
        var currentHour = currentTime.Hour;
        var currentMinute = currentTime.Minute;

        // Only process at the top of the hour (within 30 minutes of hour start)
        if (currentMinute > 30)
            return;

        _logger.LogDebug("Processing digest emails at {CurrentTime}", currentTime);

        // Process different digest frequencies
        var digestFrequencies = new[]
        {
            1,   // Hourly
            6,   // Every 6 hours
            12,  // Twice daily
            24,  // Daily
            168  // Weekly
        };

        foreach (var frequency in digestFrequencies)
        {
            if (ShouldProcessFrequency(frequency, currentHour))
            {
                try
                {
                    await digestService.ProcessDigestEmailsAsync(frequency);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process {Frequency}h digest emails", frequency);
                }
            }
        }
    }

    private static bool ShouldProcessFrequency(int frequencyHours, int currentHour)
    {
        return frequencyHours switch
        {
            1 => true, // Process hourly digests every hour
            6 => currentHour % 6 == 0, // Process every 6 hours (0, 6, 12, 18)
            12 => currentHour % 12 == 0, // Process twice daily (0, 12)
            24 => currentHour == 8, // Process daily at 8 AM UTC
            168 => currentHour == 8 && DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday, // Weekly on Monday at 8 AM UTC
            _ => false
        };
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Digest Background Service is stopping");
        await base.StopAsync(stoppingToken);
    }
}
