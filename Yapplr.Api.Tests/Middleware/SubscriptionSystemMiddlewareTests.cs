using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Text;
using Xunit;
using Yapplr.Api.Middleware;
using Yapplr.Api.Services;

namespace Yapplr.Api.Tests.Middleware;

public class SubscriptionSystemMiddlewareTests
{
    private readonly Mock<ILogger<SubscriptionSystemMiddleware>> _mockLogger;
    private readonly Mock<ISystemConfigurationService> _mockConfigService;
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly SubscriptionSystemMiddleware _middleware;

    public SubscriptionSystemMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<SubscriptionSystemMiddleware>>();
        _mockConfigService = new Mock<ISystemConfigurationService>();
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new SubscriptionSystemMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_NonSubscriptionEndpoint_CallsNext()
    {
        // Arrange
        var context = CreateHttpContext("/api/users");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockConfigService.Object);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockConfigService.Verify(x => x.IsSubscriptionSystemEnabledAsync(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_SubscriptionEndpointEnabled_CallsNext()
    {
        // Arrange
        var context = CreateHttpContext("/api/subscriptions/tiers");
        _mockConfigService.Setup(x => x.IsSubscriptionSystemEnabledAsync()).ReturnsAsync(true);
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockConfigService.Object);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockConfigService.Verify(x => x.IsSubscriptionSystemEnabledAsync(), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_SubscriptionEndpointDisabled_Returns404()
    {
        // Arrange
        var context = CreateHttpContext("/api/subscriptions/tiers");
        _mockConfigService.Setup(x => x.IsSubscriptionSystemEnabledAsync()).ReturnsAsync(false);

        // Act
        await _middleware.InvokeAsync(context, _mockConfigService.Object);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);
        _mockNext.Verify(x => x(It.IsAny<HttpContext>()), Times.Never);
        _mockConfigService.Verify(x => x.IsSubscriptionSystemEnabledAsync(), Times.Once);

        // Verify response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        Assert.Equal("Not Found", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_AdminSubscriptionEndpoint_CallsNext()
    {
        // Arrange
        var context = CreateHttpContext("/api/admin/subscriptions");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockConfigService.Object);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockConfigService.Verify(x => x.IsSubscriptionSystemEnabledAsync(), Times.Never);
    }

    [Theory]
    [InlineData("/api/subscriptions")]
    [InlineData("/api/subscriptions/tiers")]
    [InlineData("/api/subscriptions/my-subscription")]
    [InlineData("/API/SUBSCRIPTIONS/TIERS")] // Case insensitive
    public async Task InvokeAsync_VariousSubscriptionEndpoints_ChecksSystemStatus(string path)
    {
        // Arrange
        var context = CreateHttpContext(path);
        _mockConfigService.Setup(x => x.IsSubscriptionSystemEnabledAsync()).ReturnsAsync(true);
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockConfigService.Object);

        // Assert
        _mockConfigService.Verify(x => x.IsSubscriptionSystemEnabledAsync(), Times.Once);
    }

    [Theory]
    [InlineData("/api/admin/subscriptions")]
    [InlineData("/api/admin/subscriptions/tiers")]
    [InlineData("/api/admin/subscription-system/status")]
    [InlineData("/api/users")]
    [InlineData("/api/posts")]
    [InlineData("/health")]
    public async Task InvokeAsync_NonBlockedEndpoints_DoesNotCheckSystemStatus(string path)
    {
        // Arrange
        var context = CreateHttpContext(path);
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockConfigService.Object);

        // Assert
        _mockConfigService.Verify(x => x.IsSubscriptionSystemEnabledAsync(), Times.Never);
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_NullPath_CallsNext()
    {
        // Arrange
        var context = CreateHttpContext(null);
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockConfigService.Object);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockConfigService.Verify(x => x.IsSubscriptionSystemEnabledAsync(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_EmptyPath_CallsNext()
    {
        // Arrange
        var context = CreateHttpContext("");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context, _mockConfigService.Object);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockConfigService.Verify(x => x.IsSubscriptionSystemEnabledAsync(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_SubscriptionEndpointDisabled_LogsWarning()
    {
        // Arrange
        var context = CreateHttpContext("/api/subscriptions/tiers");
        _mockConfigService.Setup(x => x.IsSubscriptionSystemEnabledAsync()).ReturnsAsync(false);

        // Act
        await _middleware.InvokeAsync(context, _mockConfigService.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Subscription system is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private static HttpContext CreateHttpContext(string? path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path ?? "";
        context.Response.Body = new MemoryStream();
        return context;
    }
}
