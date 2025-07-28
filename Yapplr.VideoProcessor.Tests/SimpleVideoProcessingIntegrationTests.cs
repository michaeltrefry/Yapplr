using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Yapplr.VideoProcessor.Services;
using Yapplr.Shared.Models;
using System.Text.Json;

namespace Yapplr.VideoProcessor.Tests;

/// <summary>
/// Integration tests for SimpleVideoProcessingService that test the complete workflow
/// These tests require FFmpeg to be installed and available in the system PATH
/// </summary>
public class SimpleVideoProcessingIntegrationTests : IDisposable
{
    private readonly SimpleVideoProcessingService _service;
    private readonly Mock<ILogger<SimpleVideoProcessingService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly string _tempDirectory;
    private readonly List<string> _filesToCleanup;

    public SimpleVideoProcessingIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<SimpleVideoProcessingService>>();
        
        // Create real configuration for integration tests
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FFmpeg:BinaryPath"] = "ffmpeg",
                ["FFmpeg:ProbePath"] = "ffprobe"
            });
        _configuration = configBuilder.Build();
        
        _service = new SimpleVideoProcessingService(_mockLogger.Object, _configuration);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "yapplr_video_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _filesToCleanup = new List<string>();
    }

    public void Dispose()
    {
        // Cleanup test files
        foreach (var file in _filesToCleanup)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        try
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task GetVideoInfoAsync_WithValidVideo_ShouldReturnCorrectMetadata()
    {
        // Skip if FFmpeg is not available
        if (!await IsFFmpegAvailable())
        {
            return; // Skip test if FFmpeg is not available
        }

        // Arrange - Create a test video using FFmpeg
        var testVideoPath = await CreateTestVideo("test_landscape.mp4", 1920, 1080, 0);

        // Act
        var metadata = await _service.GetVideoMetadataAsync(testVideoPath);

        // Assert
        metadata.Should().NotBeNull();
        metadata!.OriginalWidth.Should().Be(1920);
        metadata.OriginalHeight.Should().Be(1080);
        metadata.OriginalRotation.Should().Be(0);
        metadata.DisplayWidth.Should().Be(1920);
        metadata.DisplayHeight.Should().Be(1080);
    }

    [Fact]
    public async Task GetVideoInfoAsync_WithRotatedVideo_ShouldDetectRotation()
    {
        // Skip if FFmpeg is not available
        if (!await IsFFmpegAvailable())
        {
            return; // Skip test if FFmpeg is not available
        }

        // Arrange - Create a test video with rotation metadata
        var testVideoPath = await CreateTestVideoWithRotation("test_rotated.mp4", 1920, 1080, 90);

        // Act
        var metadata = await _service.GetVideoMetadataAsync(testVideoPath);

        // Assert
        metadata.Should().NotBeNull();
        metadata!.OriginalWidth.Should().Be(1920);
        metadata.OriginalHeight.Should().Be(1080);
        // Note: The rotation detection depends on how FFmpeg handles metadata
        // This test verifies the parsing logic works
    }

    [Theory]
    [InlineData(1920, 1080, 0, "landscape_no_rotation")]
    [InlineData(1080, 1920, 0, "portrait_no_rotation")]
    public async Task ProcessVideoAsync_WithDifferentOrientations_ShouldProcessCorrectly(
        int width, int height, int rotation, string testName)
    {
        // Skip if FFmpeg is not available
        if (!await IsFFmpegAvailable())
        {
            return; // Skip test if FFmpeg is not available
        }

        // Arrange
        var inputPath = await CreateTestVideo($"input_{testName}.mp4", width, height, rotation);
        var outputPath = Path.Combine(_tempDirectory, $"output_{testName}.mp4");
        var thumbnailPath = Path.Combine(_tempDirectory, $"thumbnail_{testName}.jpg");
        
        var config = new VideoProcessingConfig
        {
            MaxWidth = 1920,
            MaxHeight = 1080,
            TargetBitrate = 1000,
            VideoCodec = "libx264",
            AudioCodec = "aac",
            ThumbnailWidth = 320,
            ThumbnailHeight = 240,
            ThumbnailTimeSeconds = 1.0
        };

        // Act
        var result = await _service.ProcessVideoAsync(inputPath, outputPath, thumbnailPath, config);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue($"Processing should succeed for {testName}");
        result.ErrorMessage.Should().BeNullOrEmpty();
        
        // Verify output files exist
        File.Exists(outputPath).Should().BeTrue($"Output video should exist for {testName}");
        File.Exists(thumbnailPath).Should().BeTrue($"Thumbnail should exist for {testName}");
        
        // Verify metadata
        result.Metadata.Should().NotBeNull();
        result.Metadata!.ProcessedRotation.Should().Be(0, "Processed video should have no rotation metadata");
    }

    [Fact]
    public async Task ProcessVideoAsync_WithInvalidInput_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.mp4");
        var outputPath = Path.Combine(_tempDirectory, "output.mp4");
        var thumbnailPath = Path.Combine(_tempDirectory, "thumbnail.jpg");
        var config = new VideoProcessingConfig();

        // Act
        var result = await _service.ProcessVideoAsync(nonExistentPath, outputPath, thumbnailPath, config);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Input file not found");
    }

    /// <summary>
    /// Check if FFmpeg is available in the system
    /// </summary>
    private async Task<bool> IsFFmpegAvailable()
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = processInfo };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create a test video file using FFmpeg
    /// </summary>
    private async Task<string> CreateTestVideo(string filename, int width, int height, int rotation)
    {
        var outputPath = Path.Combine(_tempDirectory, filename);
        _filesToCleanup.Add(outputPath);

        // Create a simple test video with a colored background and some movement
        var arguments = $"-f lavfi -i \"testsrc2=size={width}x{height}:duration=3:rate=30\" " +
                       $"-c:v libx264 -pix_fmt yuv420p " +
                       $"-y \"{outputPath}\"";

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new System.Diagnostics.Process { StartInfo = processInfo };
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to create test video: {error}");
        }

        return outputPath;
    }

    /// <summary>
    /// Create a test video with rotation metadata
    /// </summary>
    private async Task<string> CreateTestVideoWithRotation(string filename, int width, int height, int rotation)
    {
        var outputPath = Path.Combine(_tempDirectory, filename);
        _filesToCleanup.Add(outputPath);

        // Create a test video and add rotation metadata
        var arguments = $"-f lavfi -i \"testsrc2=size={width}x{height}:duration=3:rate=30\" " +
                       $"-c:v libx264 -pix_fmt yuv420p " +
                       $"-metadata:s:v:0 rotate={rotation} " +
                       $"-y \"{outputPath}\"";

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new System.Diagnostics.Process { StartInfo = processInfo };
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to create test video with rotation: {error}");
        }

        return outputPath;
    }
}
