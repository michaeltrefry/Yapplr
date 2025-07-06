namespace Yapplr.VideoProcessor.Models;

public class VideoProcessingOptions
{
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string TempPath { get; set; } = string.Empty;
    public int MaxConcurrentJobs { get; set; } = 2;
    public int PollIntervalSeconds { get; set; } = 5;
    public string FFmpegPath { get; set; } = "ffmpeg";
    public string FFprobePath { get; set; } = "ffprobe";
    
    public VideoQualitySettings DefaultQuality { get; set; } = new();
    public ThumbnailSettings ThumbnailSettings { get; set; } = new();
    public SupportedFormats SupportedFormats { get; set; } = new();
    public ProcessingLimits Limits { get; set; } = new();
}

public class VideoQualitySettings
{
    public string VideoCodec { get; set; } = "libx264";
    public string AudioCodec { get; set; } = "aac";
    public string VideoBitrate { get; set; } = "1000k";
    public string AudioBitrate { get; set; } = "128k";
    public int MaxWidth { get; set; } = 1280;
    public int MaxHeight { get; set; } = 720;
    public int FrameRate { get; set; } = 30;
}

public class ThumbnailSettings
{
    public int Width { get; set; } = 320;
    public int Height { get; set; } = 180;
    public int Quality { get; set; } = 85;
    public string TimeOffset { get; set; } = "00:00:01";
}

public class SupportedFormats
{
    public string[] Input { get; set; } = Array.Empty<string>();
    public string[] Output { get; set; } = Array.Empty<string>();
}

public class ProcessingLimits
{
    public int MaxDurationSeconds { get; set; } = 300;
    public long MaxFileSizeBytes { get; set; } = 104857600; // 100MB
}

public class VideoMetadata
{
    public int DurationSeconds { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int Bitrate { get; set; }
    public double FrameRate { get; set; }
    public long SizeBytes { get; set; }
    public bool HasAudio { get; set; }
    public bool HasVideo { get; set; }
}

public class ProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputFileName { get; set; }
    public string? ThumbnailFileName { get; set; }
    public VideoMetadata? Metadata { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}

public enum ProcessingStep
{
    Queued,
    Starting,
    AnalyzingInput,
    GeneratingThumbnail,
    ProcessingVideo,
    OptimizingForWeb,
    Finalizing,
    Completed,
    Failed
}
