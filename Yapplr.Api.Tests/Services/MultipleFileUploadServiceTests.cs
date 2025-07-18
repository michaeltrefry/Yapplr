using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Yapplr.Api.Configuration;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Tests.Services;

public class MultipleFileUploadServiceTests
{
    private readonly Mock<IImageService> _mockImageService;
    private readonly Mock<IVideoService> _mockVideoService;
    private readonly Mock<IUploadSettingsService> _mockUploadSettingsService;
    private readonly Mock<ILogger<MultipleFileUploadService>> _mockLogger;
    private readonly Mock<IOptions<UploadsConfiguration>> _mockUploadsConfig;
    private readonly MultipleFileUploadService _service;

    public MultipleFileUploadServiceTests()
    {
        _mockImageService = new Mock<IImageService>();
        _mockVideoService = new Mock<IVideoService>();
        _mockUploadSettingsService = new Mock<IUploadSettingsService>();
        _mockLogger = new Mock<ILogger<MultipleFileUploadService>>();
        _mockUploadsConfig = new Mock<IOptions<UploadsConfiguration>>();

        _mockUploadsConfig.Setup(x => x.Value).Returns(new UploadsConfiguration());

        // Setup upload settings service mock
        _mockUploadSettingsService.Setup(x => x.GetMaxVideoSizeBytesAsync())
            .ReturnsAsync(1024L * 1024 * 1024); // 1GB
        _mockUploadSettingsService.Setup(x => x.GetMaxImageSizeBytesAsync())
            .ReturnsAsync(5L * 1024 * 1024); // 5MB
        _mockUploadSettingsService.Setup(x => x.GetMaxMediaFilesPerPostAsync())
            .ReturnsAsync(10); // 10 files max
        _mockUploadSettingsService.Setup(x => x.GetAllowedVideoExtensionsAsync())
            .ReturnsAsync(new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv", ".3gp" });
        _mockUploadSettingsService.Setup(x => x.GetAllowedImageExtensionsAsync())
            .ReturnsAsync(new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" });

        _service = new MultipleFileUploadService(
            _mockImageService.Object,
            _mockVideoService.Object,
            _mockUploadSettingsService.Object,
            _mockLogger.Object,
            _mockUploadsConfig.Object
        );
    }

    [Fact]
    public void MaxFilesAllowed_ShouldReturn10()
    {
        // Act
        var result = _service.MaxFilesAllowed;

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void MaxImageSizeBytes_ShouldReturn5MB()
    {
        // Act
        var result = _service.MaxImageSizeBytes;

        // Assert
        result.Should().Be(5 * 1024 * 1024);
    }

    [Fact]
    public void MaxVideoSizeBytes_ShouldReturn1GB()
    {
        // Act
        var result = _service.MaxVideoSizeBytes;

        // Assert
        result.Should().Be(1024L * 1024 * 1024); // 1GB as configured in mock
    }

    [Fact]
    public async Task ValidateMultipleFiles_WithNullFiles_ShouldReturnInvalid()
    {
        // Arrange
        IFormFileCollection? files = null;

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("No files provided");
    }

    [Fact]
    public async Task ValidateMultipleFiles_WithEmptyFiles_ShouldReturnInvalid()
    {
        // Arrange
        var files = new FormFileCollection();

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("No files provided");
    }

    [Fact]
    public async Task ValidateMultipleFiles_WithTooManyFiles_ShouldReturnInvalid()
    {
        // Arrange
        var files = new FormFileCollection();
        for (int i = 0; i < 11; i++)
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns($"file{i}.jpg");
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            files.Add(mockFile.Object);
        }

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Maximum 10 files allowed, but 11 files provided");
    }

    [Fact]
    public async Task ValidateMultipleFiles_WithValidImageFiles_ShouldReturnValid()
    {
        // Arrange
        var files = new FormFileCollection();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        files.Add(mockFile.Object);

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateMultipleFiles_WithOversizedImage_ShouldReturnInvalid()
    {
        // Arrange
        var files = new FormFileCollection();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB (over 5MB limit)
        mockFile.Setup(f => f.FileName).Returns("large.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        files.Add(mockFile.Object);

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("exceeds maximum size"));
    }

    [Fact]
    public async Task ValidateMultipleFiles_WithOversizedVideo_ShouldReturnInvalid()
    {
        // Arrange
        var files = new FormFileCollection();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(2L * 1024 * 1024 * 1024); // 2GB (over 1GB limit)
        mockFile.Setup(f => f.FileName).Returns("large.mp4");
        mockFile.Setup(f => f.ContentType).Returns("video/mp4");
        files.Add(mockFile.Object);

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("exceeds maximum size"));
    }

    [Fact]
    public async Task ValidateMultipleFiles_WithUnsupportedFileType_ShouldReturnInvalid()
    {
        // Arrange
        var files = new FormFileCollection();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.FileName).Returns("document.txt");
        mockFile.Setup(f => f.ContentType).Returns("text/plain");
        files.Add(mockFile.Object);

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("unsupported type"));
    }

    [Fact]
    public async Task ValidateMultipleFiles_WithEmptyFile_ShouldReturnInvalid()
    {
        // Arrange
        var files = new FormFileCollection();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        mockFile.Setup(f => f.FileName).Returns("empty.jpg");
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        files.Add(mockFile.Object);

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("is empty"));
    }

    [Fact]
    public async Task ValidateMultipleFiles_WithMixedValidFiles_ShouldReturnValid()
    {
        // Arrange
        var files = new FormFileCollection();
        
        // Add valid image
        var imageFile = new Mock<IFormFile>();
        imageFile.Setup(f => f.Length).Returns(2 * 1024 * 1024); // 2MB
        imageFile.Setup(f => f.FileName).Returns("image.png");
        imageFile.Setup(f => f.ContentType).Returns("image/png");
        files.Add(imageFile.Object);

        // Add valid video
        var videoFile = new Mock<IFormFile>();
        videoFile.Setup(f => f.Length).Returns(50 * 1024 * 1024); // 50MB
        videoFile.Setup(f => f.FileName).Returns("video.mp4");
        videoFile.Setup(f => f.ContentType).Returns("video/mp4");
        files.Add(videoFile.Object);

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("image/jpeg", "test.jpg")]
    [InlineData("image/png", "test.png")]
    [InlineData("image/gif", "test.gif")]
    [InlineData("image/webp", "test.webp")]
    public async Task ValidateMultipleFiles_WithSupportedImageFormats_ShouldReturnValid(string contentType, string fileName)
    {
        // Arrange
        var files = new FormFileCollection();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        files.Add(mockFile.Object);

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("video/mp4", "test.mp4")]
    [InlineData("video/avi", "test.avi")]
    [InlineData("video/quicktime", "test.mov")]
    [InlineData("video/webm", "test.webm")]
    public async Task ValidateMultipleFiles_WithSupportedVideoFormats_ShouldReturnValid(string contentType, string fileName)
    {
        // Arrange
        var files = new FormFileCollection();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(10 * 1024 * 1024); // 10MB
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        files.Add(mockFile.Object);

        // Act
        var result = await _service.ValidateMultipleFilesAsync(files);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadMultipleMediaFilesAsync_WithValidFiles_ShouldUploadSuccessfully()
    {
        // Arrange
        var files = new FormFileCollection();

        var imageFile = new Mock<IFormFile>();
        imageFile.Setup(f => f.Length).Returns(1024 * 1024);
        imageFile.Setup(f => f.FileName).Returns("test.jpg");
        imageFile.Setup(f => f.ContentType).Returns("image/jpeg");
        files.Add(imageFile.Object);

        _mockImageService.Setup(x => x.SaveImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("uploaded_image.jpg");

        // Act
        var result = await _service.UploadMultipleMediaFilesAsync(files);

        // Assert
        result.Should().NotBeNull();
        result.SuccessfulUploads.Should().Be(1);
        result.FailedUploads.Should().Be(0);
        result.UploadedFiles.Should().HaveCount(1);
        result.UploadedFiles.First().FileName.Should().Be("uploaded_image.jpg");
        result.UploadedFiles.First().MediaType.Should().Be(MediaType.Image);
    }

    [Fact]
    public async Task UploadMultipleMediaFilesAsync_WithMixedFiles_ShouldUploadBoth()
    {
        // Arrange
        var files = new FormFileCollection();

        var imageFile = new Mock<IFormFile>();
        imageFile.Setup(f => f.Length).Returns(1024 * 1024);
        imageFile.Setup(f => f.FileName).Returns("test.jpg");
        imageFile.Setup(f => f.ContentType).Returns("image/jpeg");
        files.Add(imageFile.Object);

        var videoFile = new Mock<IFormFile>();
        videoFile.Setup(f => f.Length).Returns(10 * 1024 * 1024);
        videoFile.Setup(f => f.FileName).Returns("test.mp4");
        videoFile.Setup(f => f.ContentType).Returns("video/mp4");
        files.Add(videoFile.Object);

        _mockImageService.Setup(x => x.SaveImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("uploaded_image.jpg");
        _mockVideoService.Setup(x => x.SaveVideoAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("uploaded_video.mp4");

        // Act
        var result = await _service.UploadMultipleMediaFilesAsync(files);

        // Assert
        result.Should().NotBeNull();
        result.SuccessfulUploads.Should().Be(2);
        result.FailedUploads.Should().Be(0);
        result.UploadedFiles.Should().HaveCount(2);
        result.UploadedFiles.Should().Contain(f => f.MediaType == MediaType.Image);
        result.UploadedFiles.Should().Contain(f => f.MediaType == MediaType.Video);
    }

    [Fact]
    public async Task UploadMultipleMediaFilesAsync_WithFailingUpload_ShouldHandlePartialFailure()
    {
        // Arrange
        var files = new FormFileCollection();

        var imageFile = new Mock<IFormFile>();
        imageFile.Setup(f => f.Length).Returns(1024 * 1024);
        imageFile.Setup(f => f.FileName).Returns("test.jpg");
        imageFile.Setup(f => f.ContentType).Returns("image/jpeg");
        files.Add(imageFile.Object);

        var videoFile = new Mock<IFormFile>();
        videoFile.Setup(f => f.Length).Returns(10 * 1024 * 1024);
        videoFile.Setup(f => f.FileName).Returns("test.mp4");
        videoFile.Setup(f => f.ContentType).Returns("video/mp4");
        files.Add(videoFile.Object);

        _mockImageService.Setup(x => x.SaveImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("uploaded_image.jpg");
        _mockVideoService.Setup(x => x.SaveVideoAsync(It.IsAny<IFormFile>()))
            .ThrowsAsync(new Exception("Video upload failed"));

        // Act
        var result = await _service.UploadMultipleMediaFilesAsync(files);

        // Assert
        result.Should().NotBeNull();
        result.SuccessfulUploads.Should().Be(1);
        result.FailedUploads.Should().Be(1);
        result.UploadedFiles.Should().HaveCount(1);
        result.Errors.Should().HaveCount(1);
        result.Errors.First().ErrorMessage.Should().Contain("Video upload failed");
    }

    [Fact]
    public async Task UploadMultipleMediaFilesAsync_WithInvalidFiles_ShouldReturnErrors()
    {
        // Arrange
        var files = new FormFileCollection();

        var invalidFile = new Mock<IFormFile>();
        invalidFile.Setup(f => f.Length).Returns(1024);
        invalidFile.Setup(f => f.FileName).Returns("test.txt");
        invalidFile.Setup(f => f.ContentType).Returns("text/plain");
        files.Add(invalidFile.Object);

        // Act
        var result = await _service.UploadMultipleMediaFilesAsync(files);

        // Assert
        result.Should().NotBeNull();
        result.SuccessfulUploads.Should().Be(0);
        result.FailedUploads.Should().Be(1);
        result.UploadedFiles.Should().BeEmpty();
        result.Errors.Should().HaveCount(1);
    }
}
