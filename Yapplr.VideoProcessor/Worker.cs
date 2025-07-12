namespace Yapplr.VideoProcessor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Video Processing Worker started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Video Processing Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(60000, stoppingToken); // Check every minute
        }

        _logger.LogInformation("Video Processing Worker stopped at: {time}", DateTimeOffset.Now);
    }
}
