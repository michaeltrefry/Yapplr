namespace Yapplr.Api.Services;

/// <summary>
/// Compressed notification payload
/// </summary>
public class CompressedNotificationPayload
{
    public string CompressedData { get; set; } = string.Empty;
    public string CompressionMethod { get; set; } = string.Empty;
    public int OriginalSize { get; set; }
    public int CompressedSize { get; set; }
    public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize : 1.0;
}