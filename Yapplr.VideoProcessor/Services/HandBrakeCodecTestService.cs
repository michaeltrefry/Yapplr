using System.Diagnostics;

namespace Yapplr.VideoProcessor.Services;

/// <summary>
/// Service for testing HandBrake CLI installation and codec availability
/// </summary>
public class HandBrakeCodecTestService : ICodecTestService
{
    private readonly ILogger<HandBrakeCodecTestService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _handBrakePath;
    private readonly string _ffmpegPath;

    public HandBrakeCodecTestService(
        ILogger<HandBrakeCodecTestService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _handBrakePath = _configuration["HandBrake:BinaryPath"] ?? "HandBrakeCLI";
        _ffmpegPath = _configuration["FFmpeg:BinaryPath"] ?? "ffmpeg";
    }

    public async Task<CodecTestResult> RunCodecTestsAsync()
    {
        var result = new CodecTestResult();

        try
        {
            _logger.LogInformation("Starting HandBrake and FFmpeg codec compatibility tests");

            // Test HandBrake installation
            result.HandBrakeInstalled = await TestHandBrakeInstallationAsync();
            _logger.LogInformation("HandBrake installation test: {Result}", result.HandBrakeInstalled ? "PASS" : "FAIL");

            // Test FFmpeg installation (still needed for metadata and thumbnails)
            result.FFmpegInstalled = await TestFFmpegInstallationAsync();
            _logger.LogInformation("FFmpeg installation test: {Result}", result.FFmpegInstalled ? "PASS" : "FAIL");

            if (result.HandBrakeInstalled)
            {
                // Test HandBrake encoders
                await TestHandBrakeEncodersAsync(result);
                
                // Test basic HandBrake processing
                result.HandBrakeProcessingWorks = await TestBasicHandBrakeProcessingAsync();
                _logger.LogInformation("HandBrake basic processing test: {Result}", result.HandBrakeProcessingWorks ? "PASS" : "FAIL");
            }

            if (result.FFmpegInstalled)
            {
                // Test FFmpeg codecs (for thumbnail generation)
                await TestFFmpegCodecsAsync(result);
                
                // Test basic FFmpeg processing (for thumbnails)
                result.BasicProcessingWorks = await TestBasicFFmpegProcessingAsync();
                _logger.LogInformation("FFmpeg basic processing test: {Result}", result.BasicProcessingWorks ? "PASS" : "FAIL");
            }

            result.Success = result.HandBrakeInstalled && result.FFmpegInstalled && 
                           result.HandBrakeProcessingWorks && result.BasicProcessingWorks;

            _logger.LogInformation("Overall codec test result: {Result}", result.Success ? "PASS" : "FAIL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Codec test failed with exception");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task<bool> TestHandBrakeInstallationAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _handBrakePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HandBrake installation test failed");
            return false;
        }
    }

    private async Task<bool> TestFFmpegInstallationAsync()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FFmpeg installation test failed");
            return false;
        }
    }

    private async Task TestHandBrakeEncodersAsync(CodecTestResult result)
    {
        var encodersToTest = new[] { "x264", "x265", "VP8", "VP9", "svt_av1" };

        foreach (var encoder in encodersToTest)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _handBrakePath,
                    Arguments = $"--encoder-list",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                var isAvailable = process.ExitCode == 0 && output.Contains(encoder, StringComparison.OrdinalIgnoreCase);
                result.HandBrakeEncoders[encoder] = isAvailable;
                
                _logger.LogDebug("HandBrake encoder {Encoder}: {Available}", encoder, isAvailable ? "Available" : "Not Available");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to test HandBrake encoder {Encoder}", encoder);
                result.HandBrakeEncoders[encoder] = false;
            }
        }
    }

    private async Task TestFFmpegCodecsAsync(CodecTestResult result)
    {
        // Test video codecs (for fallback scenarios)
        var videoCodecs = new[] { "libx264", "libx265", "libvpx", "libvpx-vp9" };
        foreach (var codec in videoCodecs)
        {
            result.VideoCodecs[codec] = await TestFFmpegCodecAsync(codec, "encoders");
        }

        // Test audio codecs
        var audioCodecs = new[] { "aac", "libmp3lame", "libvorbis", "libopus" };
        foreach (var codec in audioCodecs)
        {
            result.AudioCodecs[codec] = await TestFFmpegCodecAsync(codec, "encoders");
        }

        // Test input formats
        var inputFormats = new[] { "mp4", "avi", "mov", "mkv", "webm", "flv" };
        foreach (var format in inputFormats)
        {
            result.InputFormats[format] = await TestFFmpegFormatAsync(format);
        }
    }

    private async Task<bool> TestFFmpegCodecAsync(string codec, string type)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-{type}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Contains(codec, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to test FFmpeg codec {Codec}", codec);
            return false;
        }
    }

    private async Task<bool> TestFFmpegFormatAsync(string format)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-formats",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Contains($" {format} ", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to test FFmpeg format {Format}", format);
            return false;
        }
    }

    private async Task<bool> TestBasicHandBrakeProcessingAsync()
    {
        var inputFile = Path.Combine(Path.GetTempPath(), "handbrake_test_input.mp4");
        var outputFile = Path.Combine(Path.GetTempPath(), "handbrake_test_output.mp4");

        try
        {
            // Create a simple test video using FFmpeg
            await CreateTestVideoAsync(inputFile);

            if (!File.Exists(inputFile))
                return false;

            // Process the test video with HandBrake
            var processInfo = new ProcessStartInfo
            {
                FileName = _handBrakePath,
                Arguments = $"-i \"{inputFile}\" -o \"{outputFile}\" -e x264 -q 22 -w 160 -l 120",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && File.Exists(outputFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Basic HandBrake processing test failed");
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

    private async Task<bool> TestBasicFFmpegProcessingAsync()
    {
        var inputFile = Path.Combine(Path.GetTempPath(), "ffmpeg_test_input.mp4");
        var outputFile = Path.Combine(Path.GetTempPath(), "ffmpeg_test_thumb.jpg");

        try
        {
            // Create a simple test video
            await CreateTestVideoAsync(inputFile);

            if (!File.Exists(inputFile))
                return false;

            // Generate thumbnail using FFmpeg
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-i \"{inputFile}\" -vf scale=80:60 -vframes 1 \"{outputFile}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && File.Exists(outputFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Basic FFmpeg processing test failed");
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

    private async Task CreateTestVideoAsync(string outputPath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-f lavfi -i testsrc=duration=1:size=320x240:rate=1 -c:v libx264 -t 1 \"{outputPath}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create test video");
        }
    }
}
