using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.VideoProcessor.Models;

namespace Yapplr.VideoProcessor.Services;

public class VideoProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly VideoProcessingOptions _options;
    private readonly ILogger<VideoProcessingWorker> _logger;
    private readonly SemaphoreSlim _concurrencySemaphore;

    public VideoProcessingWorker(
        IServiceProvider serviceProvider,
        VideoProcessingOptions options,
        ILogger<VideoProcessingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(_options.MaxConcurrentJobs, _options.MaxConcurrentJobs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Video Processing Worker started with {MaxJobs} max concurrent jobs", _options.MaxConcurrentJobs);

        // Verify FFmpeg is available
        using (var scope = _serviceProvider.CreateScope())
        {
            var ffmpegService = scope.ServiceProvider.GetRequiredService<IFFmpegService>();
            if (!ffmpegService.IsFFmpegAvailable())
            {
                _logger.LogError("FFmpeg is not available. Please install FFmpeg and ensure it's in the PATH or configure the correct path in appsettings.json");
                return;
            }

            if (!ffmpegService.IsFFprobeAvailable())
            {
                _logger.LogError("FFprobe is not available. Please install FFmpeg (which includes FFprobe) and ensure it's in the PATH or configure the correct path in appsettings.json");
                return;
            }

            _logger.LogInformation("FFmpeg and FFprobe are available and ready for video processing");
        }

        // Ensure required directories exist
        EnsureDirectoriesExist();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Video processing worker is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in video processing worker main loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Video Processing Worker stopped");
    }

    private async Task ProcessPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();

        // Get pending jobs
        var pendingJobs = await dbContext.VideoProcessingJobs
            .Where(j => j.Status == VideoProcessingStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .Take(_options.MaxConcurrentJobs * 2) // Get a few extra to keep the pipeline full
            .ToListAsync(cancellationToken);

        if (pendingJobs.Count == 0)
        {
            return; // No jobs to process
        }

        _logger.LogDebug("Found {JobCount} pending video processing jobs", pendingJobs.Count);

        // Process jobs concurrently
        var tasks = pendingJobs.Select(job => ProcessJobAsync(job, cancellationToken)).ToArray();
        
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video jobs batch");
        }
    }

    private async Task ProcessJobAsync(VideoProcessingJob job, CancellationToken cancellationToken)
    {
        // Wait for available slot
        await _concurrencySemaphore.WaitAsync(cancellationToken);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var processingService = scope.ServiceProvider.GetRequiredService<IVideoProcessingService>();

            _logger.LogInformation("Starting processing for job {JobId}: {FileName}", job.Id, job.FileName);

            var result = await processingService.ProcessVideoAsync(job, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Successfully processed job {JobId} in {ProcessingTime}", 
                    job.Id, result.ProcessingTime);
            }
            else
            {
                _logger.LogError("Failed to process job {JobId}: {Error}", job.Id, result.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Job {JobId} processing was cancelled", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing job {JobId}", job.Id);
            
            // Update job status to failed
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
                
                var jobEntity = await dbContext.VideoProcessingJobs.FindAsync(job.Id);
                if (jobEntity != null)
                {
                    jobEntity.Status = VideoProcessingStatus.Failed;
                    jobEntity.ErrorMessage = ex.Message;
                    jobEntity.CompletedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update job {JobId} status to failed", job.Id);
            }
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    private void EnsureDirectoriesExist()
    {
        var directories = new[]
        {
            _options.InputPath,
            _options.OutputPath,
            _options.ThumbnailPath,
            _options.TempPath
        };

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation("Created directory: {Directory}", directory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create directory: {Directory}", directory);
                }
            }
        }
    }

    public override void Dispose()
    {
        _concurrencySemaphore?.Dispose();
        base.Dispose();
    }
}
