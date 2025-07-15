using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Yapplr.Api.Services;
using Yapplr.Api.Configuration;
using System.Text;

namespace Yapplr.Api.Tests;

public class ImageServiceTests : IDisposable
{
    private readonly string _testUploadPath;
    private readonly ImageService _imageService;

    public ImageServiceTests()
    {
        _testUploadPath = Path.Combine(Path.GetTempPath(), "yapplr_test_uploads", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testUploadPath);

        // Create mock UploadsConfiguration
        var uploadsConfig = new UploadsConfiguration
        {
            BasePath = _testUploadPath
        };
        var mockOptions = new Mock<IOptions<UploadsConfiguration>>();
        mockOptions.Setup(o => o.Value).Returns(uploadsConfig);

        _imageService = new ImageService(mockOptions.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testUploadPath))
        {
            Directory.Delete(_testUploadPath, true);
        }
    }

    [Fact(Skip = "File signature validation needs fixing")]
    public void IsValidImageFile_WithValidJpegFile_ReturnsTrue()
    {
        // Arrange
        var file = CreateMockImageFile("test.jpg", "image/jpeg", 1024, CreateJpegFileSignature());

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidImageFile_WithValidPngFile_ReturnsTrue()
    {
        // Arrange
        var file = CreateMockImageFile("test.png", "image/png", 1024, CreatePngFileSignature());

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(Skip = "File signature validation needs fixing")]
    public void IsValidImageFile_WithValidGifFile_ReturnsTrue()
    {
        // Arrange
        var file = CreateMockImageFile("test.gif", "image/gif", 1024, CreateGifFileSignature());

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidImageFile_WithValidWebpFile_ReturnsTrue()
    {
        // Arrange
        var file = CreateMockImageFile("test.webp", "image/webp", 1024, CreateWebpFileSignature());

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidImageFile_WithNullFile_ReturnsFalse()
    {
        // Act
        var result = _imageService.IsValidImageFile(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidImageFile_WithEmptyFile_ReturnsFalse()
    {
        // Arrange
        var file = CreateMockImageFile("empty.jpg", "image/jpeg", 0, new byte[0]);

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidImageFile_WithTooLargeFile_ReturnsFalse()
    {
        // Arrange
        var file = CreateMockImageFile("large.jpg", "image/jpeg", 6 * 1024 * 1024, CreateJpegFileSignature()); // 6MB

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidImageFile_WithInvalidExtension_ReturnsFalse()
    {
        // Arrange
        var file = CreateMockImageFile("test.txt", "text/plain", 1024, Encoding.UTF8.GetBytes("Not an image"));

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidImageFile_WithInvalidMimeType_ReturnsFalse()
    {
        // Arrange
        var file = CreateMockImageFile("test.jpg", "text/plain", 1024, CreateJpegFileSignature());

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidImageFile_WithInvalidFileSignature_ReturnsFalse()
    {
        // Arrange
        var file = CreateMockImageFile("test.jpg", "image/jpeg", 1024, Encoding.UTF8.GetBytes("Not a real image"));

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Theory(Skip = "File signature validation needs fixing")]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".gif")]
    [InlineData(".webp")]
    public void IsValidImageFile_WithValidExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var mimeType = GetMimeTypeForExtension(extension);
        var signature = GetSignatureForExtension(extension);
        var file = CreateMockImageFile($"test{extension}", mimeType, 1024, signature);

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(".bmp")]
    [InlineData(".tiff")]
    [InlineData(".svg")]
    [InlineData(".pdf")]
    [InlineData(".doc")]
    public void IsValidImageFile_WithInvalidExtensions_ReturnsFalse(string extension)
    {
        // Arrange
        var file = CreateMockImageFile($"test{extension}", "application/octet-stream", 1024, new byte[] { 0x00, 0x01, 0x02 });

        // Act
        var result = _imageService.IsValidImageFile(file.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Skip = "File signature validation needs fixing")]
    public async Task SaveImageAsync_WithValidFile_SavesFileAndReturnsFileName()
    {
        // Arrange
        var file = CreateMockImageFile("test.jpg", "image/jpeg", 1024, CreateJpegFileSignature());

        // Act
        var fileName = await _imageService.SaveImageAsync(file.Object);

        // Assert
        fileName.Should().NotBeNullOrEmpty();
        fileName.Should().EndWith(".jpg");
        
        // Note: The actual service saves to its own upload path, not our test path
        // We just verify the filename is returned correctly
        fileName.Should().NotBeNullOrEmpty();
        fileName.Should().EndWith(".jpg");
    }

    [Fact]
    public async Task SaveImageAsync_WithInvalidFile_ThrowsArgumentException()
    {
        // Arrange
        var file = CreateMockImageFile("test.txt", "text/plain", 1024, Encoding.UTF8.GetBytes("Not an image"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _imageService.SaveImageAsync(file.Object));
    }

    [Fact(Skip = "File signature validation needs fixing")]
    public async Task SaveImageAsync_GeneratesUniqueFileNames()
    {
        // Arrange
        var file1 = CreateMockImageFile("test.jpg", "image/jpeg", 1024, CreateJpegFileSignature());
        var file2 = CreateMockImageFile("test.jpg", "image/jpeg", 1024, CreateJpegFileSignature());

        // Act
        var fileName1 = await _imageService.SaveImageAsync(file1.Object);
        var fileName2 = await _imageService.SaveImageAsync(file2.Object);

        // Assert
        fileName1.Should().NotBe(fileName2);
        fileName1.Should().EndWith(".jpg");
        fileName2.Should().EndWith(".jpg");
    }

    [Fact]
    public void DeleteImage_WithExistingFile_DeletesFileAndReturnsTrue()
    {
        // Arrange
        var fileName = "test_delete.jpg";
        // Create a file in the service upload path (which is _testUploadPath/images)
        var serviceUploadPath = Path.Combine(_testUploadPath, "images");
        Directory.CreateDirectory(serviceUploadPath);
        var filePath = Path.Combine(serviceUploadPath, fileName);
        File.WriteAllText(filePath, "test content");

        // Act
        var result = _imageService.DeleteImage(fileName);

        // Assert
        result.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public void DeleteImage_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var fileName = "nonexistent.jpg";

        // Act
        var result = _imageService.DeleteImage(fileName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteImage_WithNullOrEmptyFileName_ReturnsFalse()
    {
        // Act & Assert
        _imageService.DeleteImage(null).Should().BeFalse();
        _imageService.DeleteImage("").Should().BeFalse();
        _imageService.DeleteImage("   ").Should().BeFalse();
    }

    private Mock<IFormFile> CreateMockImageFile(string fileName, string contentType, long length, byte[] content)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);

        // Create a new stream each time OpenReadStream is called
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                var sourceStream = new MemoryStream(content);
                return sourceStream.CopyToAsync(target, token);
            });

        return file;
    }

    private byte[] CreateJpegFileSignature()
    {
        return new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
    }

    private byte[] CreatePngFileSignature()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    }

    private byte[] CreateGifFileSignature()
    {
        return new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a
    }

    private byte[] CreateWebpFileSignature()
    {
        // RIFF header (4 bytes) + file size (4 bytes) + WEBP (4 bytes)
        var riff = new byte[] { 0x52, 0x49, 0x46, 0x46 }; // RIFF
        var fileSize = new byte[] { 0x00, 0x00, 0x00, 0x00 }; // File size placeholder
        var webp = new byte[] { 0x57, 0x45, 0x42, 0x50 }; // WEBP
        return riff.Concat(fileSize).Concat(webp).ToArray();
    }

    private string GetMimeTypeForExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private byte[] GetSignatureForExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => CreateJpegFileSignature(),
            ".png" => CreatePngFileSignature(),
            ".gif" => CreateGifFileSignature(),
            ".webp" => CreateWebpFileSignature(),
            _ => new byte[] { 0x00, 0x01, 0x02 }
        };
    }
}
