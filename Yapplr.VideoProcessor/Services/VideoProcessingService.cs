using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.VideoProcessor.Models;

namespace Yapplr.VideoProcessor.Services;

public interface IVideoProcessingService
{
    Task<ProcessingResult> ProcessVideoAsync(VideoProcessingJob job, CancellationToken cancellationToken = default);
}

public class VideoProcessingService : IVideoProcessingService
{
    private readonly IFFmpegService _ffmpegService;
    private readonly VideoProcessingOptions _options;
    private readonly YapplrDbContext _dbContext;
    private readonly ILogger<VideoProcessingService> _logger;

    public VideoProcessingService(
        IFFmpegService ffmpegService,
        VideoProcessingOptions options,
        YapplrDbContext dbContext,
        ILogger<VideoProcessingService> logger)
    {
        _ffmpegService = ffmpegService;
        _options = options;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ProcessingResult> ProcessVideoAsync(VideoProcessingJob job, CancellationToken cancellationToken = default)
    {
        var result = new ProcessingResult();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting video processing for job {JobId}: {FileName}", job.Id, job.FileName);

            // Update job status to processing
            await UpdateJobStatusAsync(job.Id, VideoProcessingStatus.Processing, ProcessingStep.Starting);

            // Validate input file
            var inputPath = Path.Combine(_options.InputPath, job.FileName);
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException($"Input video file not found: {inputPath}");
            }

            // Create temp directory for processing
            var tempDir = Path.Combine(_options.TempPath, $"job_{job.Id}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Step 1: Analyze input video
                await UpdateJobStatusAsync(job.Id, VideoProcessingStatus.Processing, ProcessingStep.AnalyzingInput);
                var metadata = await _ffmpegService.GetVideoMetadataAsync(inputPath);
                
                if (metadata == null)
                {
                    throw new InvalidOperationException("Failed to analyze input video metadata");
                }

                _logger.LogInformation("Video metadata - Duration: {Duration}s, Resolution: {Resolution}, Size: {Size} bytes", 
                    metadata.DurationSeconds, metadata.Resolution, metadata.SizeBytes);

                // Validate video constraints
                if (metadata.DurationSeconds > _options.Limits.MaxDurationSeconds)
                {
                    throw new InvalidOperationException($"Video duration ({metadata.DurationSeconds}s) exceeds maximum allowed ({_options.Limits.MaxDurationSeconds}s)");
                }

                if (metadata.SizeBytes > _options.Limits.MaxFileSizeBytes)
                {
                    throw new InvalidOperationException($"Video size ({metadata.SizeBytes} bytes) exceeds maximum allowed ({_options.Limits.MaxFileSizeBytes} bytes)");
                }

                // Step 2: Generate thumbnail
                await UpdateJobStatusAsync(job.Id, VideoProcessingStatus.Processing, ProcessingStep.GeneratingThumbnail);
                var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(job.FileName)}_thumb.jpg";
                var thumbnailPath = Path.Combine(_options.ThumbnailPath, thumbnailFileName);
                
                var thumbnailSuccess = await _ffmpegService.GenerateThumbnailAsync(inputPath, thumbnailPath, _options.ThumbnailSettings);
                if (!thumbnailSuccess)
                {
                    _logger.LogWarning("Failed to generate thumbnail for job {JobId}, continuing without thumbnail", job.Id);
                    thumbnailFileName = null;
                }

                // Step 3: Process video (if needed)
                string processedFileName;
                string outputPath;

                // Check if we need to process the video or if we can use the original
                if (ShouldProcessVideo(metadata, job.FileName))
                {
                    await UpdateJobStatusAsync(job.Id, VideoProcessingStatus.Processing, ProcessingStep.ProcessingVideo);
                    
                    processedFileName = $"{Path.GetFileNameWithoutExtension(job.FileName)}_processed.mp4";
                    outputPath = Path.Combine(tempDir, processedFileName);

                    var progress = new Progress<ProcessingStep>(step => 
                    {
                        _ = Task.Run(async () => await UpdateJobStatusAsync(job.Id, VideoProcessingStatus.Processing, step));
                    });

                    var processSuccess = await _ffmpegService.ProcessVideoAsync(inputPath, outputPath, _options.DefaultQuality, progress);
                    if (!processSuccess)
                    {
                        throw new InvalidOperationException("Video processing failed");
                    }

                    // Step 4: Optimize for web
                    await UpdateJobStatusAsync(job.Id, VideoProcessingStatus.Processing, ProcessingStep.OptimizingForWeb);
                    var optimizedPath = Path.Combine(_options.OutputPath, processedFileName);
                    var optimizeSuccess = await _ffmpegService.OptimizeForWebAsync(outputPath, optimizedPath);
                    
                    if (!optimizeSuccess)
                    {
                        // If optimization fails, use the processed version
                        _logger.LogWarning("Web optimization failed for job {JobId}, using processed version", job.Id);
                        File.Move(outputPath, optimizedPath);
                    }

                    // Get final metadata
                    metadata = await _ffmpegService.GetVideoMetadataAsync(optimizedPath) ?? metadata;
                }
                else
                {
                    // Use original file
                    processedFileName = job.FileName;
                    _logger.LogInformation("Video {FileName} doesn't need processing, using original", job.FileName);
                }

                // Step 5: Update database with results
                await UpdateJobStatusAsync(job.Id, VideoProcessingStatus.Processing, ProcessingStep.Finalizing);
                await UpdateJobWithResultsAsync(job.Id, processedFileName, thumbnailFileName, metadata);

                // Step 6: Update related Post or Message
                await UpdateRelatedEntityAsync(job, processedFileName, thumbnailFileName, metadata);

                result.Success = true;
                result.OutputFileName = processedFileName;
                result.ThumbnailFileName = thumbnailFileName;
                result.Metadata = metadata;
                result.ProcessingTime = DateTime.UtcNow - startTime;

                await UpdateJobStatusAsync(job.Id, VideoProcessingStatus.Completed, ProcessingStep.Completed);
                _logger.LogInformation("Video processing completed for job {JobId} in {ProcessingTime}", 
                    job.Id, result.ProcessingTime);
            }
            finally
            {
                // Cleanup temp directory
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cleanup temp directory: {TempDir}", tempDir);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ProcessingTime = DateTime.UtcNow - startTime;

            _logger.LogError(ex, "Video processing failed for job {JobId}: {Error}", job.Id, ex.Message);
            await UpdateJobStatusAsync(job.Id, VideoProcessingStatus.Failed, ProcessingStep.Failed, ex.Message);
        }

        return result;
    }

    private bool ShouldProcessVideo(VideoMetadata metadata, string fileName)
    {
        // Check if video needs processing based on format, quality, etc.
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        // Always process if not MP4
        if (extension != ".mp4")
        {
            return true;
        }

        // Process if resolution is too high
        if (metadata.Width > _options.DefaultQuality.MaxWidth || 
            metadata.Height > _options.DefaultQuality.MaxHeight)
        {
            return true;
        }

        // Process if bitrate is too high (rough estimate)
        var targetBitrate = int.Parse(_options.DefaultQuality.VideoBitrate.Replace("k", "")) * 1000;
        if (metadata.Bitrate > targetBitrate * 1.5) // 50% tolerance
        {
            return true;
        }

        return false;
    }

    private async Task UpdateJobStatusAsync(int jobId, VideoProcessingStatus status, ProcessingStep step, string? errorMessage = null)
    {
        try
        {
            var job = await _dbContext.VideoProcessingJobs.FindAsync(jobId);
            if (job != null)
            {
                job.Status = status;
                job.ErrorMessage = errorMessage;
                
                if (status == VideoProcessingStatus.Processing && job.StartedAt == null)
                {
                    job.StartedAt = DateTime.UtcNow;
                }
                else if (status == VideoProcessingStatus.Completed || status == VideoProcessingStatus.Failed)
                {
                    job.CompletedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("Updated job {JobId} status to {Status} - {Step}", jobId, status, step);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job status for job {JobId}", jobId);
        }
    }

    private async Task UpdateJobWithResultsAsync(int jobId, string processedFileName, string? thumbnailFileName, VideoMetadata metadata)
    {
        try
        {
            var job = await _dbContext.VideoProcessingJobs.FindAsync(jobId);
            if (job != null)
            {
                job.ProcessedFileName = processedFileName;
                job.ThumbnailFileName = thumbnailFileName;
                job.DurationSeconds = metadata.DurationSeconds;
                job.ProcessedSizeBytes = metadata.SizeBytes;
                job.ProcessedFormat = metadata.Format;
                job.Resolution = metadata.Resolution;
                job.Bitrate = metadata.Bitrate;
                job.FrameRate = metadata.FrameRate;

                await _dbContext.SaveChangesAsync();
                _logger.LogDebug("Updated job {JobId} with processing results", jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job results for job {JobId}", jobId);
        }
    }

    private async Task UpdateRelatedEntityAsync(VideoProcessingJob job, string processedFileName, string? thumbnailFileName, VideoMetadata metadata)
    {
        try
        {
            if (job.PostId.HasValue)
            {
                var post = await _dbContext.Posts.FindAsync(job.PostId.Value);
                if (post != null)
                {
                    post.VideoFileName = processedFileName;
                    post.VideoThumbnailFileName = thumbnailFileName;
                    post.VideoDurationSeconds = metadata.DurationSeconds;
                    post.VideoSizeBytes = metadata.SizeBytes;
                    post.VideoFormat = metadata.Format;
                    post.VideoProcessingStatus = VideoProcessingStatus.Completed;
                    
                    await _dbContext.SaveChangesAsync();
                    _logger.LogDebug("Updated post {PostId} with video processing results", job.PostId.Value);
                }
            }
            else if (job.MessageId.HasValue)
            {
                var message = await _dbContext.Messages.FindAsync(job.MessageId.Value);
                if (message != null)
                {
                    message.VideoFileName = processedFileName;
                    message.VideoThumbnailFileName = thumbnailFileName;
                    message.VideoDurationSeconds = metadata.DurationSeconds;
                    message.VideoSizeBytes = metadata.SizeBytes;
                    message.VideoFormat = metadata.Format;
                    message.VideoProcessingStatus = VideoProcessingStatus.Completed;
                    
                    await _dbContext.SaveChangesAsync();
                    _logger.LogDebug("Updated message {MessageId} with video processing results", job.MessageId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update related entity for job {JobId}", job.Id);
        }
    }
}
