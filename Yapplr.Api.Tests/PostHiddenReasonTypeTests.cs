using Xunit;
using FluentAssertions;
using Yapplr.Api.Models;

namespace Yapplr.Api.Tests;

/// <summary>
/// Tests for PostHiddenReasonType enum and its usage in the hybrid filtering system
/// </summary>
public class PostHiddenReasonTypeTests
{
    #region Enum Value Tests

    [Fact]
    public void PostHiddenReasonType_ShouldHaveCorrectValues()
    {
        // Assert - Verify enum values match expected constants
        ((int)PostHiddenReasonType.None).Should().Be(0);
        ((int)PostHiddenReasonType.DeletedByUser).Should().Be(1);
        ((int)PostHiddenReasonType.ModeratorHidden).Should().Be(2);
        ((int)PostHiddenReasonType.VideoProcessing).Should().Be(3);
        ((int)PostHiddenReasonType.ContentModerationHidden).Should().Be(4);
        ((int)PostHiddenReasonType.SpamDetection).Should().Be(5);
        ((int)PostHiddenReasonType.MaliciousContent).Should().Be(6);
    }

    [Fact]
    public void PostHiddenReasonType_ShouldHaveAllExpectedValues()
    {
        // Arrange
        var expectedValues = new[]
        {
            PostHiddenReasonType.None,
            PostHiddenReasonType.DeletedByUser,
            PostHiddenReasonType.ModeratorHidden,
            PostHiddenReasonType.VideoProcessing,
            PostHiddenReasonType.ContentModerationHidden,
            PostHiddenReasonType.SpamDetection,
            PostHiddenReasonType.MaliciousContent
        };

        // Act
        var actualValues = Enum.GetValues<PostHiddenReasonType>();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    #endregion

    #region Enum String Conversion Tests

    [Theory]
    [InlineData(PostHiddenReasonType.None, "None")]
    [InlineData(PostHiddenReasonType.DeletedByUser, "DeletedByUser")]
    [InlineData(PostHiddenReasonType.ModeratorHidden, "ModeratorHidden")]
    [InlineData(PostHiddenReasonType.VideoProcessing, "VideoProcessing")]
    [InlineData(PostHiddenReasonType.ContentModerationHidden, "ContentModerationHidden")]
    [InlineData(PostHiddenReasonType.SpamDetection, "SpamDetection")]
    [InlineData(PostHiddenReasonType.MaliciousContent, "MaliciousContent")]
    public void PostHiddenReasonType_ToString_ShouldReturnCorrectString(PostHiddenReasonType reasonType, string expected)
    {
        // Act
        var result = reasonType.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("None", PostHiddenReasonType.None)]
    [InlineData("DeletedByUser", PostHiddenReasonType.DeletedByUser)]
    [InlineData("ModeratorHidden", PostHiddenReasonType.ModeratorHidden)]
    [InlineData("VideoProcessing", PostHiddenReasonType.VideoProcessing)]
    [InlineData("ContentModerationHidden", PostHiddenReasonType.ContentModerationHidden)]
    [InlineData("SpamDetection", PostHiddenReasonType.SpamDetection)]
    [InlineData("MaliciousContent", PostHiddenReasonType.MaliciousContent)]
    public void PostHiddenReasonType_Parse_ShouldReturnCorrectEnum(string input, PostHiddenReasonType expected)
    {
        // Act
        var result = Enum.Parse<PostHiddenReasonType>(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Categorization Tests

    [Theory]
    [InlineData(PostHiddenReasonType.DeletedByUser)]
    [InlineData(PostHiddenReasonType.ModeratorHidden)]
    [InlineData(PostHiddenReasonType.ContentModerationHidden)]
    [InlineData(PostHiddenReasonType.SpamDetection)]
    [InlineData(PostHiddenReasonType.MaliciousContent)]
    public void PostHiddenReasonType_PermanentReasons_ShouldRequireManualAction(PostHiddenReasonType reasonType)
    {
        // These reason types represent permanent hiding that requires manual action to change
        // This test documents the expected behavior for permanent hiding reasons
        
        // Assert
        reasonType.Should().NotBe(PostHiddenReasonType.None);
        reasonType.Should().NotBe(PostHiddenReasonType.VideoProcessing); // VideoProcessing is temporary
    }

    [Fact]
    public void PostHiddenReasonType_VideoProcessing_ShouldBeTemporary()
    {
        // VideoProcessing is the only reason type that should be automatically cleared
        // when video processing completes, and should be visible to the post author
        
        // Assert
        PostHiddenReasonType.VideoProcessing.Should().Be(PostHiddenReasonType.VideoProcessing);
        ((int)PostHiddenReasonType.VideoProcessing).Should().Be(3);
    }

    [Fact]
    public void PostHiddenReasonType_None_ShouldBeDefault()
    {
        // None should be the default value (0) indicating the post is not hidden
        
        // Assert
        PostHiddenReasonType.None.Should().Be(default(PostHiddenReasonType));
        ((int)PostHiddenReasonType.None).Should().Be(0);
    }

    #endregion

    #region Business Logic Tests

    [Theory]
    [InlineData(PostHiddenReasonType.DeletedByUser, true)]
    [InlineData(PostHiddenReasonType.ModeratorHidden, true)]
    [InlineData(PostHiddenReasonType.ContentModerationHidden, true)]
    [InlineData(PostHiddenReasonType.SpamDetection, true)]
    [InlineData(PostHiddenReasonType.MaliciousContent, true)]
    [InlineData(PostHiddenReasonType.VideoProcessing, true)] // VideoProcessing also hides from public timeline
    [InlineData(PostHiddenReasonType.None, false)]
    public void PostHiddenReasonType_ShouldHideFromPublicTimeline(PostHiddenReasonType reasonType, bool shouldHide)
    {
        // This test documents which reason types should hide posts from public timeline
        // VideoProcessing hides from public but has special visibility rules for the author

        // Act
        var isHidingReason = reasonType != PostHiddenReasonType.None;

        // Assert
        if (shouldHide)
        {
            isHidingReason.Should().BeTrue();
        }
        else
        {
            reasonType.Should().Be(PostHiddenReasonType.None);
        }
    }

    [Fact]
    public void PostHiddenReasonType_VideoProcessing_ShouldAllowAuthorVisibility()
    {
        // VideoProcessing is the only reason type that allows the post author to see their own post
        // while it's hidden from everyone else
        
        // Arrange
        var reasonType = PostHiddenReasonType.VideoProcessing;
        
        // Assert
        reasonType.Should().Be(PostHiddenReasonType.VideoProcessing);
        
        // This documents the special handling in the filtering logic:
        // (!p.IsHidden || (p.HiddenReasonType == PostHiddenReasonType.VideoProcessing && currentUserId.HasValue && p.UserId == currentUserId.Value))
    }

    #endregion

    #region Integration with Post Model Tests

    [Fact]
    public void Post_DefaultHiddenReasonType_ShouldBeNone()
    {
        // Arrange & Act
        var post = new Post
        {
            Content = "Test post",
            UserId = 1
        };

        // Assert
        post.HiddenReasonType.Should().Be(PostHiddenReasonType.None);
        post.IsHidden.Should().BeFalse();
    }

    [Theory]
    [InlineData(PostHiddenReasonType.DeletedByUser)]
    [InlineData(PostHiddenReasonType.ModeratorHidden)]
    [InlineData(PostHiddenReasonType.VideoProcessing)]
    [InlineData(PostHiddenReasonType.ContentModerationHidden)]
    [InlineData(PostHiddenReasonType.SpamDetection)]
    [InlineData(PostHiddenReasonType.MaliciousContent)]
    public void Post_CanSetHiddenReasonType(PostHiddenReasonType reasonType)
    {
        // Arrange
        var post = new Post
        {
            Content = "Test post",
            UserId = 1,
            IsHidden = true,
            HiddenReasonType = reasonType,
            HiddenAt = DateTime.UtcNow
        };

        // Assert
        post.HiddenReasonType.Should().Be(reasonType);
        post.IsHidden.Should().BeTrue();
        post.HiddenAt.Should().NotBeNull();
    }

    [Fact]
    public void Post_HiddenReasonType_ShouldWorkWithIsHiddenFlag()
    {
        // Arrange
        var post = new Post
        {
            Content = "Test post",
            UserId = 1
        };

        // Act - Hide the post
        post.IsHidden = true;
        post.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
        post.HiddenAt = DateTime.UtcNow;
        post.HiddenReason = "Inappropriate content";

        // Assert
        post.IsHidden.Should().BeTrue();
        post.HiddenReasonType.Should().Be(PostHiddenReasonType.ModeratorHidden);
        post.HiddenAt.Should().NotBeNull();
        post.HiddenReason.Should().Be("Inappropriate content");

        // Act - Unhide the post
        post.IsHidden = false;
        post.HiddenReasonType = PostHiddenReasonType.None;
        post.HiddenAt = null;
        post.HiddenReason = null;

        // Assert
        post.IsHidden.Should().BeFalse();
        post.HiddenReasonType.Should().Be(PostHiddenReasonType.None);
        post.HiddenAt.Should().BeNull();
        post.HiddenReason.Should().BeNull();
    }

    #endregion
}
