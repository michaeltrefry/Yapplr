using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Tests;

public class UserServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly UserService _userService;
    private readonly Mock<IBlockService> _mockBlockService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.TransactionStarted))
            .Options;

        _context = new TestYapplrDbContext(options);
        _mockBlockService = new Mock<IBlockService>();
        
        _userService = new UserService(_context, _mockBlockService.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ReturnsUserDto()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            Bio = "Test bio",
            EmailVerified = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Email.Should().Be("test@example.com");
        result.Username.Should().Be("testuser");
        result.Bio.Should().Be("Test bio");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserProfileAsync_WithExistingUser_ReturnsProfile()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            Bio = "Test bio",
            EmailVerified = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockBlockService.Setup(b => b.IsBlockedByUserAsync(It.IsAny<int>(), It.IsAny<int>()))
                         .ReturnsAsync(false);

        // Act
        var result = await _userService.GetUserProfileAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.User.Username.Should().Be("testuser");
        result.User.Bio.Should().Be("Test bio");
        result.IsFollowing.Should().BeFalse();
        result.IsBlocked.Should().BeFalse();
        result.FollowersCount.Should().Be(0);
        result.FollowingCount.Should().Be(0);
        result.PostsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithNonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserProfileAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_WithValidData_UpdatesUserAndReturnsDto()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            Bio = "Old bio",
            EmailVerified = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateUserDto
        {
            Bio = "New bio",
            Tagline = "New tagline",
            Pronouns = "they/them"
        };

        // Act
        var result = await _userService.UpdateUserAsync(1, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Bio.Should().Be("New bio");
        result.Tagline.Should().Be("New tagline");
        result.Pronouns.Should().Be("they/them");

        // Verify database was updated
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.Bio.Should().Be("New bio");
        updatedUser.Tagline.Should().Be("New tagline");
        updatedUser.Pronouns.Should().Be("they/them");
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            Bio = "New bio"
        };

        // Act
        var result = await _userService.UpdateUserAsync(999, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchUsersAsync_WithQuery_ReturnsMatchingUsers()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = 1, Username = "john_doe", Email = "john@example.com", EmailVerified = true },
            new User { Id = 2, Username = "jane_smith", Email = "jane@example.com", EmailVerified = true },
            new User { Id = 3, Username = "bob_jones", Email = "bob@example.com", EmailVerified = true }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        _mockBlockService.Setup(b => b.IsBlockedByUserAsync(It.IsAny<int>(), It.IsAny<int>()))
                         .ReturnsAsync(false);

        // Act
        var result = await _userService.SearchUsersAsync("john", 1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Username.Should().Be("john_doe");
    }

    [Fact]
    public async Task SearchUsersAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", EmailVerified = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.SearchUsersAsync("nonexistent", 1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FollowUserAsync_WithValidUsers_CreatesFollowRelationship()
    {
        // Arrange
        var follower = new User { Id = 1, Username = "follower", Email = "follower@example.com" };
        var following = new User { Id = 2, Username = "following", Email = "following@example.com" };
        _context.Users.AddRange(follower, following);
        await _context.SaveChangesAsync();

        _mockBlockService.Setup(b => b.IsBlockedByUserAsync(It.IsAny<int>(), It.IsAny<int>()))
                         .ReturnsAsync(false);

        // Act
        var result = await _userService.FollowUserAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.IsFollowing.Should().BeTrue();

        // Verify follow relationship was created
        var follow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == 1 && f.FollowingId == 2);
        follow.Should().NotBeNull();
    }

    [Fact]
    public async Task FollowUserAsync_WhenUserTriesToFollowThemselves_ReturnsFailure()
    {
        // Arrange
        var user = new User { Id = 1, Username = "user", Email = "user@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.FollowUserAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("yourself");
    }

    [Fact]
    public async Task FollowUserAsync_WhenAlreadyFollowing_ReturnsFailure()
    {
        // Arrange
        var follower = new User { Id = 1, Username = "follower", Email = "follower@example.com" };
        var following = new User { Id = 2, Username = "following", Email = "following@example.com" };
        var existingFollow = new Follow { FollowerId = 1, FollowingId = 2, CreatedAt = DateTime.UtcNow };
        
        _context.Users.AddRange(follower, following);
        _context.Follows.Add(existingFollow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.FollowUserAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already");
    }

    [Fact]
    public async Task UnfollowUserAsync_WithExistingFollow_RemovesFollowRelationship()
    {
        // Arrange
        var follower = new User { Id = 1, Username = "follower", Email = "follower@example.com" };
        var following = new User { Id = 2, Username = "following", Email = "following@example.com" };
        var existingFollow = new Follow { FollowerId = 1, FollowingId = 2, CreatedAt = DateTime.UtcNow };
        
        _context.Users.AddRange(follower, following);
        _context.Follows.Add(existingFollow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.UnfollowUserAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.IsFollowing.Should().BeFalse();

        // Verify follow relationship was removed
        var follow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == 1 && f.FollowingId == 2);
        follow.Should().BeNull();
    }

    [Fact]
    public async Task UnfollowUserAsync_WithNoExistingFollow_ReturnsFailure()
    {
        // Arrange
        var follower = new User { Id = 1, Username = "follower", Email = "follower@example.com" };
        var following = new User { Id = 2, Username = "following", Email = "following@example.com" };
        _context.Users.AddRange(follower, following);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.UnfollowUserAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not following");
    }

    [Fact]
    public async Task GetFollowingAsync_ReturnsFollowedUsers()
    {
        // Arrange
        var user = new User { Id = 1, Username = "user", Email = "user@example.com" };
        var followed1 = new User { Id = 2, Username = "followed1", Email = "followed1@example.com" };
        var followed2 = new User { Id = 3, Username = "followed2", Email = "followed2@example.com" };
        
        var follows = new[]
        {
            new Follow { FollowerId = 1, FollowingId = 2, CreatedAt = DateTime.UtcNow },
            new Follow { FollowerId = 1, FollowingId = 3, CreatedAt = DateTime.UtcNow }
        };

        _context.Users.AddRange(user, followed1, followed2);
        _context.Follows.AddRange(follows);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetFollowingAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Username == "followed1");
        result.Should().Contain(u => u.Username == "followed2");
    }

    [Fact]
    public async Task GetFollowersAsync_ReturnsFollowerUsers()
    {
        // Arrange
        var user = new User { Id = 1, Username = "user", Email = "user@example.com" };
        var follower1 = new User { Id = 2, Username = "follower1", Email = "follower1@example.com" };
        var follower2 = new User { Id = 3, Username = "follower2", Email = "follower2@example.com" };
        
        var follows = new[]
        {
            new Follow { FollowerId = 2, FollowingId = 1, CreatedAt = DateTime.UtcNow },
            new Follow { FollowerId = 3, FollowingId = 1, CreatedAt = DateTime.UtcNow }
        };

        _context.Users.AddRange(user, follower1, follower2);
        _context.Follows.AddRange(follows);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetFollowersAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Username == "follower1");
        result.Should().Contain(u => u.Username == "follower2");
    }

    [Fact]
    public async Task UpdateProfileImageAsync_WithValidData_UpdatesImageAndReturnsDto()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            ProfileImageFileName = "old-image.jpg"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.UpdateProfileImageAsync(1, "new-image.jpg");

        // Assert
        result.Should().NotBeNull();
        result!.ProfileImageFileName.Should().Be("new-image.jpg");

        // Verify database was updated
        var updatedUser = await _context.Users.FindAsync(1);
        updatedUser!.ProfileImageFileName.Should().Be("new-image.jpg");
    }

    [Fact]
    public async Task UpdateProfileImageAsync_WithNonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _userService.UpdateProfileImageAsync(999, "image.jpg");

        // Assert
        result.Should().BeNull();
    }
}
