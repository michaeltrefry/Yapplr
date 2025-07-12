namespace Yapplr.Api.Configuration;

/// <summary>
/// Configuration for file uploads
/// </summary>
public class UploadsConfiguration
{
    public const string SectionName = "Uploads";

    /// <summary>
    /// Base path for uploads (relative to application root)
    /// </summary>
    public string BasePath { get; set; } = "../uploads";

    /// <summary>
    /// Videos subdirectory name
    /// </summary>
    public string VideosPath { get; set; } = "videos";

    /// <summary>
    /// Processed videos subdirectory name
    /// </summary>
    public string ProcessedPath { get; set; } = "processed";

    /// <summary>
    /// Thumbnails subdirectory name
    /// </summary>
    public string ThumbnailsPath { get; set; } = "thumbnails";

    /// <summary>
    /// Get full path for videos directory
    /// </summary>
    public string GetVideosFullPath() => Path.Combine(BasePath, VideosPath);

    /// <summary>
    /// Get full path for processed videos directory
    /// </summary>
    public string GetProcessedFullPath() => Path.Combine(BasePath, ProcessedPath);

    /// <summary>
    /// Get full path for thumbnails directory
    /// </summary>
    public string GetThumbnailsFullPath() => Path.Combine(BasePath, ThumbnailsPath);
}
