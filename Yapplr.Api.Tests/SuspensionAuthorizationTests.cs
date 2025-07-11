using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;
using Yapplr.Api.Authorization;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Tests;

public class SuspensionAuthorizationTests
{
    [Fact]
    public async Task RequireActiveUser_ShouldBlock_SuspendedUser()
    {
        // Arrange
        var mockUserService = new Mock<IUserService>();
        var suspendedUser = new User
        {
            Id = 1,
            Status = UserStatus.Suspended,
            Email = "test@example.com",
            Username = "testuser"
        };
        
        mockUserService.Setup(x => x.GetUserEntityByIdAsync(1))
            .ReturnsAsync(suspendedUser);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockUserService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "test"));

        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var authorizationContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        var attribute = new RequireActiveUserAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ForbidResult>(authorizationContext.Result);
    }

    [Fact]
    public async Task RequireActiveUser_ShouldBlock_BannedUser()
    {
        // Arrange
        var mockUserService = new Mock<IUserService>();
        var bannedUser = new User
        {
            Id = 1,
            Status = UserStatus.Banned,
            Email = "test@example.com",
            Username = "testuser"
        };
        
        mockUserService.Setup(x => x.GetUserEntityByIdAsync(1))
            .ReturnsAsync(bannedUser);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockUserService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "test"));

        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var authorizationContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        var attribute = new RequireActiveUserAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.IsType<ForbidResult>(authorizationContext.Result);
    }

    [Fact]
    public async Task RequireActiveUser_ShouldAllow_ActiveUser()
    {
        // Arrange
        var mockUserService = new Mock<IUserService>();
        var activeUser = new User
        {
            Id = 1,
            Status = UserStatus.Active,
            Email = "test@example.com",
            Username = "testuser"
        };
        
        mockUserService.Setup(x => x.GetUserEntityByIdAsync(1))
            .ReturnsAsync(activeUser);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockUserService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "test"));

        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var authorizationContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        var attribute = new RequireActiveUserAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.Null(authorizationContext.Result); // No result means authorization passed
    }

    [Fact]
    public async Task RequireActiveUser_ShouldAutoUnsuspend_ExpiredSuspension()
    {
        // Arrange
        var mockUserService = new Mock<IUserService>();
        var expiredSuspendedUser = new User
        {
            Id = 1,
            Status = UserStatus.Suspended,
            SuspendedUntil = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            Email = "test@example.com",
            Username = "testuser"
        };
        
        mockUserService.Setup(x => x.GetUserEntityByIdAsync(1))
            .ReturnsAsync(expiredSuspendedUser);
        
        mockUserService.Setup(x => x.UnsuspendUserAsync(1))
            .ReturnsAsync(true);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mockUserService.Object);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = serviceProvider;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "test"));

        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var authorizationContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        var attribute = new RequireActiveUserAttribute();

        // Act
        await attribute.OnAuthorizationAsync(authorizationContext);

        // Assert
        Assert.Null(authorizationContext.Result); // Should pass after auto-unsuspension
        mockUserService.Verify(x => x.UnsuspendUserAsync(1), Times.Once);
    }
}
