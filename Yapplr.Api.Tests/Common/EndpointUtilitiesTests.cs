using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;
using Yapplr.Api.Common;

namespace Yapplr.Api.Tests.Common;

public class EndpointUtilitiesTests
{
    [Fact]
    public void GetPaginationParams_ValidInput_ReturnsCorrectValues()
    {
        // Arrange
        var page = 2;
        var pageSize = 10;

        // Act
        var (resultPage, resultPageSize) = EndpointUtilities.GetPaginationParams(page, pageSize);

        // Assert
        Assert.Equal(2, resultPage);
        Assert.Equal(10, resultPageSize);
    }

    [Fact]
    public void GetPaginationParams_InvalidPage_ClampsToMinimum()
    {
        // Arrange
        var page = -1;
        var pageSize = 10;

        // Act
        var (resultPage, resultPageSize) = EndpointUtilities.GetPaginationParams(page, pageSize);

        // Assert
        Assert.Equal(1, resultPage);
        Assert.Equal(10, resultPageSize);
    }

    [Fact]
    public void GetPaginationParams_ExcessivePageSize_ClampsToMaximum()
    {
        // Arrange
        var page = 1;
        var pageSize = 200;

        // Act
        var (resultPage, resultPageSize) = EndpointUtilities.GetPaginationParams(page, pageSize);

        // Assert
        Assert.Equal(1, resultPage);
        Assert.Equal(100, resultPageSize);
    }

    [Fact]
    public void CanAccessUserResource_SameUser_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "123")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var resourceOwnerId = 123;

        // Act
        var result = EndpointUtilities.CanAccessUserResource(principal, resourceOwnerId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanAccessUserResource_DifferentUser_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "123")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var resourceOwnerId = 456;

        // Act
        var result = EndpointUtilities.CanAccessUserResource(principal, resourceOwnerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulOperation_ReturnsOkResult()
    {
        // Arrange
        var testData = "test result";
        Func<Task<string?>> operation = () => Task.FromResult<string?>(testData);

        // Act
        var result = await EndpointUtilities.HandleAsync(operation);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>(result);
    }

    [Fact]
    public async Task HandleAsync_NullResult_ReturnsNotFound()
    {
        // Arrange
        Func<Task<string?>> operation = () => Task.FromResult<string?>(null);

        // Act
        var result = await EndpointUtilities.HandleAsync(operation);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        // Check that it's a NotFound result by checking the status code
        var httpResult = result as IStatusCodeHttpResult;
        Assert.NotNull(httpResult);
        Assert.Equal(404, httpResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        Func<Task<string?>> operation = () => throw new ArgumentException("Test error");

        // Act
        var result = await EndpointUtilities.HandleAsync(operation);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        var httpResult = result as IStatusCodeHttpResult;
        Assert.NotNull(httpResult);
        Assert.Equal(400, httpResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_UnauthorizedAccessException_ReturnsForbidden()
    {
        // Arrange
        Func<Task<string?>> operation = () => throw new UnauthorizedAccessException("Access denied");

        // Act
        var result = await EndpointUtilities.HandleAsync(operation);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>(result);
    }

    [Fact]
    public async Task HandleBooleanAsync_SuccessfulOperation_ReturnsOk()
    {
        // Arrange
        Func<Task<bool>> operation = () => Task.FromResult(true);

        // Act
        var result = await EndpointUtilities.HandleBooleanAsync(operation);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        var httpResult = result as IStatusCodeHttpResult;
        Assert.NotNull(httpResult);
        Assert.Equal(200, httpResult.StatusCode);
    }

    [Fact]
    public async Task HandleBooleanAsync_FailedOperation_ReturnsBadRequest()
    {
        // Arrange
        Func<Task<bool>> operation = () => Task.FromResult(false);

        // Act
        var result = await EndpointUtilities.HandleBooleanAsync(operation);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        var httpResult = result as IStatusCodeHttpResult;
        Assert.NotNull(httpResult);
        Assert.Equal(400, httpResult.StatusCode);
    }
}