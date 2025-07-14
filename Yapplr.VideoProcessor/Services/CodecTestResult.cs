namespace Yapplr.VideoProcessor.Services;

public class CodecTestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool FFmpegInstalled { get; set; }
    public Dictionary<string, bool> VideoCodecs { get; set; } = new();
    public Dictionary<string, bool> AudioCodecs { get; set; } = new();
    public Dictionary<string, bool> InputFormats { get; set; } = new();
    public bool BasicProcessingWorks { get; set; }
}