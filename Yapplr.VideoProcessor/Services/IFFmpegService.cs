using Microsoft.Extensions.Logging;
using Yapplr.VideoProcessor.Models;

namespace Yapplr.VideoProcessor.Services;

public interface IFFmpegService
{
    Task<VideoMetadata?> GetVideoMetadataAsync(string inputPath);
    Task<bool> GenerateThumbnailAsync(string inputPath, string outputPath, ThumbnailSettings settings);
    Task<bool> ProcessVideoAsync(string inputPath, string outputPath, VideoQualitySettings quality, IProgress<ProcessingStep>? progress = null);
    Task<bool> OptimizeForWebAsync(string inputPath, string outputPath);
    bool IsFFmpegAvailable();
    bool IsFFprobeAvailable();
}

public class FFmpegService : IFFmpegService
{
    private readonly VideoProcessingOptions _options;
    private readonly ILogger<FFmpegService> _logger;

    public FFmpegService(VideoProcessingOptions options, ILogger<FFmpegService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool IsFFmpegAvailable()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _options.FFmpegPath,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(5000); // 5 second timeout
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg not available at path: {FFmpegPath}", _options.FFmpegPath);
            return false;
        }
    }

    public bool IsFFprobeAvailable()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _options.FFprobePath,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(5000); // 5 second timeout
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFprobe not available at path: {FFprobePath}", _options.FFprobePath);
            return false;
        }
    }

    public async Task<VideoMetadata?> GetVideoMetadataAsync(string inputPath)
    {
        try
        {
            var arguments = $"-v quiet -print_format json -show_format -show_streams \"{inputPath}\"";
            
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _options.FFprobePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _logger.LogInformation("Getting video metadata for: {InputPath}", inputPath);
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFprobe failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return null;
            }

            return ParseFFprobeOutput(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video metadata for: {InputPath}", inputPath);
            return null;
        }
    }

    public async Task<bool> GenerateThumbnailAsync(string inputPath, string outputPath, ThumbnailSettings settings)
    {
        try
        {
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var arguments = $"-i \"{inputPath}\" -ss {settings.TimeOffset} -vframes 1 -vf \"scale={settings.Width}:{settings.Height}:force_original_aspect_ratio=decrease,pad={settings.Width}:{settings.Height}:(ow-iw)/2:(oh-ih)/2\" -q:v {100 - settings.Quality} \"{outputPath}\"";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _options.FFmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _logger.LogInformation("Generating thumbnail: {InputPath} -> {OutputPath}", inputPath, outputPath);
            
            process.Start();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Thumbnail generation failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return false;
            }

            _logger.LogInformation("Thumbnail generated successfully: {OutputPath}", outputPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail: {InputPath} -> {OutputPath}", inputPath, outputPath);
            return false;
        }
    }

    public async Task<bool> ProcessVideoAsync(string inputPath, string outputPath, VideoQualitySettings quality, IProgress<ProcessingStep>? progress = null)
    {
        try
        {
            progress?.Report(ProcessingStep.ProcessingVideo);

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var arguments = BuildFFmpegArguments(inputPath, outputPath, quality);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _options.FFmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _logger.LogInformation("Processing video: {InputPath} -> {OutputPath}", inputPath, outputPath);
            _logger.LogDebug("FFmpeg arguments: {Arguments}", arguments);
            
            process.Start();
            
            // Monitor progress by reading stderr
            var errorOutput = new List<string>();
            while (!process.StandardError.EndOfStream)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    errorOutput.Add(line);
                    // You could parse progress from FFmpeg output here if needed
                }
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = string.Join("\n", errorOutput);
                _logger.LogError("Video processing failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return false;
            }

            progress?.Report(ProcessingStep.Completed);
            _logger.LogInformation("Video processed successfully: {OutputPath}", outputPath);
            return true;
        }
        catch (Exception ex)
        {
            progress?.Report(ProcessingStep.Failed);
            _logger.LogError(ex, "Error processing video: {InputPath} -> {OutputPath}", inputPath, outputPath);
            return false;
        }
    }

    public async Task<bool> OptimizeForWebAsync(string inputPath, string outputPath)
    {
        try
        {
            // Add web optimization flags
            var arguments = $"-i \"{inputPath}\" -c:v libx264 -preset fast -crf 23 -c:a aac -movflags +faststart \"{outputPath}\"";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _options.FFmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _logger.LogInformation("Optimizing video for web: {InputPath} -> {OutputPath}", inputPath, outputPath);
            
            process.Start();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Web optimization failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                return false;
            }

            _logger.LogInformation("Video optimized for web: {OutputPath}", outputPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing video for web: {InputPath} -> {OutputPath}", inputPath, outputPath);
            return false;
        }
    }

    private string BuildFFmpegArguments(string inputPath, string outputPath, VideoQualitySettings quality)
    {
        var args = new List<string>
        {
            $"-i \"{inputPath}\"",
            $"-c:v {quality.VideoCodec}",
            $"-b:v {quality.VideoBitrate}",
            $"-c:a {quality.AudioCodec}",
            $"-b:a {quality.AudioBitrate}",
            $"-r {quality.FrameRate}",
            $"-vf \"scale='min({quality.MaxWidth},iw)':'min({quality.MaxHeight},ih)':force_original_aspect_ratio=decrease\"",
            "-preset fast",
            "-movflags +faststart", // Optimize for web streaming
            $"\"{outputPath}\""
        };

        return string.Join(" ", args);
    }

    private VideoMetadata? ParseFFprobeOutput(string jsonOutput)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(jsonOutput);
            var root = document.RootElement;

            var metadata = new VideoMetadata();

            // Parse format information
            if (root.TryGetProperty("format", out var format))
            {
                if (format.TryGetProperty("duration", out var duration))
                {
                    if (double.TryParse(duration.GetString(), out var durationSeconds))
                    {
                        metadata.DurationSeconds = (int)Math.Round(durationSeconds);
                    }
                }

                if (format.TryGetProperty("format_name", out var formatName))
                {
                    metadata.Format = formatName.GetString() ?? string.Empty;
                }

                if (format.TryGetProperty("size", out var size))
                {
                    if (long.TryParse(size.GetString(), out var sizeBytes))
                    {
                        metadata.SizeBytes = sizeBytes;
                    }
                }

                if (format.TryGetProperty("bit_rate", out var bitRate))
                {
                    if (int.TryParse(bitRate.GetString(), out var bitRateValue))
                    {
                        metadata.Bitrate = bitRateValue;
                    }
                }
            }

            // Parse streams
            if (root.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    if (stream.TryGetProperty("codec_type", out var codecType))
                    {
                        var type = codecType.GetString();
                        
                        if (type == "video")
                        {
                            metadata.HasVideo = true;
                            
                            if (stream.TryGetProperty("width", out var width))
                            {
                                metadata.Width = width.GetInt32();
                            }
                            
                            if (stream.TryGetProperty("height", out var height))
                            {
                                metadata.Height = height.GetInt32();
                                metadata.Resolution = $"{metadata.Width}x{metadata.Height}";
                            }
                            
                            if (stream.TryGetProperty("r_frame_rate", out var frameRate))
                            {
                                var frameRateStr = frameRate.GetString();
                                if (!string.IsNullOrEmpty(frameRateStr) && frameRateStr.Contains('/'))
                                {
                                    var parts = frameRateStr.Split('/');
                                    if (parts.Length == 2 && 
                                        double.TryParse(parts[0], out var numerator) && 
                                        double.TryParse(parts[1], out var denominator) && 
                                        denominator != 0)
                                    {
                                        metadata.FrameRate = numerator / denominator;
                                    }
                                }
                            }
                        }
                        else if (type == "audio")
                        {
                            metadata.HasAudio = true;
                        }
                    }
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing FFprobe output: {Output}", jsonOutput);
            return null;
        }
    }
}
