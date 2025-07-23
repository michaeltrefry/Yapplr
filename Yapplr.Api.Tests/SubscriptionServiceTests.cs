using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Tests;

public class SubscriptionServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly SubscriptionService _service;
    private readonly Mock<ILogger<SubscriptionService>> _mockLogger;

    public SubscriptionServiceTests()
    {
        _context = new TestYapplrDbContext();
        _mockLogger = new Mock<ILogger<SubscriptionService>>();
        _service = new SubscriptionService(_context, _mockLogger.Object);
        
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

        var creatorTier = new SubscriptionTier
        {
            Id = 3,
            Name = "Verified Creator",
            Description = "No ads + verified badge + creator tools",
            Price = 6.00m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = false,
            SortOrder = 2,
            ShowAdvertisements = false,
            HasVerifiedBadge = true,
            Features = "{\"noAds\": true, \"verifiedBadge\": true, \"creatorTools\": true}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveTier = new SubscriptionTier
        {
            Id = 4,
            Name = "Inactive Tier",
            Description = "This tier is inactive",
            Price = 10.00m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = false,
            IsDefault = false,
            SortOrder = 3,
            ShowAdvertisements = false,
            HasVerifiedBadge = false,
            Features = null,
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
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = 2,
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = "hash2",
            SubscriptionTierId = 2,
            CreatedAt = DateTime.UtcNow
        };

        _context.SubscriptionTiers.AddRange(freeTier, subscriberTier, creatorTier, inactiveTier);
        _context.Users.AddRange(user1, user2);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetAllSubscriptionTiersAsync Tests

    [Fact]
    public async Task GetAllSubscriptionTiersAsync_WithIncludeInactiveFalse_ReturnsOnlyActiveTiers()
    {
        // Act
        var result = await _service.GetAllSubscriptionTiersAsync(includeInactive: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.All(t => t.Name != "Inactive Tier").Should().BeTrue();
        result.Should().BeInAscendingOrder(t => t.SortOrder);
    }

    [Fact]
    public async Task GetAllSubscriptionTiersAsync_WithIncludeInactiveTrue_ReturnsAllTiers()
    {
        // Act
        var result = await _service.GetAllSubscriptionTiersAsync(includeInactive: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Any(t => t.Name == "Inactive Tier").Should().BeTrue();
        result.Should().BeInAscendingOrder(t => t.SortOrder);
    }

    [Fact]
    public async Task GetActiveSubscriptionTiersAsync_ReturnsOnlyActiveTiers()
    {
        // Act
        var result = await _service.GetActiveSubscriptionTiersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.All(t => t.Name != "Inactive Tier").Should().BeTrue();
        result.Should().BeInAscendingOrder(t => t.SortOrder);
    }

    #endregion

    #region GetSubscriptionTierAsync Tests

    [Fact]
    public async Task GetSubscriptionTierAsync_WithValidId_ReturnsTier()
    {
        // Act
        var result = await _service.GetSubscriptionTierAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Free");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetSubscriptionTierAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetSubscriptionTierAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSubscriptionTierAsync_WithInactiveTier_ReturnsTier()
    {
        // Act
        var result = await _service.GetSubscriptionTierAsync(4);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Inactive Tier");
        result.IsActive.Should().BeFalse();
    }

    #endregion

    #region GetDefaultSubscriptionTierAsync Tests

    [Fact]
    public async Task GetDefaultSubscriptionTierAsync_ReturnsDefaultTier()
    {
        // Act
        var result = await _service.GetDefaultSubscriptionTierAsync();

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
        result.Name.Should().Be("Free");
    }

    [Fact]
    public async Task GetDefaultSubscriptionTierAsync_WhenNoDefaultExists_ReturnsNull()
    {
        // Arrange - Remove default flag from all tiers
        var tiers = await _context.SubscriptionTiers.ToListAsync();
        foreach (var tier in tiers)
        {
            tier.IsDefault = false;
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDefaultSubscriptionTierAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateSubscriptionTierAsync Tests

    [Fact]
    public async Task CreateSubscriptionTierAsync_WithValidData_CreatesTier()
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
            SortOrder = 5,
            ShowAdvertisements = false,
            HasVerifiedBadge = true,
            Features = "{\"premium\": true}"
        };

        // Act
        var result = await _service.CreateSubscriptionTierAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Premium");
        result.Price.Should().Be(9.99m);
        result.HasVerifiedBadge.Should().BeTrue();

        // Verify in database
        var dbTier = await _context.SubscriptionTiers.FirstOrDefaultAsync(t => t.Name == "Premium");
        dbTier.Should().NotBeNull();
        dbTier!.Features.Should().Be("{\"premium\": true}");
    }

    [Fact]
    public async Task CreateSubscriptionTierAsync_WithIsDefaultTrue_UnsetsExistingDefault()
    {
        // Arrange
        var createDto = new CreateSubscriptionTierDto
        {
            Name = "New Default",
            Description = "New default tier",
            Price = 0,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = true,
            SortOrder = 0,
            ShowAdvertisements = true,
            HasVerifiedBadge = false
        };

        // Act
        var result = await _service.CreateSubscriptionTierAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.IsDefault.Should().BeTrue();

        // Verify old default is no longer default
        var oldDefault = await _context.SubscriptionTiers.FirstOrDefaultAsync(t => t.Name == "Free");
        oldDefault.Should().NotBeNull();
        oldDefault!.IsDefault.Should().BeFalse();

        // Verify only one default exists
        var defaultCount = await _context.SubscriptionTiers.CountAsync(t => t.IsDefault);
        defaultCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateSubscriptionTierAsync_SetsCreatedAndUpdatedAt()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;
        var createDto = new CreateSubscriptionTierDto
        {
            Name = "Test Tier",
            Description = "Test",
            Price = 5.00m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true
        };

        // Act
        var result = await _service.CreateSubscriptionTierAsync(createDto);
        var afterCreate = DateTime.UtcNow;

        // Assert
        result.CreatedAt.Should().BeAfter(beforeCreate);
        result.CreatedAt.Should().BeBefore(afterCreate);
        result.UpdatedAt.Should().BeAfter(beforeCreate);
        result.UpdatedAt.Should().BeBefore(afterCreate);
    }

    #endregion

    #region UpdateSubscriptionTierAsync Tests

    [Fact]
    public async Task UpdateSubscriptionTierAsync_WithValidData_UpdatesTier()
    {
        // Arrange
        var updateDto = new UpdateSubscriptionTierDto
        {
            Name = "Updated Free",
            Description = "Updated description",
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
        var result = await _service.UpdateSubscriptionTierAsync(1, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Free");
        result.ShowAdvertisements.Should().BeFalse();
        result.HasVerifiedBadge.Should().BeTrue();
        result.Features.Should().Be("{\"updated\": true}");

        // Verify UpdatedAt was changed
        var dbTier = await _context.SubscriptionTiers.FindAsync(1);
        dbTier!.UpdatedAt.Should().BeAfter(dbTier.CreatedAt);
    }

    [Fact]
    public async Task UpdateSubscriptionTierAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateSubscriptionTierDto
        {
            Name = "Non-existent",
            Description = "Test",
            Price = 0,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = false,
            SortOrder = 0,
            ShowAdvertisements = true,
            HasVerifiedBadge = false
        };

        // Act
        var result = await _service.UpdateSubscriptionTierAsync(999, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSubscriptionTierAsync_SettingNewDefault_UnsetsOldDefault()
    {
        // Arrange
        var updateDto = new UpdateSubscriptionTierDto
        {
            Name = "Subscriber",
            Description = "No advertisements",
            Price = 3.00m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = true, // Making this the new default
            SortOrder = 1,
            ShowAdvertisements = false,
            HasVerifiedBadge = false,
            Features = "{\"noAds\": true}"
        };

        // Act
        var result = await _service.UpdateSubscriptionTierAsync(2, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();

        // Verify old default is no longer default
        var oldDefault = await _context.SubscriptionTiers.FindAsync(1);
        oldDefault!.IsDefault.Should().BeFalse();

        // Verify only one default exists
        var defaultCount = await _context.SubscriptionTiers.CountAsync(t => t.IsDefault);
        defaultCount.Should().Be(1);
    }

    #endregion

    #region DeleteSubscriptionTierAsync Tests

    [Fact]
    public async Task DeleteSubscriptionTierAsync_WithValidId_DeletesTier()
    {
        // Act
        var result = await _service.DeleteSubscriptionTierAsync(4); // Inactive tier

        // Assert
        result.Should().BeTrue();

        // Verify tier is deleted
        var deletedTier = await _context.SubscriptionTiers.FindAsync(4);
        deletedTier.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSubscriptionTierAsync_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteSubscriptionTierAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSubscriptionTierAsync_WithUsersAssigned_ReturnsFalse()
    {
        // Act - Try to delete tier that has users assigned (tier 1 has user 1)
        var result = await _service.DeleteSubscriptionTierAsync(1);

        // Assert
        result.Should().BeFalse();

        // Verify tier still exists
        var tier = await _context.SubscriptionTiers.FindAsync(1);
        tier.Should().NotBeNull();
    }

    #endregion

    #region AssignSubscriptionTierAsync Tests

    [Fact]
    public async Task AssignSubscriptionTierAsync_WithValidUserAndTier_AssignsTier()
    {
        // Arrange
        var user3 = new User
        {
            Id = 3,
            Username = "testuser3",
            Email = "test3@example.com",
            PasswordHash = "hash3",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AssignSubscriptionTierAsync(3, 2);

        // Assert
        result.Should().BeTrue();

        // Verify assignment
        var updatedUser = await _context.Users.FindAsync(3);
        updatedUser!.SubscriptionTierId.Should().Be(2);
    }

    [Fact]
    public async Task AssignSubscriptionTierAsync_WithInvalidUser_ReturnsFalse()
    {
        // Act
        var result = await _service.AssignSubscriptionTierAsync(999, 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssignSubscriptionTierAsync_WithInvalidTier_ReturnsFalse()
    {
        // Act
        var result = await _service.AssignSubscriptionTierAsync(1, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssignSubscriptionTierAsync_WithInactiveTier_ReturnsFalse()
    {
        // Act
        var result = await _service.AssignSubscriptionTierAsync(1, 4); // Inactive tier

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RemoveUserSubscriptionAsync Tests

    [Fact]
    public async Task RemoveUserSubscriptionAsync_WithValidUser_RemovesSubscription()
    {
        // Act
        var result = await _service.RemoveUserSubscriptionAsync(2);

        // Assert
        result.Should().BeTrue();

        // Verify subscription removed
        var user = await _context.Users.FindAsync(2);
        user!.SubscriptionTierId.Should().BeNull();
    }

    [Fact]
    public async Task RemoveUserSubscriptionAsync_WithInvalidUser_ReturnsFalse()
    {
        // Act
        var result = await _service.RemoveUserSubscriptionAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveUserSubscriptionAsync_WithUserWithoutSubscription_ReturnsTrue()
    {
        // Arrange
        var user3 = new User
        {
            Id = 3,
            Username = "testuser3",
            Email = "test3@example.com",
            PasswordHash = "hash3",
            SubscriptionTierId = null,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RemoveUserSubscriptionAsync(3);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetUserSubscriptionAsync Tests

    [Fact]
    public async Task GetUserSubscriptionAsync_WithValidUser_ReturnsSubscription()
    {
        // Act
        var result = await _service.GetUserSubscriptionAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(1);
        result.Username.Should().Be("testuser1");
        result.SubscriptionTier.Should().NotBeNull();
        result.SubscriptionTier!.Name.Should().Be("Free");
    }

    [Fact]
    public async Task GetUserSubscriptionAsync_WithUserWithoutSubscription_ReturnsUserWithNullTier()
    {
        // Arrange
        var user3 = new User
        {
            Id = 3,
            Username = "testuser3",
            Email = "test3@example.com",
            PasswordHash = "hash3",
            SubscriptionTierId = null,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserSubscriptionAsync(3);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(3);
        result.Username.Should().Be("testuser3");
        result.SubscriptionTier.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSubscriptionAsync_WithInvalidUser_ReturnsNull()
    {
        // Act
        var result = await _service.GetUserSubscriptionAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region EnsureDefaultTierExistsAsync Tests

    [Fact]
    public async Task EnsureDefaultTierExistsAsync_WhenDefaultExists_ReturnsTrue()
    {
        // Act
        var result = await _service.EnsureDefaultTierExistsAsync();

        // Assert
        result.Should().BeTrue();

        // Verify no new tier was created
        var tierCount = await _context.SubscriptionTiers.CountAsync();
        tierCount.Should().Be(4); // Original count
    }

    [Fact]
    public async Task EnsureDefaultTierExistsAsync_WhenNoDefaultExists_CreatesDefaultTier()
    {
        // Arrange - Remove default flag from all tiers
        var tiers = await _context.SubscriptionTiers.ToListAsync();
        foreach (var tier in tiers)
        {
            tier.IsDefault = false;
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.EnsureDefaultTierExistsAsync();

        // Assert
        result.Should().BeTrue();

        // Verify new default tier was created
        var defaultTier = await _context.SubscriptionTiers.FirstOrDefaultAsync(t => t.IsDefault);
        defaultTier.Should().NotBeNull();
        defaultTier!.Name.Should().Be("Free");
        defaultTier.Price.Should().Be(0);
        defaultTier.ShowAdvertisements.Should().BeTrue();
        defaultTier.HasVerifiedBadge.Should().BeFalse();

        // Verify logger was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created default free subscription tier")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureDefaultTierExistsAsync_WhenInactiveDefaultExists_CreatesNewDefaultTier()
    {
        // Arrange - Get initial count and make default tier inactive
        var initialCount = await _context.SubscriptionTiers.CountAsync();
        var defaultTier = await _context.SubscriptionTiers.FirstAsync(t => t.IsDefault);
        defaultTier.IsActive = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.EnsureDefaultTierExistsAsync();

        // Assert
        result.Should().BeTrue();

        // Verify a new default tier was created since no active default existed
        var tierCount = await _context.SubscriptionTiers.CountAsync();
        tierCount.Should().Be(initialCount + 1); // Should be one more than initial count

        // Verify there's now an active default tier
        var activeDefault = await _context.SubscriptionTiers.FirstOrDefaultAsync(t => t.IsDefault && t.IsActive);
        activeDefault.Should().NotBeNull();
        activeDefault!.Name.Should().Be("Free");
    }

    #endregion

    #region GetUserCountByTierAsync Tests

    [Fact]
    public async Task GetUserCountByTierAsync_WithValidTier_ReturnsCorrectCount()
    {
        // Act
        var result = await _service.GetUserCountByTierAsync(1);

        // Assert
        result.Should().Be(1); // One user assigned to tier 1
    }

    [Fact]
    public async Task GetUserCountByTierAsync_WithTierWithNoUsers_ReturnsZero()
    {
        // Act
        var result = await _service.GetUserCountByTierAsync(3);

        // Assert
        result.Should().Be(0); // No users assigned to tier 3
    }

    [Fact]
    public async Task GetUserCountByTierAsync_WithInvalidTier_ReturnsZero()
    {
        // Act
        var result = await _service.GetUserCountByTierAsync(999);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task CreateSubscriptionTierAsync_WithValidUniqueData_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateSubscriptionTierDto
        {
            Name = "Unique Tier Name",
            Description = "Unique tier description",
            Price = 15.99m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = false,
            SortOrder = 10,
            ShowAdvertisements = false,
            HasVerifiedBadge = true,
            Features = "{\"unique\": true}"
        };

        // Act
        var result = await _service.CreateSubscriptionTierAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Unique Tier Name");
        result.Price.Should().Be(15.99m);
        result.HasVerifiedBadge.Should().BeTrue();

        // Verify in database
        var dbTier = await _context.SubscriptionTiers.FirstOrDefaultAsync(t => t.Name == "Unique Tier Name");
        dbTier.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSubscriptionTierAsync_WithValidUniqueData_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateSubscriptionTierDto
        {
            Name = "Updated Unique Name",
            Description = "Updated description",
            Price = 3.50m,
            Currency = "USD",
            BillingCycleMonths = 1,
            IsActive = true,
            IsDefault = false,
            SortOrder = 1,
            ShowAdvertisements = false,
            HasVerifiedBadge = true,
            Features = "{\"updated\": true}"
        };

        // Act
        var result = await _service.UpdateSubscriptionTierAsync(2, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Unique Name");
        result.HasVerifiedBadge.Should().BeTrue();
        result.Features.Should().Be("{\"updated\": true}");
    }

    [Fact]
    public async Task MapToDto_MapsAllPropertiesCorrectly()
    {
        // Act
        var result = await _service.GetSubscriptionTierAsync(2);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Name.Should().Be("Subscriber");
        result.Description.Should().Be("No advertisements");
        result.Price.Should().Be(3.00m);
        result.Currency.Should().Be("USD");
        result.BillingCycleMonths.Should().Be(1);
        result.IsActive.Should().BeTrue();
        result.IsDefault.Should().BeFalse();
        result.SortOrder.Should().Be(1);
        result.ShowAdvertisements.Should().BeFalse();
        result.HasVerifiedBadge.Should().BeFalse();
        result.Features.Should().Be("{\"noAds\": true}");
        result.CreatedAt.Should().BeAfter(DateTime.MinValue);
        result.UpdatedAt.Should().BeAfter(DateTime.MinValue);
    }

    #endregion
}
