using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Tests.Integration;

public class SubscriptionIntegrationTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly SubscriptionService _subscriptionService;
    private readonly Mock<ILogger<SubscriptionService>> _mockLogger;

    public SubscriptionIntegrationTests()
    {
        _context = new TestYapplrDbContext();
        _mockLogger = new Mock<ILogger<SubscriptionService>>();
        _subscriptionService = new SubscriptionService(_context, _mockLogger.Object);
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test subscription tiers
        var freeTier = new SubscriptionTier
        {
            Id = 1,
            Name = "Free",
            Description = "Free tier with advertisements",
            Price = 0,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = true,
            SortOrder = 0,
            ShowAdvertisements = true,
            HasVerifiedBadge = false,
            Features = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var subscriberTier = new SubscriptionTier
        {
            Id = 2,
            Name = "Subscriber",
            Description = "No advertisements",
            Price = 3.00m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = false,
            SortOrder = 1,
            ShowAdvertisements = false,
            HasVerifiedBadge = false,
            Features = "{\"noAds\": true}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create test users
        var user1 = new User
        {
            Id = 1,
            Username = "testuser1",
            Email = "test1@example.com",
            PasswordHash = "hash1",
            SubscriptionTierId = 1,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = 2,
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = "hash2",
            SubscriptionTierId = null, // No subscription
            Role = UserRole.User,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.SubscriptionTiers.AddRange(freeTier, subscriberTier);
        _context.Users.AddRange(user1, user2);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Database Relationship Tests

    [Fact]
    public async Task SubscriptionTier_UserRelationship_WorksCorrectly()
    {
        // Act
        var tier = await _context.SubscriptionTiers
            .Include(t => t.Users)
            .FirstAsync(t => t.Id == 1);

        // Assert
        tier.Should().NotBeNull();
        tier.Users.Should().HaveCount(1);
        tier.Users.First().Username.Should().Be("testuser1");
    }

    [Fact]
    public async Task User_SubscriptionTierRelationship_WorksCorrectly()
    {
        // Act
        var user = await _context.Users
            .Include(u => u.SubscriptionTier)
            .FirstAsync(u => u.Id == 1);

        // Assert
        user.Should().NotBeNull();
        user.SubscriptionTier.Should().NotBeNull();
        user.SubscriptionTier!.Name.Should().Be("Free");
    }

    [Fact]
    public async Task User_WithoutSubscription_HasNullTier()
    {
        // Act
        var user = await _context.Users
            .Include(u => u.SubscriptionTier)
            .FirstAsync(u => u.Id == 2);

        // Assert
        user.Should().NotBeNull();
        user.SubscriptionTier.Should().BeNull();
        user.SubscriptionTierId.Should().BeNull();
    }

    #endregion

    #region Service Integration Tests

    [Fact]
    public async Task CreateAndAssignSubscriptionTier_FullWorkflow_WorksCorrectly()
    {
        // Arrange
        var createDto = new CreateSubscriptionTierDto
        {
            Name = "Premium",
            Description = "Premium tier",
            Price = 9.99m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = false,
            SortOrder = 2,
            ShowAdvertisements = false,
            HasVerifiedBadge = true,
            Features = "{\"premium\": true}"
        };

        // Act - Create tier
        var createdTier = await _subscriptionService.CreateSubscriptionTierAsync(createDto);

        // Assert - Tier created
        createdTier.Should().NotBeNull();
        createdTier.Name.Should().Be("Premium");

        // Act - Assign to user
        var assignResult = await _subscriptionService.AssignSubscriptionTierAsync(2, createdTier.Id);

        // Assert - Assignment successful
        assignResult.Should().BeTrue();

        // Verify in database
        var user = await _context.Users
            .Include(u => u.SubscriptionTier)
            .FirstAsync(u => u.Id == 2);

        user.SubscriptionTier.Should().NotBeNull();
        user.SubscriptionTier!.Name.Should().Be("Premium");
        user.SubscriptionTier.HasVerifiedBadge.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSubscriptionTier_WithUsersAssigned_UpdatesCorrectly()
    {
        // Arrange
        var updateDto = new UpdateSubscriptionTierDto
        {
            Name = "Updated Free",
            Description = "Updated free tier description",
            Price = 0,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = true,
            SortOrder = 0,
            ShowAdvertisements = false, // Changed from true
            HasVerifiedBadge = true, // Changed from false
            Features = "{\"updated\": true}"
        };

        // Act
        var result = await _subscriptionService.UpdateSubscriptionTierAsync(1, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.ShowAdvertisements.Should().BeFalse();
        result.HasVerifiedBadge.Should().BeTrue();

        // Verify user sees updated tier
        var user = await _context.Users
            .Include(u => u.SubscriptionTier)
            .FirstAsync(u => u.Id == 1);

        user.SubscriptionTier!.ShowAdvertisements.Should().BeFalse();
        user.SubscriptionTier.HasVerifiedBadge.Should().BeTrue();
        user.SubscriptionTier.Features.Should().Be("{\"updated\": true}");
    }

    [Fact]
    public async Task RemoveUserSubscription_WithActiveSubscription_RemovesCorrectly()
    {
        // Act
        var result = await _subscriptionService.RemoveUserSubscriptionAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify in database
        var user = await _context.Users
            .Include(u => u.SubscriptionTier)
            .FirstAsync(u => u.Id == 1);

        user.SubscriptionTierId.Should().BeNull();
        user.SubscriptionTier.Should().BeNull();

        // Verify tier still exists but has no users
        var tier = await _context.SubscriptionTiers
            .Include(t => t.Users)
            .FirstAsync(t => t.Id == 1);

        tier.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserCountByTier_WithMultipleUsers_ReturnsCorrectCount()
    {
        // Arrange - Assign multiple users to same tier
        await _subscriptionService.AssignSubscriptionTierAsync(2, 1); // Assign user 2 to tier 1

        // Act
        var count = await _subscriptionService.GetUserCountByTierAsync(1);

        // Assert
        count.Should().Be(2); // Both users now on tier 1
    }

    [Fact]
    public async Task DefaultTierManagement_EnsuresOnlyOneDefault()
    {
        // Arrange - Create new tier as default
        var createDto = new CreateSubscriptionTierDto
        {
            Name = "New Default",
            Description = "New default tier",
            Price = 0,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = true, // This should unset the existing default
            SortOrder = 0,
            ShowAdvertisements = true,
            HasVerifiedBadge = false
        };

        // Act
        var newTier = await _subscriptionService.CreateSubscriptionTierAsync(createDto);

        // Assert
        newTier.IsDefault.Should().BeTrue();

        // Verify only one default exists
        var defaultTiers = await _context.SubscriptionTiers
            .Where(t => t.IsDefault)
            .ToListAsync();

        defaultTiers.Should().HaveCount(1);
        defaultTiers.First().Name.Should().Be("New Default");

        // Verify old default is no longer default
        var oldTier = await _context.SubscriptionTiers.FindAsync(1);
        oldTier!.IsDefault.Should().BeFalse();
    }

    #endregion

    #region Data Consistency Tests

    [Fact]
    public async Task SubscriptionTierDeletion_WithAssignedUsers_PreventsDeletion()
    {
        // Act
        var result = await _subscriptionService.DeleteSubscriptionTierAsync(1); // Has user assigned

        // Assert
        result.Should().BeFalse();

        // Verify tier still exists
        var tier = await _context.SubscriptionTiers.FindAsync(1);
        tier.Should().NotBeNull();
    }

    [Fact]
    public async Task SubscriptionTierDeletion_WithoutAssignedUsers_AllowsDeletion()
    {
        // Arrange - Remove user from tier first
        await _subscriptionService.RemoveUserSubscriptionAsync(1);

        // Act
        var result = await _subscriptionService.DeleteSubscriptionTierAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify tier is deleted
        var tier = await _context.SubscriptionTiers.FindAsync(1);
        tier.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentTierUpdates_MaintainDataIntegrity()
    {
        // Arrange - Simulate concurrent updates
        var updateDto1 = new UpdateSubscriptionTierDto
        {
            Name = "Updated Name 1",
            Description = "Description 1",
            Price = 5.00m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = false,
            SortOrder = 1,
            ShowAdvertisements = false,
            HasVerifiedBadge = false
        };

        var updateDto2 = new UpdateSubscriptionTierDto
        {
            Name = "Updated Name 2",
            Description = "Description 2",
            Price = 7.00m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = false,
            SortOrder = 1,
            ShowAdvertisements = false,
            HasVerifiedBadge = false
        };

        // Act - Perform updates sequentially (simulating concurrent scenario)
        var result1 = await _subscriptionService.UpdateSubscriptionTierAsync(2, updateDto1);
        var result2 = await _subscriptionService.UpdateSubscriptionTierAsync(2, updateDto2);

        // Assert - Last update wins
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result2!.Name.Should().Be("Updated Name 2");
        result2.Price.Should().Be(7.00m);

        // Verify final state in database
        var finalTier = await _context.SubscriptionTiers.FindAsync(2);
        finalTier!.Name.Should().Be("Updated Name 2");
        finalTier.Price.Should().Be(7.00m);
    }

    #endregion
}
