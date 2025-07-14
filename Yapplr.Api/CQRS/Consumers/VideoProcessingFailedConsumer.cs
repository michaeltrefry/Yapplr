using MassTransit;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Shared.Messages;
using Yapplr.Shared.Models;

namespace Yapplr.Api.CQRS.Consumers;

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
            var post = await _context.Posts
                .Include(p => p.PostMedia)
                .FirstOrDefaultAsync(p => p.Id == message.PostId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for video processing failure", message.PostId);
                return;
            }

            // Find the video media record
            var videoMedia = post.PostMedia.FirstOrDefault(m => m.MediaType == MediaType.Video);
            if (videoMedia == null)
            {
                _logger.LogWarning("No video media record found for Post {PostId}", message.PostId);
                return;
            }

            // Update video media with failure information
            videoMedia.VideoProcessingStatus = VideoProcessingStatus.Failed;
            videoMedia.VideoProcessingError = message.ErrorMessage?.Length > 500
                ? message.ErrorMessage.Substring(0, 497) + "..."
                : message.ErrorMessage;
            videoMedia.VideoProcessingCompletedAt = message.FailedAt;
            videoMedia.UpdatedAt = DateTime.UtcNow;

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