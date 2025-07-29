using Xunit;
using FluentAssertions;
using Yapplr.Api.Common;
using Yapplr.Api.Extensions;
using Yapplr.Api.Models;

namespace Yapplr.Api.Tests;

public class ExtensionTests
{
    
    [Fact]
    public void UserToDto_WithCompleteUser_MapsAllProperties()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            Bio = "Test bio",
            Birthday = new DateTime(1990, 1, 1),
            Pronouns = "they/them",
            Tagline = "Test tagline",
            ProfileImageFileName = "profile.jpg",
            CreatedAt = new DateTime(2023, 1, 1),
            UpdatedAt = new DateTime(2023, 1, 2),
            EmailVerified = true,
            LastSeenAt = new DateTime(2023, 1, 3),
            Role = UserRole.User,
            Status = UserStatus.Active,
            FcmToken = "test-fcm-token",
            SuspendedUntil = null,
            SuspensionReason = null
        };

        // Act
        var dto = user.MapToUserDto();

        // Assert
        dto.Id.Should().Be(1);
        dto.Email.Should().Be("test@example.com");
        dto.Username.Should().Be("testuser");
        dto.Bio.Should().Be("Test bio");
        dto.Birthday.Should().Be(new DateTime(1990, 1, 1));
        dto.Pronouns.Should().Be("they/them");
        dto.Tagline.Should().Be("Test tagline");
        dto.ProfileImageUrl.Should().Be("http://test.com/api/images/profile.jpg");
        dto.CreatedAt.Should().Be(new DateTime(2023, 1, 1));
        dto.EmailVerified.Should().BeTrue();
        dto.Role.Should().Be(UserRole.User);
        dto.Status.Should().Be(UserStatus.Active);
        dto.FcmToken.Should().Be("test-fcm-token");
        dto.SuspendedUntil.Should().BeNull();
        dto.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public void UserToDto_WithMinimalUser_MapsRequiredProperties()
    {
        // Arrange
        var user = new User
        {
            Id = 2,
            Email = "minimal@example.com",
            Username = "minimal",
            Bio = "",
            Birthday = null,
            Pronouns = "",
            Tagline = "",
            ProfileImageFileName = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EmailVerified = false,

            Role = UserRole.User,
            Status = UserStatus.Active,
            FcmToken = ""
        };

        // Act
        var dto = user.MapToUserDto();

        // Assert
        dto.Id.Should().Be(2);
        dto.Email.Should().Be("minimal@example.com");
        dto.Username.Should().Be("minimal");
        dto.Bio.Should().Be("");
        dto.Birthday.Should().BeNull();
        dto.Pronouns.Should().Be("");
        dto.Tagline.Should().Be("");
        dto.ProfileImageUrl.Should().BeNull();
        dto.EmailVerified.Should().BeFalse();

        dto.Role.Should().Be(UserRole.User);
        dto.Status.Should().Be(UserStatus.Active);
        dto.FcmToken.Should().Be("");
    }

    [Fact]
    public void UserToDto_WithDifferentRoles_MapsCorrectly()
    {
        // Arrange
        var adminUser = new User
        {
            Id = 3,
            Email = "admin@example.com",
            Username = "admin",
            Role = UserRole.Admin,
            Status = UserStatus.Active
        };

        var moderatorUser = new User
        {
            Id = 4,
            Email = "mod@example.com",
            Username = "moderator",
            Role = UserRole.Moderator,
            Status = UserStatus.Active
        };

        // Act
        var adminDto = adminUser.MapToUserDto();
        var modDto = moderatorUser.MapToUserDto();

        // Assert
        adminDto.Role.Should().Be(UserRole.Admin);
        modDto.Role.Should().Be(UserRole.Moderator);
    }

    [Fact]
    public void UserToDto_WithDifferentStatuses_MapsCorrectly()
    {
        // Arrange
        var activeUser = new User
        {
            Id = 5,
            Email = "active@example.com",
            Username = "active",
            Status = UserStatus.Active
        };

        var bannedUser = new User
        {
            Id = 6,
            Email = "banned@example.com",
            Username = "banned",
            Status = UserStatus.Banned
        };

        var suspendedUser = new User
        {
            Id = 7,
            Email = "suspended@example.com",
            Username = "suspended",
            Status = UserStatus.Suspended
        };

        var shadowBannedUser = new User
        {
            Id = 8,
            Email = "shadow@example.com",
            Username = "shadow",
            Status = UserStatus.ShadowBanned
        };

        // Act
        var activeDto = activeUser.MapToUserDto();
        var bannedDto = bannedUser.MapToUserDto();
        var suspendedDto = suspendedUser.MapToUserDto();
        var shadowDto = shadowBannedUser.MapToUserDto();

        // Assert
        activeDto.Status.Should().Be(UserStatus.Active);
        bannedDto.Status.Should().Be(UserStatus.Banned);
        suspendedDto.Status.Should().Be(UserStatus.Suspended);
        shadowDto.Status.Should().Be(UserStatus.ShadowBanned);
    }

    [Fact]
    public void UserToDto_WithSuspendedUser_MapsSuspensionFields()
    {
        // Arrange
        var suspensionDate = new DateTime(2023, 12, 31, 23, 59, 59);
        var suspendedUser = new User
        {
            Id = 10,
            Email = "suspended@example.com",
            Username = "suspended",
            Status = UserStatus.Suspended,
            SuspendedUntil = suspensionDate,
            SuspensionReason = "Violation of community guidelines"
        };

        // Act
        var dto = suspendedUser.MapToUserDto();

        // Assert
        dto.Status.Should().Be(UserStatus.Suspended);
        dto.SuspendedUntil.Should().Be(suspensionDate);
        dto.SuspensionReason.Should().Be("Violation of community guidelines");
    }

    [Fact]
    public void TagToDto_WithCompleteTag_MapsAllProperties()
    {
        // Arrange
        var tag = new Tag
        {
            Id = 1,
            Name = "technology",
            CreatedAt = new DateTime(2023, 1, 1),
            PostCount = 42
        };

        // Act
        var dto = tag.ToDto();

        // Assert
        dto.Id.Should().Be(1);
        dto.Name.Should().Be("technology");
        dto.PostCount.Should().Be(42);
    }

    [Fact]
    public void TagToDto_WithZeroPostCount_MapsCorrectly()
    {
        // Arrange
        var tag = new Tag
        {
            Id = 2,
            Name = "newtag",
            CreatedAt = DateTime.UtcNow,
            PostCount = 0
        };

        // Act
        var dto = tag.ToDto();

        // Assert
        dto.Id.Should().Be(2);
        dto.Name.Should().Be("newtag");
        dto.PostCount.Should().Be(0);
    }

    [Fact]
    public void TagToDto_WithHighPostCount_MapsCorrectly()
    {
        // Arrange
        var tag = new Tag
        {
            Id = 3,
            Name = "popular",
            CreatedAt = DateTime.UtcNow,
            PostCount = 9999
        };

        // Act
        var dto = tag.ToDto();

        // Assert
        dto.Id.Should().Be(3);
        dto.Name.Should().Be("popular");
        dto.PostCount.Should().Be(9999);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("technology")]
    [InlineData("web_development")]
    [InlineData("front-end")]
    [InlineData("very_long_tag_name_that_is_still_valid")]
    public void TagToDto_WithVariousTagNames_MapsCorrectly(string tagName)
    {
        // Arrange
        var tag = new Tag
        {
            Id = 100,
            Name = tagName,
            PostCount = 1
        };

        // Act
        var dto = tag.ToDto();

        // Assert
        dto.Name.Should().Be(tagName);
    }

    [Fact]
    public void UserToDto_PreservesDateTimePrecision()
    {
        // Arrange
        var specificDateTime = new DateTime(2023, 12, 25, 14, 30, 45, 123);
        var user = new User
        {
            Id = 9,
            Email = "datetime@example.com",
            Username = "datetime",
            CreatedAt = specificDateTime,

        };

        // Act
        var dto = user.MapToUserDto();

        // Assert
        dto.CreatedAt.Should().Be(specificDateTime);

    }

    [Fact]
    public void UserToDto_WithSpecialCharactersInFields_MapsCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 10,
            Email = "special+chars@example.com",
            Username = "user_with-special.chars",
            Bio = "Bio with émojis 🚀 and special chars: @#$%",
            Pronouns = "xe/xir",
            Tagline = "Tagline with \"quotes\" and 'apostrophes'"
        };

        // Act
        var dto = user.MapToUserDto();

        // Assert
        dto.Email.Should().Be("special+chars@example.com");
        dto.Username.Should().Be("user_with-special.chars");
        dto.Bio.Should().Be("Bio with émojis 🚀 and special chars: @#$%");
        dto.Pronouns.Should().Be("xe/xir");
        dto.Tagline.Should().Be("Tagline with \"quotes\" and 'apostrophes'");
    }
}
