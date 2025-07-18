namespace Yapplr.Shared.Models;

/// <summary>
/// Video processing configuration
/// </summary>
public record VideoProcessingConfig
{
    public int MaxWidth { get; init; } = 1920;
    public int MaxHeight { get; init; } = 1080;
    public int TargetBitrate { get; init; } = 2000; // kbps
    public string OutputFormat { get; init; } = "mp4";
    public string VideoCodec { get; init; } = "libx264";
    public string AudioCodec { get; init; } = "aac";
    public int ThumbnailWidth { get; init; } = 320;
    public int ThumbnailHeight { get; init; } = 240;
    public double ThumbnailTimeSeconds { get; init; } = 1.0; // Extract thumbnail at 1 second
    public string InputPath { get; init; } = "/app/uploads/videos";
    public string OutputPath { get; init; } = "/app/uploads/processed";
    public string ThumbnailPath { get; init; } = "/app/uploads/thumbnails";
    public bool DeleteOriginalAfterProcessing { get; init; } = true;
}