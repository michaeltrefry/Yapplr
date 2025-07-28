using Yapplr.Shared.Models;

namespace Yapplr.VideoProcessor.Services;

/// <summary>
/// Service for generating video thumbnails
/// </summary>
public interface IThumbnailGenerationService
{
    /// <summary>
    /// Generate a thumbnail from a video file
    /// </summary>
    /// <param name="videoPath">Path to the video file</param>
    /// <param name="thumbnailPath">Path where the thumbnail should be saved</param>
    /// <param name="config">Video processing configuration containing thumbnail settings</param>
    /// <param name="videoMetadata">Video metadata for dimension calculations</param>
    /// <returns>Task representing the thumbnail generation operation</returns>
    Task GenerateThumbnailAsync(string videoPath, string thumbnailPath, VideoProcessingConfig config, VideoMetadata videoMetadata);
}
