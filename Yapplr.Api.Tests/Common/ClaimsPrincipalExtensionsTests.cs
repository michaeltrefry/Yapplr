using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;
using Yapplr.Api.Extensions;
using Yapplr.Api.Models;

namespace Yapplr.Api.Tests.Common;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ValidClaim_ReturnsUserId()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "123")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        Assert.Equal(123, result);
    }

    [Fact]
    public void GetUserId_NoClaim_ThrowsException()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        Assert.Throws<BadHttpRequestException>(() => principal.GetUserId(true));
    }

    [Fact]
    public void GetUserIdOrNull_ValidClaim_ReturnsUserId()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "123")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserIdOrNull();

        // Assert
        Assert.Equal(123, result);
    }

    [Fact]
    public void GetUserIdOrNull_NoClaim_ReturnsNull()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserIdOrNull();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsAuthenticated_AuthenticatedUser_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "123")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.IsAuthenticated();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAuthenticated_UnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.IsAuthenticated();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetUserRole_ValidRoleClaim_ReturnsRole()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserRole();

        // Assert
        Assert.Equal(UserRole.Admin, result);
    }

    [Fact]
    public void GetUserRole_NoRoleClaim_ReturnsNull()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserRole();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void HasRoleOrHigher_UserHasExactRole_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Moderator")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasRoleOrHigher(UserRole.Moderator);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasRoleOrHigher_UserHasHigherRole_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasRoleOrHigher(UserRole.Moderator);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasRoleOrHigher_UserHasLowerRole_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.HasRoleOrHigher(UserRole.Moderator);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAdminOrModerator_AdminUser_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.IsAdminOrModerator();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAdminOrModerator_ModeratorUser_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Moderator")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.IsAdminOrModerator();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAdminOrModerator_RegularUser_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.IsAdminOrModerator();

        // Assert
        Assert.False(result);
    }
}