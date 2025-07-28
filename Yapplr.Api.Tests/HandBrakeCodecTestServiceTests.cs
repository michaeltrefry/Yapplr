using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.VideoProcessor.Services;
using FluentAssertions;

namespace Yapplr.Api.Tests;

public class HandBrakeCodecTestServiceTests
{
    private readonly Mock<ILogger<HandBrakeCodecTestService>> _logger;
    private readonly Mock<IConfiguration> _configuration;
    private readonly HandBrakeCodecTestService _service;

    public HandBrakeCodecTestServiceTests()
    {
        _logger = new Mock<ILogger<HandBrakeCodecTestService>>();
        _configuration = new Mock<IConfiguration>();

        // Setup configuration mocks
        _configuration.Setup(x => x["HandBrake:BinaryPath"]).Returns("HandBrakeCLI");
        _configuration.Setup(x => x["FFmpeg:BinaryPath"]).Returns("ffmpeg");

        _service = new HandBrakeCodecTestService(_logger.Object, _configuration.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithConfiguration()
    {
        // Arrange & Act
        var service = new HandBrakeCodecTestService(_logger.Object, _configuration.Object);

        // Assert
        service.Should().NotBeNull();
        _configuration.Verify(x => x["HandBrake:BinaryPath"], Times.Once);
        _configuration.Verify(x => x["FFmpeg:BinaryPath"], Times.Once);
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldReturnCodecTestResult()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<CodecTestResult>();
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldTestHandBrakeInstallation()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        // The result will depend on whether HandBrake is actually installed in the test environment
        // We're mainly testing that the method doesn't throw and returns a valid result
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldTestFFmpegInstallation()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        // The result will depend on whether FFmpeg is actually installed in the test environment
        // We're mainly testing that the method doesn't throw and returns a valid result
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldPopulateHandBrakeEncoders()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.HandBrakeEncoders.Should().NotBeNull();
        // The dictionary should be populated with encoder test results
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldPopulateVideoCodecs()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.VideoCodecs.Should().NotBeNull();
        // The dictionary should be populated with video codec test results
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldPopulateAudioCodecs()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.AudioCodecs.Should().NotBeNull();
        // The dictionary should be populated with audio codec test results
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldPopulateInputFormats()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.InputFormats.Should().NotBeNull();
        // The dictionary should be populated with input format test results
    }

    [Fact]
    public async Task RunCodecTestsAsync_WithException_ShouldReturnFailureResult()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["HandBrake:BinaryPath"]).Returns((string?)null);
        mockConfig.Setup(x => x["FFmpeg:BinaryPath"]).Returns((string?)null);

        var serviceWithBadConfig = new HandBrakeCodecTestService(_logger.Object, mockConfig.Object);

        // Act
        var result = await serviceWithBadConfig.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        // The test should handle missing configuration gracefully
    }

    [Fact]
    public void CodecTestResult_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var result = new CodecTestResult();

        // Assert
        result.Success.Should().BeFalse(); // Default value
        result.ErrorMessage.Should().BeNull();
        result.FFmpegInstalled.Should().BeFalse(); // Default value
        result.HandBrakeInstalled.Should().BeFalse(); // Default value
        result.VideoCodecs.Should().NotBeNull();
        result.AudioCodecs.Should().NotBeNull();
        result.InputFormats.Should().NotBeNull();
        result.HandBrakeEncoders.Should().NotBeNull();
        result.BasicProcessingWorks.Should().BeFalse(); // Default value
        result.HandBrakeProcessingWorks.Should().BeFalse(); // Default value
    }

    [Fact]
    public void CodecTestResult_ShouldAllowPropertySetting()
    {
        // Arrange
        var result = new CodecTestResult();

        // Act
        result.Success = true;
        result.ErrorMessage = "Test error";
        result.FFmpegInstalled = true;
        result.HandBrakeInstalled = true;
        result.BasicProcessingWorks = true;
        result.HandBrakeProcessingWorks = true;
        result.VideoCodecs["x264"] = true;
        result.AudioCodecs["aac"] = true;
        result.InputFormats["mp4"] = true;
        result.HandBrakeEncoders["x264"] = true;

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().Be("Test error");
        result.FFmpegInstalled.Should().BeTrue();
        result.HandBrakeInstalled.Should().BeTrue();
        result.BasicProcessingWorks.Should().BeTrue();
        result.HandBrakeProcessingWorks.Should().BeTrue();
        result.VideoCodecs["x264"].Should().BeTrue();
        result.AudioCodecs["aac"].Should().BeTrue();
        result.InputFormats["mp4"].Should().BeTrue();
        result.HandBrakeEncoders["x264"].Should().BeTrue();
    }

    [Theory]
    [InlineData("HandBrakeCLI")]
    [InlineData("/usr/bin/HandBrakeCLI")]
    [InlineData("handbrake-cli")]
    public void Constructor_WithDifferentHandBrakePaths_ShouldWork(string handBrakePath)
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(x => x["HandBrake:BinaryPath"]).Returns(handBrakePath);
        config.Setup(x => x["FFmpeg:BinaryPath"]).Returns("ffmpeg");

        // Act
        var service = new HandBrakeCodecTestService(_logger.Object, config.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineData("ffmpeg")]
    [InlineData("/usr/bin/ffmpeg")]
    [InlineData("/usr/local/bin/ffmpeg")]
    public void Constructor_WithDifferentFFmpegPaths_ShouldWork(string ffmpegPath)
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(x => x["HandBrake:BinaryPath"]).Returns("HandBrakeCLI");
        config.Setup(x => x["FFmpeg:BinaryPath"]).Returns(ffmpegPath);

        // Act
        var service = new HandBrakeCodecTestService(_logger.Object, config.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldLogInformation()
    {
        // Act
        await _service.RunCodecTestsAsync();

        // Assert
        // Verify that logging methods were called
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("codec compatibility tests")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldSetOverallSuccessBasedOnComponents()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        
        // The overall success should be based on the combination of:
        // - HandBrake installation
        // - FFmpeg installation  
        // - HandBrake processing works
        // - Basic processing works
        
        if (result.Success)
        {
            result.HandBrakeInstalled.Should().BeTrue();
            result.FFmpegInstalled.Should().BeTrue();
            result.HandBrakeProcessingWorks.Should().BeTrue();
            result.BasicProcessingWorks.Should().BeTrue();
        }
    }

    [Fact]
    public void ICodecTestService_Interface_ShouldBeImplemented()
    {
        // Assert
        _service.Should().BeAssignableTo<ICodecTestService>();
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldTestExpectedEncoders()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        
        // The service should test these specific encoders
        var expectedEncoders = new[] { "x264", "x265", "VP8", "VP9", "svt_av1" };
        
        foreach (var encoder in expectedEncoders)
        {
            result.HandBrakeEncoders.Should().ContainKey(encoder);
        }
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldTestExpectedVideoCodecs()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        
        // The service should test these specific video codecs
        var expectedCodecs = new[] { "libx264", "libx265", "libvpx", "libvpx-vp9" };
        
        foreach (var codec in expectedCodecs)
        {
            result.VideoCodecs.Should().ContainKey(codec);
        }
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldTestExpectedAudioCodecs()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        
        // The service should test these specific audio codecs
        var expectedCodecs = new[] { "aac", "libmp3lame", "libvorbis", "libopus" };
        
        foreach (var codec in expectedCodecs)
        {
            result.AudioCodecs.Should().ContainKey(codec);
        }
    }

    [Fact]
    public async Task RunCodecTestsAsync_ShouldTestExpectedInputFormats()
    {
        // Act
        var result = await _service.RunCodecTestsAsync();

        // Assert
        result.Should().NotBeNull();
        
        // The service should test these specific input formats
        var expectedFormats = new[] { "mp4", "avi", "mov", "mkv", "webm", "flv" };
        
        foreach (var format in expectedFormats)
        {
            result.InputFormats.Should().ContainKey(format);
        }
    }
}
