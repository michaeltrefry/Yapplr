using Yapplr.Shared.Models;

namespace Yapplr.Api.Services;

public interface IVideoService
{
    /// <summary>
    /// Save an uploaded video file
    /// </summary>
    /// <param name="file">The uploaded video file</param>
    /// <returns>The filename of the saved video</returns>
    Task<string> SaveVideoAsync(IFormFile? file);

    /// <summary>
    /// Delete a video file
    /// </summary>
    /// <param name="fileName">The filename to delete</param>
    /// <returns>True if deleted successfully</returns>
    bool DeleteVideo(string fileName);

    /// <summary>
    /// Delete a processed video file
    /// </summary>
    /// <param name="fileName">The filename to delete</param>
    /// <returns>True if deleted successfully</returns>
    bool DeleteProcessedVideo(string fileName);

    /// <summary>
    /// Delete a video thumbnail file
    /// </summary>
    /// <param name="fileName">The filename to delete</param>
    /// <returns>True if deleted successfully</returns>
    bool DeleteVideoThumbnail(string fileName);

    /// <summary>
    /// Validate if a file is a valid video (async version)
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <returns>True if valid video file</returns>
    Task<bool> IsValidVideoFileAsync(IFormFile? file);

    /// <summary>
    /// Validate if a file is a valid video (synchronous version - deprecated)
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <returns>True if valid video file</returns>
    [Obsolete("Use IsValidVideoFileAsync instead")]
    bool IsValidVideoFile(IFormFile? file);

    /// <summary>
    /// Get video upload response with metadata
    /// </summary>
    /// <param name="fileName">The saved video filename</param>
    /// <param name="httpContext">HTTP context for URL generation</param>
    /// <returns>Video upload response</returns>
    Task<VideoUploadResponse> GetVideoUploadResponseAsync(string fileName, HttpContext httpContext);
}
