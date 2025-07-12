using MassTransit;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
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

    public VideoProcessingCompletedConsumer(YapplrDbContext context, ILogger<VideoProcessingCompletedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VideoProcessingCompleted> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Received video processing completed message for Post {PostId}", message.PostId);

        try
        {
            var post = await _context.Posts.FindAsync(message.PostId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for video processing completion", message.PostId);
                return;
            }

            // Update post with processed video information
            post.ProcessedVideoFileName = message.ProcessedVideoFileName;
            post.VideoThumbnailFileName = message.ThumbnailFileName;
            post.VideoProcessingStatus = VideoProcessingStatus.Completed;
            post.VideoProcessingCompletedAt = message.CompletedAt;
            post.VideoProcessingError = null; // Clear any previous errors

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated Post {PostId} with processed video: {ProcessedVideoFileName}, thumbnail: {ThumbnailFileName}", 
                message.PostId, message.ProcessedVideoFileName, message.ThumbnailFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video completion message for Post {PostId}", message.PostId);
            throw; // Re-throw to trigger retry mechanism
        }
    }
}

/// <summary>
/// Consumer for video processing failure messages
/// </summary>
public class VideoProcessingFailedConsumer : IConsumer<VideoProcessingFailed>
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<VideoProcessingFailedConsumer> _logger;

    public VideoProcessingFailedConsumer(YapplrDbContext context, ILogger<VideoProcessingFailedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VideoProcessingFailed> context)
    {
        var message = context.Message;
        
        _logger.LogWarning("Received video processing failed message for Post {PostId}: {ErrorMessage}", 
            message.PostId, message.ErrorMessage);

        try
        {
            var post = await _context.Posts.FindAsync(message.PostId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for video processing failure", message.PostId);
                return;
            }

            // Update post with failure information
            post.VideoProcessingStatus = VideoProcessingStatus.Failed;
            post.VideoProcessingError = message.ErrorMessage?.Length > 500
                ? message.ErrorMessage.Substring(0, 497) + "..."
                : message.ErrorMessage;
            post.VideoProcessingCompletedAt = message.FailedAt;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated Post {PostId} with video processing failure: {ErrorMessage}", 
                message.PostId, message.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video failure message for Post {PostId}", message.PostId);
            throw; // Re-throw to trigger retry mechanism
        }
    }
}
