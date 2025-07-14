using MassTransit;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Models.Analytics;
using Yapplr.Shared.Messages;
using Yapplr.Shared.Models;

namespace Yapplr.Api.CQRS.Consumers;

/// <summary>
/// Consumer for video processing completion messages
/// </summary>
public class VideoProcessingCompletedConsumer : IConsumer<VideoProcessingCompleted>
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<VideoProcessingCompletedConsumer> _logger;
    private readonly INotificationService _notificationService;
    private readonly IAnalyticsService _analyticsService;

    public VideoProcessingCompletedConsumer(
        YapplrDbContext context,
        ILogger<VideoProcessingCompletedConsumer> logger,
        INotificationService notificationService,
        IAnalyticsService analyticsService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
        _analyticsService = analyticsService;
    }

    public async Task Consume(ConsumeContext<VideoProcessingCompleted> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received video processing completed message for Post {PostId}", message.PostId);

        try
        {
            var post = await _context.Posts
                .Include(p => p.PostMedia)
                .FirstOrDefaultAsync(p => p.Id == message.PostId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for video processing completion", message.PostId);
                return;
            }

            // Find or create the video media record
            var videoMedia = post.PostMedia.FirstOrDefault(m => m.MediaType == MediaType.Video);
            if (videoMedia == null)
            {
                _logger.LogWarning("No video media record found for Post {PostId}", message.PostId);
                return;
            }

            // Update video media with processed information
            videoMedia.ProcessedVideoFileName = message.ProcessedVideoFileName;
            videoMedia.VideoThumbnailFileName = message.ThumbnailFileName;
            videoMedia.VideoProcessingStatus = VideoProcessingStatus.Completed;
            videoMedia.VideoProcessingCompletedAt = message.CompletedAt;
            videoMedia.VideoProcessingError = null; // Clear any previous errors
            videoMedia.UpdatedAt = DateTime.UtcNow;

            // Store video metadata if available
            if (message.Metadata != null)
            {
                videoMedia.VideoWidth = message.Metadata.ProcessedWidth;
                videoMedia.VideoHeight = message.Metadata.ProcessedHeight;
                videoMedia.VideoDuration = message.Metadata.ProcessedDuration;
                videoMedia.VideoFileSizeBytes = message.Metadata.ProcessedFileSizeBytes;
                videoMedia.VideoFormat = message.Metadata.ProcessedFormat;
                videoMedia.VideoBitrate = message.Metadata.ProcessedBitrate;
                videoMedia.VideoCompressionRatio = message.Metadata.CompressionRatio;

                // Store original metadata if available
                videoMedia.OriginalVideoWidth = message.Metadata.OriginalWidth;
                videoMedia.OriginalVideoHeight = message.Metadata.OriginalHeight;
                videoMedia.OriginalVideoDuration = message.Metadata.OriginalDuration;
                videoMedia.OriginalVideoFileSizeBytes = message.Metadata.OriginalFileSizeBytes;
                videoMedia.OriginalVideoFormat = message.Metadata.OriginalFormat;
                videoMedia.OriginalVideoBitrate = message.Metadata.OriginalBitrate;
            }

            // Make the post visible at the user's selected privacy level
            post.IsHiddenDuringVideoProcessing = false;

            await _context.SaveChangesAsync();

            // Track video processing performance metrics
            await _analyticsService.RecordPerformanceMetricAsync(
                metricType: MetricType.VideoProcessingTime,
                value: message.ProcessingDuration.TotalMilliseconds,
                unit: "ms",
                source: "VideoProcessor",
                operation: "ProcessVideo",
                success: true,
                userId: message.UserId);

            // Track video processing completion activity
            await _analyticsService.TrackUserActivityAsync(
                userId: message.UserId,
                activityType: ActivityType.VideoWatched, // Using closest available activity type
                targetEntityType: "post",
                targetEntityId: message.PostId,
                metadata: message.Metadata != null ?
                    $"{{\"duration\":\"{message.Metadata.ProcessedDuration}\",\"resolution\":\"{message.Metadata.ProcessedWidth}x{message.Metadata.ProcessedHeight}\",\"format\":\"{message.Metadata.ProcessedFormat}\"}}" :
                    null);

            _logger.LogInformation("Updated Post {PostId} with processed video: {ProcessedVideoFileName}, thumbnail: {ThumbnailFileName}",
                message.PostId, message.ProcessedVideoFileName, message.ThumbnailFileName);

            // Send notification to the user that their video is ready
            await _notificationService.CreateVideoProcessingCompletedNotificationAsync(message.UserId, message.PostId);

            _logger.LogInformation("Sent video processing completion notification to user {UserId} for post {PostId}",
                message.UserId, message.PostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video completion message for Post {PostId}", message.PostId);
            throw; // Re-throw to trigger retry mechanism
        }
    }
}