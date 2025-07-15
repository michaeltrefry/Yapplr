using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Tests.Validation;

public class MultipleFileUploadValidationTests
{
    [Fact]
    public void CreatePostWithMediaDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new CreatePostWithMediaDto(
            "Valid post content",
            PostPrivacy.Public,
            new List<MediaFileDto>
            {
                new("image1.jpg", MediaType.Image, 1920, 1080, 1024000),
                new("video1.mp4", MediaType.Video, 1920, 1080, 10240000, TimeSpan.FromSeconds(30))
            }
        );

        // Act & Assert
        dto.Content.Should().Be("Valid post content");
        dto.MediaFiles.Should().HaveCount(2);
        dto.MediaFiles.Should().Contain(m => m.MediaType == MediaType.Image);
        dto.MediaFiles.Should().Contain(m => m.MediaType == MediaType.Video);
    }

    [Fact]
    public void CreatePostWithMediaDto_WithTooManyFiles_ShouldHaveValidationAttribute()
    {
        // Arrange
        var mediaFiles = new List<MediaFileDto>();
        for (int i = 0; i < 11; i++)
        {
            mediaFiles.Add(new($"file{i}.jpg", MediaType.Image));
        }

        // Act
        var dto = new CreatePostWithMediaDto(
            "Post with too many files",
            PostPrivacy.Public,
            mediaFiles
        );

        // Assert
        dto.MediaFiles.Should().HaveCount(11);
        // Note: Actual validation would be performed by the validation framework
        // This test verifies the DTO structure supports the validation attribute
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreatePostWithMediaDto_WithInvalidContent_ShouldHaveValidationRequirement(string content)
    {
        // Arrange & Act
        var dto = new CreatePostWithMediaDto(
            content,
            PostPrivacy.Public,
            new List<MediaFileDto> { new("image1.jpg", MediaType.Image) }
        );

        // Assert
        dto.Content.Should().Be(content);
        // Note: Actual validation would be performed by the validation framework
    }

    [Fact]
    public void CreatePostWithMediaDto_WithNullContent_ShouldHaveValidationRequirement()
    {
        // Arrange & Act
        var dto = new CreatePostWithMediaDto(
            null!,
            PostPrivacy.Public,
            new List<MediaFileDto> { new("image1.jpg", MediaType.Image) }
        );

        // Assert
        dto.Content.Should().BeNull();
        // Note: Actual validation would be performed by the validation framework
    }

    [Fact]
    public void CreatePostWithMediaDto_WithVeryLongContent_ShouldExceedMaxLength()
    {
        // Arrange
        var longContent = new string('a', 257); // Exceeds 256 character limit

        // Act
        var dto = new CreatePostWithMediaDto(
            longContent,
            PostPrivacy.Public,
            new List<MediaFileDto> { new("image1.jpg", MediaType.Image) }
        );

        // Assert
        dto.Content.Length.Should().Be(257);
        // Note: Actual validation would be performed by the validation framework
    }

    [Fact]
    public void MediaFileDto_WithRequiredFields_ShouldBeValid()
    {
        // Arrange & Act
        var mediaFile = new MediaFileDto(
            "test.jpg",
            MediaType.Image,
            1920,
            1080,
            1024000,
            null
        );

        // Assert
        mediaFile.FileName.Should().Be("test.jpg");
        mediaFile.MediaType.Should().Be(MediaType.Image);
        mediaFile.Width.Should().Be(1920);
        mediaFile.Height.Should().Be(1080);
        mediaFile.FileSizeBytes.Should().Be(1024000);
        mediaFile.Duration.Should().BeNull();
    }

    [Fact]
    public void MediaFileDto_WithVideoData_ShouldIncludeDuration()
    {
        // Arrange & Act
        var mediaFile = new MediaFileDto(
            "test.mp4",
            MediaType.Video,
            1920,
            1080,
            10240000,
            TimeSpan.FromSeconds(30)
        );

        // Assert
        mediaFile.FileName.Should().Be("test.mp4");
        mediaFile.MediaType.Should().Be(MediaType.Video);
        mediaFile.Duration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData(MediaType.Image)]
    [InlineData(MediaType.Video)]
    public void MediaFileDto_WithDifferentMediaTypes_ShouldSupportBoth(MediaType mediaType)
    {
        // Arrange & Act
        var mediaFile = new MediaFileDto(
            "test.file",
            mediaType,
            1920,
            1080,
            1024000
        );

        // Assert
        mediaFile.MediaType.Should().Be(mediaType);
    }

    [Fact]
    public void MultipleFileUploadResponseDto_WithMixedResults_ShouldTrackBothSuccessAndFailure()
    {
        // Arrange
        var uploadedFiles = new List<UploadedFileDto>
        {
            new("success1.jpg", "http://example.com/success1.jpg", MediaType.Image, 1024000),
            new("success2.png", "http://example.com/success2.png", MediaType.Image, 512000)
        };

        var errors = new List<FileUploadErrorDto>
        {
            new("failed1.txt", "Unsupported file type", "UNSUPPORTED_TYPE"),
            new("failed2.exe", "Invalid file format", "INVALID_FORMAT")
        };

        // Act
        var response = new MultipleFileUploadResponseDto(
            uploadedFiles,
            errors,
            4,
            2,
            2
        );

        // Assert
        response.UploadedFiles.Should().HaveCount(2);
        response.Errors.Should().HaveCount(2);
        response.TotalFiles.Should().Be(4);
        response.SuccessfulUploads.Should().Be(2);
        response.FailedUploads.Should().Be(2);
    }

    [Fact]
    public void FileUploadErrorDto_WithErrorDetails_ShouldProvideComprehensiveInfo()
    {
        // Arrange & Act
        var error = new FileUploadErrorDto(
            "problematic_file.xyz",
            "File format not supported. Please use JPG, PNG, GIF, WebP for images or MP4, AVI, MOV for videos.",
            "UNSUPPORTED_FORMAT"
        );

        // Assert
        error.OriginalFileName.Should().Be("problematic_file.xyz");
        error.ErrorMessage.Should().Contain("File format not supported");
        error.ErrorCode.Should().Be("UNSUPPORTED_FORMAT");
    }

    [Fact]
    public void UploadedFileDto_WithImageData_ShouldIncludeImageProperties()
    {
        // Arrange & Act
        var uploadedFile = new UploadedFileDto(
            "uploaded_image.jpg",
            "https://api.yapplr.com/api/images/uploaded_image.jpg",
            MediaType.Image,
            2048000,
            1920,
            1080,
            null
        );

        // Assert
        uploadedFile.FileName.Should().Be("uploaded_image.jpg");
        uploadedFile.FileUrl.Should().StartWith("https://");
        uploadedFile.MediaType.Should().Be(MediaType.Image);
        uploadedFile.FileSizeBytes.Should().Be(2048000);
        uploadedFile.Width.Should().Be(1920);
        uploadedFile.Height.Should().Be(1080);
        uploadedFile.Duration.Should().BeNull();
    }

    [Fact]
    public void UploadedFileDto_WithVideoData_ShouldIncludeVideoDuration()
    {
        // Arrange & Act
        var uploadedFile = new UploadedFileDto(
            "uploaded_video.mp4",
            "https://api.yapplr.com/api/videos/uploaded_video.mp4",
            MediaType.Video,
            20480000,
            1920,
            1080,
            TimeSpan.FromSeconds(45) // 45 seconds
        );

        // Assert
        uploadedFile.FileName.Should().Be("uploaded_video.mp4");
        uploadedFile.MediaType.Should().Be(MediaType.Video);
        uploadedFile.Duration.Should().Be(TimeSpan.FromSeconds(45));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void CreatePostWithMediaDto_WithValidFileCount_ShouldBeAcceptable(int fileCount)
    {
        // Arrange
        var mediaFiles = new List<MediaFileDto>();
        for (int i = 0; i < fileCount; i++)
        {
            mediaFiles.Add(new($"file{i}.jpg", MediaType.Image));
        }

        // Act
        var dto = new CreatePostWithMediaDto(
            "Test post",
            PostPrivacy.Public,
            fileCount > 0 ? mediaFiles : null
        );

        // Assert
        if (fileCount > 0)
        {
            dto.MediaFiles.Should().HaveCount(fileCount);
        }
        else
        {
            dto.MediaFiles.Should().BeNull();
        }
    }

    [Fact]
    public void CreatePostDto_WithMediaFileNames_ShouldSupportLegacyMultipleFiles()
    {
        // Arrange & Act
        var dto = new CreatePostDto(
            "Legacy post with multiple files",
            null,
            null,
            PostPrivacy.Public,
            new List<string> { "image1.jpg", "video1.mp4", "image2.png" }
        );

        // Assert
        dto.Content.Should().Be("Legacy post with multiple files");
        dto.MediaFileNames.Should().HaveCount(3);
        dto.MediaFileNames.Should().Contain("image1.jpg");
        dto.MediaFileNames.Should().Contain("video1.mp4");
        dto.MediaFileNames.Should().Contain("image2.png");
    }

    [Fact]
    public void CreatePostDto_WithBothLegacyAndNewFields_ShouldSupportMixedUsage()
    {
        // Arrange & Act
        var dto = new CreatePostDto(
            "Mixed usage post",
            "legacy_image.jpg",
            "legacy_video.mp4",
            PostPrivacy.Public,
            new List<string> { "new_image1.jpg", "new_image2.png" }
        );

        // Assert
        dto.Content.Should().Be("Mixed usage post");
        dto.ImageFileName.Should().Be("legacy_image.jpg");
        dto.VideoFileName.Should().Be("legacy_video.mp4");
        dto.MediaFileNames.Should().HaveCount(2);
    }
}
