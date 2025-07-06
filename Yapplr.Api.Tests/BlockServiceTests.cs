using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;

namespace Yapplr.Api.Tests;

// Custom test DbContext that ignores problematic properties
public class TestYapplrDbContext : YapplrDbContext
{
    public TestYapplrDbContext(DbContextOptions<YapplrDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore the problematic NotificationHistory.Data property for tests
        modelBuilder.Entity<NotificationHistory>()
            .Ignore(nh => nh.Data);
    }
}

public class BlockServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly BlockService _blockService;

    public BlockServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestYapplrDbContext(options);
        _blockService = new BlockService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact(Skip = "InMemory database doesn't support transactions")]
    public async Task BlockUserAsync_WithValidUsers_CreatesBlockRelationship()
    {
        // Arrange
        var blocker = new User { Id = 1, Username = "blocker", Email = "blocker@test.com" };
        var blocked = new User { Id = 2, Username = "blocked", Email = "blocked@test.com" };
        
        _context.Users.AddRange(blocker, blocked);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.BlockUserAsync(1, 2);

        // Assert
        result.Should().BeTrue();
        
        var blockExists = await _context.Blocks
            .AnyAsync(b => b.BlockerId == 1 && b.BlockedId == 2);
        blockExists.Should().BeTrue();
    }

    [Fact]
    public async Task BlockUserAsync_WhenUserTriesToBlockThemselves_ReturnsFalse()
    {
        // Arrange
        var user = new User { Id = 1, Username = "user", Email = "user@test.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.BlockUserAsync(1, 1);

        // Assert
        result.Should().BeFalse();
        
        var blockExists = await _context.Blocks.AnyAsync();
        blockExists.Should().BeFalse();
    }

    [Fact]
    public async Task BlockUserAsync_WhenAlreadyBlocked_ReturnsFalse()
    {
        // Arrange
        var blocker = new User { Id = 1, Username = "blocker", Email = "blocker@test.com" };
        var blocked = new User { Id = 2, Username = "blocked", Email = "blocked@test.com" };
        var existingBlock = new Block { BlockerId = 1, BlockedId = 2, CreatedAt = DateTime.UtcNow };
        
        _context.Users.AddRange(blocker, blocked);
        _context.Blocks.Add(existingBlock);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.BlockUserAsync(1, 2);

        // Assert
        result.Should().BeFalse();
        
        var blockCount = await _context.Blocks.CountAsync();
        blockCount.Should().Be(1); // Should still be just the original block
    }

    [Fact(Skip = "InMemory database doesn't support transactions")]
    public async Task BlockUserAsync_RemovesExistingFollowRelationships()
    {
        // Arrange
        var blocker = new User { Id = 1, Username = "blocker", Email = "blocker@test.com" };
        var blocked = new User { Id = 2, Username = "blocked", Email = "blocked@test.com" };
        var follow1 = new Follow { FollowerId = 1, FollowingId = 2, CreatedAt = DateTime.UtcNow };
        var follow2 = new Follow { FollowerId = 2, FollowingId = 1, CreatedAt = DateTime.UtcNow };
        
        _context.Users.AddRange(blocker, blocked);
        _context.Follows.AddRange(follow1, follow2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.BlockUserAsync(1, 2);

        // Assert
        result.Should().BeTrue();
        
        var followsExist = await _context.Follows.AnyAsync();
        followsExist.Should().BeFalse();
        
        var blockExists = await _context.Blocks
            .AnyAsync(b => b.BlockerId == 1 && b.BlockedId == 2);
        blockExists.Should().BeTrue();
    }

    [Fact(Skip = "InMemory database doesn't support transactions")]
    public async Task BlockUserAsync_RemovesOnlyRelevantFollowRelationships()
    {
        // Arrange
        var blocker = new User { Id = 1, Username = "blocker", Email = "blocker@test.com" };
        var blocked = new User { Id = 2, Username = "blocked", Email = "blocked@test.com" };
        var otherUser = new User { Id = 3, Username = "other", Email = "other@test.com" };
        
        var relevantFollow1 = new Follow { FollowerId = 1, FollowingId = 2, CreatedAt = DateTime.UtcNow };
        var relevantFollow2 = new Follow { FollowerId = 2, FollowingId = 1, CreatedAt = DateTime.UtcNow };
        var irrelevantFollow = new Follow { FollowerId = 1, FollowingId = 3, CreatedAt = DateTime.UtcNow };
        
        _context.Users.AddRange(blocker, blocked, otherUser);
        _context.Follows.AddRange(relevantFollow1, relevantFollow2, irrelevantFollow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.BlockUserAsync(1, 2);

        // Assert
        result.Should().BeTrue();
        
        var remainingFollows = await _context.Follows.ToListAsync();
        remainingFollows.Should().HaveCount(1);
        remainingFollows.First().FollowingId.Should().Be(3);
    }

    [Fact]
    public async Task UnblockUserAsync_WithExistingBlock_RemovesBlockAndReturnsTrue()
    {
        // Arrange
        var blocker = new User { Id = 1, Username = "blocker", Email = "blocker@test.com" };
        var blocked = new User { Id = 2, Username = "blocked", Email = "blocked@test.com" };
        var block = new Block { BlockerId = 1, BlockedId = 2, CreatedAt = DateTime.UtcNow };
        
        _context.Users.AddRange(blocker, blocked);
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.UnblockUserAsync(1, 2);

        // Assert
        result.Should().BeTrue();
        
        var blockExists = await _context.Blocks
            .AnyAsync(b => b.BlockerId == 1 && b.BlockedId == 2);
        blockExists.Should().BeFalse();
    }

    [Fact]
    public async Task UnblockUserAsync_WithNoExistingBlock_ReturnsFalse()
    {
        // Arrange
        var blocker = new User { Id = 1, Username = "blocker", Email = "blocker@test.com" };
        var blocked = new User { Id = 2, Username = "blocked", Email = "blocked@test.com" };
        
        _context.Users.AddRange(blocker, blocked);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.UnblockUserAsync(1, 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserBlockedAsync_WithExistingBlock_ReturnsTrue()
    {
        // Arrange
        var blocker = new User { Id = 1, Username = "blocker", Email = "blocker@test.com" };
        var blocked = new User { Id = 2, Username = "blocked", Email = "blocked@test.com" };
        var block = new Block { BlockerId = 1, BlockedId = 2, CreatedAt = DateTime.UtcNow };

        _context.Users.AddRange(blocker, blocked);
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.IsUserBlockedAsync(1, 2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserBlockedAsync_WithNoBlock_ReturnsFalse()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.IsUserBlockedAsync(1, 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsBlockedByUserAsync_WithExistingBlock_ReturnsTrue()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };
        var block = new Block { BlockerId = 2, BlockedId = 1, CreatedAt = DateTime.UtcNow }; // User2 blocks User1

        _context.Users.AddRange(user1, user2);
        _context.Blocks.Add(block);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.IsBlockedByUserAsync(1, 2); // Check if User1 is blocked by User2

        // Assert
        result.Should().BeTrue(); // User1 should be considered blocked by User2
    }

    [Fact]
    public async Task GetBlockedUsersAsync_ReturnsCorrectBlockedUsers()
    {
        // Arrange
        var blocker = new User { Id = 1, Username = "blocker", Email = "blocker@test.com" };
        var blocked1 = new User { Id = 2, Username = "blocked1", Email = "blocked1@test.com", Bio = "Bio 1" };
        var blocked2 = new User { Id = 3, Username = "blocked2", Email = "blocked2@test.com", Bio = "Bio 2" };
        var notBlocked = new User { Id = 4, Username = "notblocked", Email = "notblocked@test.com" };
        
        var block1 = new Block { BlockerId = 1, BlockedId = 2, CreatedAt = DateTime.UtcNow };
        var block2 = new Block { BlockerId = 1, BlockedId = 3, CreatedAt = DateTime.UtcNow };
        
        _context.Users.AddRange(blocker, blocked1, blocked2, notBlocked);
        _context.Blocks.AddRange(block1, block2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.GetBlockedUsersAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Username == "blocked1");
        result.Should().Contain(u => u.Username == "blocked2");
        result.Should().NotContain(u => u.Username == "notblocked");
    }

    [Fact]
    public async Task GetBlockedUsersAsync_WithNoBlocks_ReturnsEmptyList()
    {
        // Arrange
        var user = new User { Id = 1, Username = "user", Email = "user@test.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _blockService.GetBlockedUsersAsync(1);

        // Assert
        result.Should().BeEmpty();
    }


}
