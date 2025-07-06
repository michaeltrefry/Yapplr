using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly AuthService _authService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IEmailService> _mockEmailService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.TransactionStarted))
            .Options;

        _context = new TestYapplrDbContext(options);
        
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("test-secret-key-that-is-long-enough-for-hmac-sha256");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("test-issuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("test-audience");
        
        _mockEmailService = new Mock<IEmailService>();
        
        _authService = new AuthService(_context, _mockConfiguration.Object, _mockEmailService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserAndReturnsSuccess()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("test@example.com");
        result.User.Username.Should().Be("testuser");
        result.User.EmailVerified.Should().BeFalse();
        result.Token.Should().NotBeNullOrEmpty();

        // Verify user was created in database
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        user.Should().NotBeNull();
        user!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            Username = "existing",
            PasswordHash = "hash"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            Email = "existing@example.com",
            Username = "newuser",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("email");
        result.User.Should().BeNull();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ReturnsFailure()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            Username = "existinguser",
            PasswordHash = "hash"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            Email = "new@example.com",
            Username = "existinguser",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("username");
        result.User.Should().BeNull();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            EmailVerified = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("test@example.com");
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid");
        result.User.Should().BeNull();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            EmailVerified = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword!"
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid");
        result.User.Should().BeNull();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            EmailVerified = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("verify");
        result.User.Should().BeNull();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_VerifiesEmailAndReturnsSuccess()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            EmailVerified = false,
            EmailVerificationToken = "valid-token"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.VerifyEmailAsync("valid-token");

        // Assert
        result.Should().BeTrue();
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.EmailVerified.Should().BeTrue();
        updatedUser.EmailVerificationToken.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            EmailVerified = false,
            EmailVerificationToken = "valid-token"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.VerifyEmailAsync("invalid-token");

        // Assert
        result.Should().BeFalse();
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.EmailVerified.Should().BeFalse();
        updatedUser.EmailVerificationToken.Should().Be("valid-token");
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithValidEmail_SendsEmailAndReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            EmailVerified = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockEmailService.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                         .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RequestPasswordResetAsync("test@example.com");

        // Assert
        result.Should().BeTrue();
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.PasswordResetToken.Should().NotBeNullOrEmpty();
        updatedUser.PasswordResetTokenExpiry.Should().BeAfter(DateTime.UtcNow);
        
        _mockEmailService.Verify(e => e.SendPasswordResetEmailAsync("test@example.com", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithInvalidEmail_ReturnsFalse()
    {
        // Act
        var result = await _authService.RequestPasswordResetAsync("nonexistent@example.com");

        // Assert
        result.Should().BeFalse();
        _mockEmailService.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ResetsPasswordAndReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
            EmailVerified = true,
            PasswordResetToken = "valid-reset-token",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetDto = new ResetPasswordDto
        {
            Token = "valid-reset-token",
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(resetDto);

        // Assert
        result.Should().BeTrue();
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.PasswordResetToken.Should().BeNull();
        updatedUser.PasswordResetTokenExpiry.Should().BeNull();
        
        // Verify new password works
        BCrypt.Net.BCrypt.Verify("NewPassword123!", updatedUser.PasswordHash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("OldPassword123!", updatedUser.PasswordHash).Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
            EmailVerified = true,
            PasswordResetToken = "expired-token",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(-1) // Expired
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetDto = new ResetPasswordDto
        {
            Token = "expired-token",
            NewPassword = "NewPassword123!"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(resetDto);

        // Assert
        result.Should().BeFalse();
        
        var updatedUser = await _context.Users.FindAsync(user.Id);
        // Password should remain unchanged
        BCrypt.Net.BCrypt.Verify("OldPassword123!", updatedUser!.PasswordHash).Should().BeTrue();
    }
}
