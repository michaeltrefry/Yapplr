using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using Yapplr.Api.Configuration;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;

namespace Yapplr.Api.Tests.Performance;

public class MultipleFileUploadPerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IImageService> _mockImageService;
    private readonly Mock<IVideoService> _mockVideoService;
    private readonly Mock<ILogger<MultipleFileUploadService>> _mockLogger;
    private readonly Mock<IOptions<UploadsConfiguration>> _mockUploadsConfig;
    private readonly MultipleFileUploadService _service;

    public MultipleFileUploadPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _mockImageService = new Mock<IImageService>();
        _mockVideoService = new Mock<IVideoService>();
        _mockLogger = new Mock<ILogger<MultipleFileUploadService>>();
        _mockUploadsConfig = new Mock<IOptions<UploadsConfiguration>>();

        _mockUploadsConfig.Setup(x => x.Value).Returns(new UploadsConfiguration());

        // Setup fast mock responses
        _mockImageService.Setup(x => x.SaveImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync((IFormFile file) => $"uploaded_{file.FileName}");
        
        _mockVideoService.Setup(x => x.SaveVideoAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync((IFormFile file) => $"uploaded_{file.FileName}");

        _service = new MultipleFileUploadService(
            _mockImageService.Object,
            _mockVideoService.Object,
            _mockLogger.Object,
            _mockUploadsConfig.Object
        );
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void ValidateMultipleFiles_WithMaximumFiles_ShouldCompleteQuickly()
    {
        // Arrange
        var files = CreateMockFiles(10, "image/jpeg", ".jpg", 1024 * 1024);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = _service.ValidateMultipleFiles(files);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Validation of 10 files took: {stopwatch.ElapsedMilliseconds}ms");
        
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should complete in under 100ms
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task UploadMultipleMediaFilesAsync_WithMaximumFiles_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var files = CreateMockFiles(10, "image/jpeg", ".jpg", 1024 * 1024);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _service.UploadMultipleMediaFilesAsync(files);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Upload of 10 files took: {stopwatch.ElapsedMilliseconds}ms");
        
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete in under 5 seconds
        result.SuccessfulUploads.Should().Be(10);
        result.FailedUploads.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task UploadMultipleMediaFilesAsync_WithMixedLargeFiles_ShouldHandleEfficiently()
    {
        // Arrange
        var files = new FormFileCollection();
        
        // Add large images (5MB each)
        for (int i = 0; i < 5; i++)
        {
            var imageFile = CreateMockFile($"large_image_{i}.jpg", "image/jpeg", 5 * 1024 * 1024);
            files.Add(imageFile);
        }
        
        // Add large videos (50MB each)
        for (int i = 0; i < 3; i++)
        {
            var videoFile = CreateMockFile($"large_video_{i}.mp4", "video/mp4", 50 * 1024 * 1024);
            files.Add(videoFile);
        }

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _service.UploadMultipleMediaFilesAsync(files);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Upload of 8 large files took: {stopwatch.ElapsedMilliseconds}ms");
        
        result.SuccessfulUploads.Should().Be(8);
        result.FailedUploads.Should().Be(0);
        
        // Should handle large files efficiently (allowing more time for large files)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [Trait("Category", "Performance")]
    public void ValidateMultipleFiles_WithVaryingFileCounts_ShouldScaleLinearly(int fileCount)
    {
        // Arrange
        var files = CreateMockFiles(fileCount, "image/jpeg", ".jpg", 1024 * 1024);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = _service.ValidateMultipleFiles(files);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Validation of {fileCount} files took: {stopwatch.ElapsedMilliseconds}ms");
        
        // Performance should scale roughly linearly with file count
        var expectedMaxTime = fileCount * 10; // 10ms per file should be more than enough
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(expectedMaxTime);
        
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task UploadMultipleMediaFilesAsync_WithConcurrentRequests_ShouldHandleLoad()
    {
        // Arrange
        var tasks = new List<Task<MultipleFileUploadResponseDto>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate 5 concurrent upload requests
        for (int i = 0; i < 5; i++)
        {
            var files = CreateMockFiles(3, "image/jpeg", ".jpg", 1024 * 1024);
            tasks.Add(_service.UploadMultipleMediaFilesAsync(files));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"5 concurrent uploads (3 files each) took: {stopwatch.ElapsedMilliseconds}ms");
        
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r => r.SuccessfulUploads.Should().Be(3));
        
        // Should handle concurrent requests efficiently
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(8000);
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void ValidateMultipleFiles_WithInvalidFiles_ShouldFailFast()
    {
        // Arrange
        var files = new FormFileCollection();
        
        // Add files that will fail validation quickly
        for (int i = 0; i < 10; i++)
        {
            var invalidFile = CreateMockFile($"invalid_{i}.txt", "text/plain", 1024);
            files.Add(invalidFile);
        }

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = _service.ValidateMultipleFiles(files);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Validation of 10 invalid files took: {stopwatch.ElapsedMilliseconds}ms");
        
        // Should fail fast for invalid files
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(10);
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task UploadMultipleMediaFilesAsync_WithPartialFailures_ShouldContinueProcessing()
    {
        // Arrange
        var files = CreateMockFiles(5, "image/jpeg", ".jpg", 1024 * 1024);
        
        // Setup some uploads to fail
        _mockImageService.SetupSequence(x => x.SaveImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("success_1.jpg")
            .ThrowsAsync(new Exception("Upload failed"))
            .ReturnsAsync("success_3.jpg")
            .ThrowsAsync(new Exception("Upload failed"))
            .ReturnsAsync("success_5.jpg");

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _service.UploadMultipleMediaFilesAsync(files);

        // Assert
        stopwatch.Stop();
        _output.WriteLine($"Upload with partial failures took: {stopwatch.ElapsedMilliseconds}ms");
        
        result.SuccessfulUploads.Should().Be(3);
        result.FailedUploads.Should().Be(2);
        
        // Should handle partial failures efficiently
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000);
    }

    [Fact]
    [Trait("Category", "Stress")]
    public void ValidateMultipleFiles_WithMaximumSizeFiles_ShouldHandleMemoryEfficiently()
    {
        // Arrange
        var files = new FormFileCollection();
        
        // Add maximum size files
        for (int i = 0; i < 5; i++)
        {
            var largeImageFile = CreateMockFile($"max_image_{i}.jpg", "image/jpeg", 5 * 1024 * 1024);
            files.Add(largeImageFile);
        }
        
        for (int i = 0; i < 3; i++)
        {
            var largeVideoFile = CreateMockFile($"max_video_{i}.mp4", "video/mp4", 100 * 1024 * 1024);
            files.Add(largeVideoFile);
        }

        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = _service.ValidateMultipleFiles(files);

        // Assert
        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
        
        _output.WriteLine($"Validation of maximum size files took: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Memory used: {memoryUsed / 1024 / 1024}MB");
        
        result.IsValid.Should().BeTrue();
        
        // Memory usage should be reasonable (validation shouldn't load entire files into memory)
        memoryUsed.Should().BeLessThan(50 * 1024 * 1024); // Less than 50MB
    }

    private FormFileCollection CreateMockFiles(int count, string contentType, string extension, long fileSize)
    {
        var files = new FormFileCollection();
        for (int i = 0; i < count; i++)
        {
            var file = CreateMockFile($"test_{i}{extension}", contentType, fileSize);
            files.Add(file);
        }
        return files;
    }

    private IFormFile CreateMockFile(string fileName, string contentType, long fileSize)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(fileSize);
        
        // Create a mock stream for the file content
        var stream = new MemoryStream(new byte[fileSize]);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        
        return mockFile.Object;
    }
}
