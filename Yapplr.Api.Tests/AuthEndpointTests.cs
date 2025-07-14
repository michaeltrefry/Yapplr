using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Exceptions;
using Yapplr.Api.Common;
using Yapplr.Api.CQRS;

namespace Yapplr.Api.Tests;

public class AuthServiceExceptionTests : IDisposable
{
    private readonly YapplrDbContext _context;
    private readonly AuthService _authService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ICommandPublisher> _mockCommandPublisher;

    public AuthServiceExceptionTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new YapplrDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-that-is-long-enough-for-hmac-sha256-algorithm",
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience",
                ["JwtSettings:ExpirationInMinutes"] = "60"
            })
            .Build();

        _mockEmailService = new Mock<IEmailService>();
        _mockCommandPublisher = new Mock<ICommandPublisher>();

        _authService = new AuthService(_context, configuration, _mockEmailService.Object, _mockCommandPublisher.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsNull()
    {
        // Arrange
        var loginDto = new LoginUserDto("nonexistent@example.com", "wrongpassword");

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ThrowsEmailNotVerifiedException()
    {
        // Arrange
        var user = new User
        {
            Email = "unverified@example.com",
            Username = "unverifieduser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginUserDto("unverified@example.com", "Password123!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailNotVerifiedException>(() => _authService.LoginAsync(loginDto));
        exception.Email.Should().Be("unverified@example.com");
        exception.Message.Should().Contain("verified");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = new User
        {
            Email = "verified@example.com",
            Username = "verifieduser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginUserDto("verified@example.com", "Password123!");

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("verified@example.com");
    }
}
