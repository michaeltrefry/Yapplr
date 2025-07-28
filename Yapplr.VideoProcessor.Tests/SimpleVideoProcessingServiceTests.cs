using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Yapplr.VideoProcessor.Services;
using Yapplr.Shared.Models;
using System.Text.Json;

namespace Yapplr.VideoProcessor.Tests;

public class SimpleVideoProcessingServiceTests
{
    private readonly Mock<ILogger<SimpleVideoProcessingService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly SimpleVideoProcessingService _service;

    public SimpleVideoProcessingServiceTests()
    {
        _mockLogger = new Mock<ILogger<SimpleVideoProcessingService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup configuration mocks using indexer
        _mockConfiguration.Setup(c => c["FFmpeg:BinaryPath"]).Returns("ffmpeg");
        _mockConfiguration.Setup(c => c["FFmpeg:ProbePath"]).Returns("ffprobe");

        _service = new SimpleVideoProcessingService(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Theory]
    [InlineData(1920, 1080, 0, 1920, 1080)]    // No rotation - landscape stays landscape
    [InlineData(1920, 1080, 90, 1080, 1920)]   // 90° rotation - landscape becomes portrait
    [InlineData(1920, 1080, 180, 1920, 1080)]  // 180° rotation - landscape stays landscape
    [InlineData(1920, 1080, 270, 1080, 1920)]  // 270° rotation - landscape becomes portrait
    [InlineData(1080, 1920, 0, 1080, 1920)]    // No rotation - portrait stays portrait
    [InlineData(1080, 1920, 90, 1920, 1080)]   // 90° rotation - portrait becomes landscape
    [InlineData(1080, 1920, 180, 1080, 1920)]  // 180° rotation - portrait stays portrait
    [InlineData(1080, 1920, 270, 1920, 1080)]  // 270° rotation - portrait becomes landscape
    public void CalculateDisplayDimensions_ShouldSwapDimensionsForRotation(
        int originalWidth, int originalHeight, int rotation,
        int expectedWidth, int expectedHeight)
    {
        // Use reflection to access private method
        var method = typeof(SimpleVideoProcessingService)
            .GetMethod("CalculateDisplayDimensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method.Should().NotBeNull("CalculateDisplayDimensions method should exist");
        
        var result = method!.Invoke(_service, new object[] { originalWidth, originalHeight, rotation });
        var (resultWidth, resultHeight) = ((int, int))result!;

        // Assert
        resultWidth.Should().Be(expectedWidth,
            $"width should be {expectedWidth} for {originalWidth}x{originalHeight} with {rotation}° rotation");
        resultHeight.Should().Be(expectedHeight,
            $"height should be {expectedHeight} for {originalWidth}x{originalHeight} with {rotation}° rotation");
    }

    [Theory]
    [InlineData(-90, 270)]   // Negative rotation should be normalized
    [InlineData(450, 90)]    // Rotation > 360 should be normalized
    [InlineData(-270, 90)]   // Multiple negative rotations
    [InlineData(0, 0)]       // Zero rotation
    [InlineData(360, 0)]     // Full rotation
    [InlineData(45, 90)]     // Round to nearest 90°
    [InlineData(135, 180)]   // Round to nearest 90°
    [InlineData(225, 270)]   // Round to nearest 90°
    [InlineData(315, 0)]     // Round to nearest 90°
    public void NormalizeRotation_ShouldWorkCorrectly(int inputRotation, int expectedNormalizedRotation)
    {
        // Use reflection to access private method
        var method = typeof(SimpleVideoProcessingService)
            .GetMethod("NormalizeRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method.Should().NotBeNull("NormalizeRotation method should exist");
        
        var result = (int)method!.Invoke(_service, new object[] { inputRotation })!;

        result.Should().Be(expectedNormalizedRotation,
            $"rotation {inputRotation} should normalize to {expectedNormalizedRotation}");
    }

    [Theory]
    [InlineData(1920, 1080, 1920, 1080, 1920, 1080)]  // No scaling needed
    [InlineData(3840, 2160, 1920, 1080, 1920, 1080)]  // Scale down 4K to 1080p
    [InlineData(1280, 720, 1920, 1080, 1280, 720)]    // No scaling needed (smaller)
    [InlineData(2560, 1440, 1920, 1080, 1920, 1080)]  // Scale down 1440p to 1080p
    [InlineData(1080, 1920, 1920, 1920, 1080, 1920)]   // Portrait video scaling (now allows full portrait)
    [InlineData(800, 600, 1920, 1080, 800, 600)]      // Small video, no scaling
    public void CalculateTargetDimensions_ShouldMaintainAspectRatio(
        int sourceWidth, int sourceHeight, int maxWidth, int maxHeight,
        int expectedWidth, int expectedHeight)
    {
        // Use reflection to access private method
        var method = typeof(SimpleVideoProcessingService)
            .GetMethod("CalculateTargetDimensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method.Should().NotBeNull("CalculateTargetDimensions method should exist");
        
        var result = method!.Invoke(_service, new object[] { sourceWidth, sourceHeight, maxWidth, maxHeight });
        var (resultWidth, resultHeight) = ((int, int))result!;

        // Assert
        resultWidth.Should().Be(expectedWidth,
            $"width should be {expectedWidth} for {sourceWidth}x{sourceHeight} scaled to max {maxWidth}x{maxHeight}");
        resultHeight.Should().Be(expectedHeight,
            $"height should be {expectedHeight} for {sourceWidth}x{sourceHeight} scaled to max {maxWidth}x{maxHeight}");
        
        // Ensure dimensions are even (required for video encoding)
        (resultWidth % 2).Should().Be(0, "width must be even for video encoding");
        (resultHeight % 2).Should().Be(0, "height must be even for video encoding");
    }

    [Theory]
    [InlineData(0, "scale=1920:1080")]
    [InlineData(90, "transpose=1,scale=1920:1080")]
    [InlineData(180, "transpose=1,transpose=1,scale=1920:1080")]
    [InlineData(270, "transpose=2,scale=1920:1080")]
    public void BuildVideoFilter_ShouldCreateCorrectFilterString(int rotation, string expectedFilter)
    {
        // Use reflection to access private method
        var method = typeof(SimpleVideoProcessingService)
            .GetMethod("BuildVideoFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method.Should().NotBeNull("BuildVideoFilter method should exist");
        
        var result = (string)method!.Invoke(_service, new object[] { rotation, 1920, 1080 })!;

        result.Should().Be(expectedFilter,
            $"filter string should be '{expectedFilter}' for {rotation}° rotation");
    }

    [Fact]
    public void BuildFFmpegArguments_ShouldCreateCorrectArgumentString()
    {
        // Use reflection to access private method
        var method = typeof(SimpleVideoProcessingService)
            .GetMethod("BuildFFmpegArguments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method.Should().NotBeNull("BuildFFmpegArguments method should exist");
        
        var config = new VideoProcessingConfig
        {
            VideoCodec = "libx264",
            AudioCodec = "aac",
            TargetBitrate = 2000
        };
        
        var result = (string)method!.Invoke(_service, new object[] { "/input.mp4", "/output.mp4", config, "scale=1920:1080" })!;

        result.Should().Contain("-i \"/input.mp4\"");
        result.Should().Contain("-c:v libx264");
        result.Should().Contain("-c:a aac");
        result.Should().Contain("-b:v 2000k");
        result.Should().Contain("-vf scale=1920:1080");
        result.Should().Contain("-pix_fmt yuv420p");
        result.Should().Contain("-profile:v baseline");
        result.Should().Contain("-level 3.1");
        result.Should().Contain("-movflags +faststart");
        result.Should().Contain("-metadata:s:v:0 rotate=0");
        result.Should().Contain("-y");
        result.Should().Contain("\"/output.mp4\"");
    }

    [Fact]
    public void BuildThumbnailArguments_ShouldCreateCorrectArgumentString()
    {
        // Use reflection to access private method
        var method = typeof(SimpleVideoProcessingService)
            .GetMethod("BuildThumbnailArguments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method.Should().NotBeNull("BuildThumbnailArguments method should exist");
        
        var config = new VideoProcessingConfig
        {
            ThumbnailTimeSeconds = 1.5
        };
        
        var result = (string)method!.Invoke(_service, new object[] { "/input.mp4", "/thumbnail.jpg", config, "scale=320:240" })!;

        result.Should().Contain("-i \"/input.mp4\"");
        result.Should().Contain("-vf scale=320:240");
        result.Should().Contain("-vframes 1");
        result.Should().Contain("-ss 1.5");
        result.Should().Contain("-y");
        result.Should().Contain("\"/thumbnail.jpg\"");
    }

    [Fact]
    public async Task ProcessVideoAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var config = new VideoProcessingConfig();
        var nonExistentPath = "/path/to/nonexistent/file.mp4";
        var outputPath = "/path/to/output.mp4";
        var thumbnailPath = "/path/to/thumbnail.jpg";

        // Act
        var result = await _service.ProcessVideoAsync(nonExistentPath, outputPath, thumbnailPath, config);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Input file not found");
    }

    [Theory]
    [InlineData("rotate", "90")]
    [InlineData("rotate", "-90")]
    [InlineData("rotate", "180")]
    public void GetRotationFromMetadata_ShouldParseRotationFromStringTags(string tagName, string rotationValue)
    {
        // Create a JSON element with rotation in tags as string
        var json = $@"{{
            ""tags"": {{
                ""{tagName}"": ""{rotationValue}""
            }}
        }}";

        using var document = JsonDocument.Parse(json);
        var stream = document.RootElement;

        // Use reflection to access private method
        var method = typeof(SimpleVideoProcessingService)
            .GetMethod("GetRotationFromMetadata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        method.Should().NotBeNull("GetRotationFromMetadata method should exist");

        var result = (int)method!.Invoke(_service, new object[] { stream })!;

        result.Should().Be(int.Parse(rotationValue),
            $"should parse rotation {rotationValue} from string tags");
    }

    [Theory]
    [InlineData("rotate", 90)]
    [InlineData("rotate", -90)]
    [InlineData("rotate", 180)]
    public void GetRotationFromMetadata_ShouldParseRotationFromNumericTags(string tagName, int rotationValue)
    {
        // Create a JSON element with rotation in tags as number
        var json = $@"{{
            ""tags"": {{
                ""{tagName}"": {rotationValue}
            }}
        }}";

        using var document = JsonDocument.Parse(json);
        var stream = document.RootElement;

        // Use reflection to access private method
        var method = typeof(SimpleVideoProcessingService)
            .GetMethod("GetRotationFromMetadata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        method.Should().NotBeNull("GetRotationFromMetadata method should exist");

        var result = (int)method!.Invoke(_service, new object[] { stream })!;

        result.Should().Be(rotationValue,
            $"should parse rotation {rotationValue} from numeric tags");
    }

    [Fact]
    public void GetRotationFromMetadata_WithNoRotation_ShouldReturnZero()
    {
        // Create a JSON element without rotation
        var json = @"{
            ""codec_type"": ""video"",
            ""width"": 1920,
            ""height"": 1080
        }";
        
        using var document = JsonDocument.Parse(json);
        var stream = document.RootElement;
        
        // Use reflection to access private method
        var method = typeof(SimpleVideoProcessingService)
            .GetMethod("GetRotationFromMetadata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method.Should().NotBeNull("GetRotationFromMetadata method should exist");
        
        var result = (int)method!.Invoke(_service, new object[] { stream })!;

        result.Should().Be(0, "should return 0 when no rotation metadata is present");
    }
}
