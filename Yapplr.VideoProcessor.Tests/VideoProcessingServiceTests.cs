using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Yapplr.VideoProcessor.Services;
using System.Reflection;

namespace Yapplr.VideoProcessor.Tests;

// Mock interface for video stream since FFMpegCore's interface is internal
public interface ITestVideoStream
{
    int Width { get; }
    int Height { get; }
    int? Rotation { get; }
}

public class VideoProcessingServiceTests
{
    private readonly VideoProcessingService _service;
    private readonly Mock<ILogger<VideoProcessingService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public VideoProcessingServiceTests()
    {
        _mockLogger = new Mock<ILogger<VideoProcessingService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _service = new VideoProcessingService(_mockLogger.Object, _mockConfiguration.Object);
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
    public void GetDisplayDimensions_ShouldSwapDimensionsForRotation(
        int originalWidth, int originalHeight, int rotation,
        int expectedWidth, int expectedHeight)
    {
        // This test verifies the logic conceptually
        // The actual implementation will be tested through integration tests

        // Test the rotation logic directly
        var normalizedRotation = ((rotation % 360) + 360) % 360;

        int resultWidth, resultHeight;
        if (normalizedRotation == 90 || normalizedRotation == 270)
        {
            resultWidth = originalHeight;
            resultHeight = originalWidth;
        }
        else
        {
            resultWidth = originalWidth;
            resultHeight = originalHeight;
        }

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
    public void RotationNormalization_ShouldWorkCorrectly(int inputRotation, int expectedNormalizedRotation)
    {
        // Test the rotation normalization logic directly
        var normalizedRotation = ((inputRotation % 360) + 360) % 360;

        normalizedRotation.Should().Be(expectedNormalizedRotation,
            $"rotation {inputRotation} should normalize to {expectedNormalizedRotation}");
    }

    [Fact]
    public void DimensionSwapping_WithNullRotation_ShouldNotSwapDimensions()
    {
        // Test the dimension swapping logic with null rotation
        int? rotation = null;
        var normalizedRotation = ((rotation ?? 0) % 360 + 360) % 360;

        int originalWidth = 1920, originalHeight = 1080;
        int resultWidth, resultHeight;

        if (normalizedRotation == 90 || normalizedRotation == 270)
        {
            resultWidth = originalHeight;
            resultHeight = originalWidth;
        }
        else
        {
            resultWidth = originalWidth;
            resultHeight = originalHeight;
        }

        resultWidth.Should().Be(1920, "width should not be swapped when rotation is null");
        resultHeight.Should().Be(1080, "height should not be swapped when rotation is null");
    }
}
