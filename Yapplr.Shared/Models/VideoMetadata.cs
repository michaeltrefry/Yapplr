namespace Yapplr.Shared.Models;

/// <summary>
/// Video metadata from processing
/// </summary>
public record VideoMetadata
{
    public int OriginalWidth { get; init; }
    public int OriginalHeight { get; init; }
    public int ProcessedWidth { get; init; }
    public int ProcessedHeight { get; init; }
    public TimeSpan OriginalDuration { get; init; }
    public TimeSpan ProcessedDuration { get; init; }
    public long OriginalFileSizeBytes { get; init; }
    public long ProcessedFileSizeBytes { get; init; }
    public string OriginalFormat { get; init; } = string.Empty;
    public string ProcessedFormat { get; init; } = string.Empty;
    public double OriginalBitrate { get; init; }
    public double ProcessedBitrate { get; init; }
    public double CompressionRatio { get; init; }

    /// <summary>
    /// Original video rotation in degrees (0, 90, 180, 270)
    /// </summary>
    public int OriginalRotation { get; init; }

    /// <summary>
    /// Processed video rotation in degrees (should be 0 after processing)
    /// </summary>
    public int ProcessedRotation { get; init; }

    /// <summary>
    /// Display width after accounting for rotation
    /// </summary>
    public int DisplayWidth { get; init; }

    /// <summary>
    /// Display height after accounting for rotation
    /// </summary>
    public int DisplayHeight { get; init; }
}