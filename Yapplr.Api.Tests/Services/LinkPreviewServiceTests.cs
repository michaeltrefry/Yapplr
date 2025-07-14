using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using System.Reflection;

namespace Yapplr.Tests.Services;

public class LinkPreviewServiceTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly Mock<ILogger<LinkPreviewService>> _mockLogger;
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly LinkPreviewService _service;

    public LinkPreviewServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);
        _mockLogger = new Mock<ILogger<LinkPreviewService>>();
        _mockHttpClient = new Mock<HttpClient>();
        
        _service = new LinkPreviewService(_context, _mockHttpClient.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?list=PLrAXtmRdnEQy8VJqQzNYaVkGUFBu9PaQr&v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void ExtractYouTubeVideoId_ShouldReturnCorrectVideoId(string url, string expectedVideoId)
    {
        // Use reflection to access the private method
        var method = typeof(LinkPreviewService).GetMethod("ExtractYouTubeVideoId", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        var result = method?.Invoke(null, new object[] { url }) as string;
        
        Assert.Equal(expectedVideoId, result);
    }

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("https://www.facebook.com")]
    [InlineData("https://twitter.com")]
    [InlineData("https://example.com")]
    [InlineData("invalid-url")]
    [InlineData("")]
    public void ExtractYouTubeVideoId_ShouldReturnNullForNonYouTubeUrls(string url)
    {
        // Use reflection to access the private method
        var method = typeof(LinkPreviewService).GetMethod("ExtractYouTubeVideoId", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        var result = method?.Invoke(null, new object[] { url }) as string;
        
        Assert.Null(result);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    public void IsYouTubeUrl_ShouldReturnTrueForYouTubeUrls(string url)
    {
        // Use reflection to access the private method
        var method = typeof(LinkPreviewService).GetMethod("IsYouTubeUrl", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        var result = (bool)(method?.Invoke(null, new object[] { url }) ?? false);
        
        Assert.True(result);
    }

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("https://www.facebook.com")]
    [InlineData("https://twitter.com")]
    [InlineData("invalid-url")]
    [InlineData("")]
    public void IsYouTubeUrl_ShouldReturnFalseForNonYouTubeUrls(string url)
    {
        // Use reflection to access the private method
        var method = typeof(LinkPreviewService).GetMethod("IsYouTubeUrl", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        var result = (bool)(method?.Invoke(null, new object[] { url }) ?? false);
        
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessPostLinksAsync_ShouldExtractYouTubeVideoId()
    {
        // Arrange
        var content = "Check out this video: https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        
        // Act
        var result = await _service.ProcessPostLinksAsync(content);
        
        // Assert
        var linkPreview = result.FirstOrDefault();
        Assert.NotNull(linkPreview);
        Assert.Equal("dQw4w9WgXcQ", linkPreview.YouTubeVideoId);
        Assert.Equal("YouTube", linkPreview.SiteName);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
