using System.Diagnostics;

namespace Yapplr.VideoProcessor.Services;

public interface ICodecTestService
{
    Task<CodecTestResult> RunCodecTestsAsync();
}

public class CodecTestService : ICodecTestService
{
    private readonly ILogger<CodecTestService> _logger;
    private readonly IConfiguration _configuration;

    public CodecTestService(ILogger<CodecTestService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<CodecTestResult> RunCodecTestsAsync()
    {
        var result = new CodecTestResult();
        var ffmpegPath = _configuration["FFmpeg:BinaryPath"] ?? "ffmpeg";

        try
        {
            _logger.LogInformation("Starting codec compatibility tests");

            // Test FFmpeg installation
            result.FFmpegInstalled = await TestFFmpegInstallationAsync(ffmpegPath);
            if (!result.FFmpegInstalled)
            {
                result.ErrorMessage = "FFmpeg is not installed or not accessible";
                return result;
            }

            // Test video codecs
            result.VideoCodecs = await TestCodecsAsync(ffmpegPath, "encoders", new[]
            {
                "libx264", "libx265", "libvpx-vp9", "libvpx"
            });

            // Test audio codecs
            result.AudioCodecs = await TestCodecsAsync(ffmpegPath, "encoders", new[]
            {
                "aac", "libmp3lame", "libvorbis", "libopus"
            });

            // Test input formats
            result.InputFormats = await TestFormatsAsync(ffmpegPath, new[]
            {
                "mp4", "avi", "mov", "mkv", "webm", "flv", "wmv", "m4v", "3gp", "ogv"
            });

            // Test basic video processing
            result.BasicProcessingWorks = await TestBasicVideoProcessingAsync(ffmpegPath);

            result.Success = result.FFmpegInstalled && 
                           result.VideoCodecs.Any(c => c.Value) && 
                           result.AudioCodecs.Any(c => c.Value) &&
                           result.BasicProcessingWorks;

            _logger.LogInformation("Codec compatibility tests completed. Success: {Success}", result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running codec tests");
            result.ErrorMessage = ex.Message;
            result.Success = false;
        }

        return result;
    }

    private async Task<bool> TestFFmpegInstallationAsync(string ffmpegPath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<Dictionary<string, bool>> TestCodecsAsync(string ffmpegPath, string listType, string[] codecs)
    {
        var result = new Dictionary<string, bool>();

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-{listType}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            foreach (var codec in codecs)
            {
                result[codec] = output.Contains(codec);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to test codecs");
            foreach (var codec in codecs)
            {
                result[codec] = false;
            }
        }

        return result;
    }

    private async Task<Dictionary<string, bool>> TestFormatsAsync(string ffmpegPath, string[] formats)
    {
        var result = new Dictionary<string, bool>();

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-formats",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            foreach (var format in formats)
            {
                result[format] = output.Contains($" {format} ");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to test formats");
            foreach (var format in formats)
            {
                result[format] = false;
            }
        }

        return result;
    }

    private async Task<bool> TestBasicVideoProcessingAsync(string ffmpegPath)
    {
        var tempDir = Path.GetTempPath();
        var inputFile = Path.Combine(tempDir, $"test_input_{Guid.NewGuid()}.mp4");
        var outputFile = Path.Combine(tempDir, $"test_output_{Guid.NewGuid()}.mp4");

        try
        {
            // Create a test video
            var createProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-f lavfi -i testsrc=duration=1:size=320x240:rate=1 -c:v libx264 -t 1 \"{inputFile}\" -y",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            createProcess.Start();
            await createProcess.WaitForExitAsync();

            if (createProcess.ExitCode != 0 || !File.Exists(inputFile))
            {
                return false;
            }

            // Process the test video
            var processVideo = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{inputFile}\" -c:v libx264 -vf scale=160:120 \"{outputFile}\" -y",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            processVideo.Start();
            await processVideo.WaitForExitAsync();

            return processVideo.ExitCode == 0 && File.Exists(outputFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Basic video processing test failed");
            return false;
        }
        finally
        {
            // Cleanup
            try
            {
                if (File.Exists(inputFile)) File.Delete(inputFile);
                if (File.Exists(outputFile)) File.Delete(outputFile);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

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
