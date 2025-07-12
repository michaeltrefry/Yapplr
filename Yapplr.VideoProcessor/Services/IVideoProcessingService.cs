using Yapplr.Shared.Models;

namespace Yapplr.VideoProcessor.Services;

public interface IVideoProcessingService
{
    /// <summary>
    /// Process a video file for web streaming optimization
    /// </summary>
    /// <param name="inputPath">Path to the original video file</param>
    /// <param name="outputPath">Path where the processed video should be saved</param>
    /// <param name="thumbnailPath">Path where the thumbnail should be saved</param>
    /// <param name="config">Video processing configuration</param>
    /// <returns>Video processing result</returns>
    Task<VideoProcessingResult> ProcessVideoAsync(
        string inputPath, 
        string outputPath, 
        string thumbnailPath, 
        VideoProcessingConfig config);

    /// <summary>
    /// Get video metadata without processing
    /// </summary>
    /// <param name="videoPath">Path to the video file</param>
    /// <returns>Video metadata</returns>
    Task<VideoMetadata?> GetVideoMetadataAsync(string videoPath);

    /// <summary>
    /// Validate if a video file is processable
    /// </summary>
    /// <param name="videoPath">Path to the video file</param>
    /// <param name="maxFileSizeBytes">Maximum allowed file size</param>
    /// <param name="maxDurationSeconds">Maximum allowed duration</param>
    /// <returns>True if video is valid for processing</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateVideoAsync(
        string videoPath, 
        long maxFileSizeBytes, 
        int maxDurationSeconds);
}
