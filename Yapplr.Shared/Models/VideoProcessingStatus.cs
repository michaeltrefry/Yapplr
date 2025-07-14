namespace Yapplr.Shared.Models;

/// <summary>
/// Represents the processing status of a video
/// </summary>
public enum VideoProcessingStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}