namespace Yapplr.Api.Common;

/// <summary>
/// Standard error response format
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Details { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}