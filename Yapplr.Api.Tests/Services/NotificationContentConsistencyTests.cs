using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Notifications;

namespace Yapplr.Api.Tests.Services;

/// <summary>
/// Tests to verify that the centralized NotificationContentBuilder generates consistent content
/// that would be used by all notification provider services
/// </summary>
public class NotificationContentConsistencyTests
{
    private readonly NotificationContentBuilder _contentBuilder;

    public NotificationContentConsistencyTests()
    {
        _contentBuilder = new NotificationContentBuilder();
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentLikeNotificationContent()
    {
        // Arrange
        var likerUsername = "testliker";
        var postId = 123;

        // Act - Get content from the builder directly
        var content = _contentBuilder.BuildLikeNotification(likerUsername, postId);

        // Assert - Verify consistent content that all services would use
        Assert.Equal("New React", content.Title);
        Assert.Equal("@testliker liked your post", content.Body);
        Assert.Equal("like", content.NotificationType);
        Assert.Equal("like", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("testliker", content.Data["likerUsername"]);
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentCommentNotificationContent()
    {
        // Arrange
        var commenterUsername = "testcommenter";
        var postId = 123;
        var commentId = 456;

        // Act
        var content = _contentBuilder.BuildCommentNotification(commenterUsername, postId, commentId);

        // Assert
        Assert.Equal("New Comment", content.Title);
        Assert.Equal("@testcommenter commented on your post", content.Body);
        Assert.Equal("comment", content.NotificationType);
        Assert.Equal("comment", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("456", content.Data["commentId"]);
        Assert.Equal("testcommenter", content.Data["commenterUsername"]);
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentFollowNotificationContent()
    {
        // Arrange
        var followerUsername = "testfollower";

        // Act
        var content = _contentBuilder.BuildFollowNotification(followerUsername);

        // Assert
        Assert.Equal("New Follower", content.Title);
        Assert.Equal("@testfollower started following you", content.Body);
        Assert.Equal("follow", content.NotificationType);
        Assert.Equal("follow", content.Data["type"]);
        Assert.Equal("testfollower", content.Data["followerUsername"]);
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentMentionNotificationContent()
    {
        // Arrange
        var mentionerUsername = "testmentioner";
        var postId = 123;
        var commentId = 456;

        // Act - Test both post and comment mentions
        var postMentionContent = _contentBuilder.BuildMentionNotification(mentionerUsername, postId);
        var commentMentionContent = _contentBuilder.BuildMentionNotification(mentionerUsername, postId, commentId);

        // Assert - Post mention
        Assert.Equal("You were mentioned", postMentionContent.Title);
        Assert.Equal("@testmentioner mentioned you in a post", postMentionContent.Body);
        Assert.Equal("mention", postMentionContent.NotificationType);
        Assert.False(postMentionContent.Data.ContainsKey("commentId"));

        // Assert - Comment mention
        Assert.Equal("You were mentioned", commentMentionContent.Title);
        Assert.Equal("@testmentioner mentioned you in a comment", commentMentionContent.Body);
        Assert.Equal("mention", commentMentionContent.NotificationType);
        Assert.Equal("456", commentMentionContent.Data["commentId"]);
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentMessageNotificationContent()
    {
        // Arrange
        var senderUsername = "testsender";
        var messageContent = "Hello, this is a test message!";
        var conversationId = 789;

        // Act
        var content = _contentBuilder.BuildMessageNotification(senderUsername, messageContent, conversationId);

        // Assert
        Assert.Equal("New Message", content.Title);
        Assert.Equal("@testsender: Hello, this is a test message!", content.Body);
        Assert.Equal("message", content.NotificationType);
        Assert.Equal("message", content.Data["type"]);
        Assert.Equal("789", content.Data["conversationId"]);
        Assert.Equal("testsender", content.Data["senderUsername"]);
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentReplyNotificationContent()
    {
        // Arrange
        var replierUsername = "testreplier";
        var postId = 123;
        var commentId = 456;

        // Act
        var content = _contentBuilder.BuildReplyNotification(replierUsername, postId, commentId);

        // Assert
        Assert.Equal("New Reply", content.Title);
        Assert.Equal("@testreplier replied to your comment", content.Body);
        Assert.Equal("reply", content.NotificationType);
        Assert.Equal("reply", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("456", content.Data["commentId"]);
        Assert.Equal("testreplier", content.Data["replierUsername"]);
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentRepostNotificationContent()
    {
        // Arrange
        var reposterUsername = "testreposter";
        var postId = 123;

        // Act
        var content = _contentBuilder.BuildRepostNotification(reposterUsername, postId);

        // Assert
        Assert.Equal("New Repost", content.Title);
        Assert.Equal("@testreposter reposted your post", content.Body);
        Assert.Equal("repost", content.NotificationType);
        Assert.Equal("repost", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("testreposter", content.Data["reposterUsername"]);
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentFollowRequestNotificationContent()
    {
        // Arrange
        var requesterUsername = "testrequester";

        // Act
        var content = _contentBuilder.BuildFollowRequestNotification(requesterUsername);

        // Assert
        Assert.Equal("Follow Request", content.Title);
        Assert.Equal("@testrequester wants to follow you", content.Body);
        Assert.Equal("follow_request", content.NotificationType);
        Assert.Equal("follow_request", content.Data["type"]);
        Assert.Equal("testrequester", content.Data["requesterUsername"]);
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentFollowRequestApprovedNotificationContent()
    {
        // Arrange
        var approverUsername = "testapprover";

        // Act
        var content = _contentBuilder.BuildFollowRequestApprovedNotification(approverUsername);

        // Assert
        Assert.Equal("Follow Request Approved", content.Title);
        Assert.Equal("@testapprover approved your follow request", content.Body);
        Assert.Equal("follow_request_approved", content.NotificationType);
        Assert.Equal("follow_request_approved", content.Data["type"]);
        Assert.Equal("testapprover", content.Data["approverUsername"]);
    }

    [Fact]
    public void ContentBuilder_ShouldGenerateConsistentCommentLikeNotificationContent()
    {
        // Arrange
        var likerUsername = "testliker";
        var postId = 123;
        var commentId = 456;

        // Act
        var content = _contentBuilder.BuildCommentLikeNotification(likerUsername, postId, commentId);

        // Assert
        Assert.Equal("New React", content.Title);
        Assert.Equal("@testliker liked your comment", content.Body);
        Assert.Equal("comment_like", content.NotificationType);
        Assert.Equal("comment_like", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("456", content.Data["commentId"]);
        Assert.Equal("testliker", content.Data["likerUsername"]);
    }

    [Fact]
    public void ContentBuilder_ShouldEnsureTypeIsAlwaysInData()
    {
        // Test that all notification types have the 'type' field in their data
        var testCases = new[]
        {
            _contentBuilder.BuildLikeNotification("user", 1),
            _contentBuilder.BuildCommentNotification("user", 1, 2),
            _contentBuilder.BuildFollowNotification("user"),
            _contentBuilder.BuildMessageNotification("user", "message", 1),
            _contentBuilder.BuildMentionNotification("user", 1),
            _contentBuilder.BuildReplyNotification("user", 1, 2),
            _contentBuilder.BuildRepostNotification("user", 1),
            _contentBuilder.BuildFollowRequestNotification("user"),
            _contentBuilder.BuildFollowRequestApprovedNotification("user"),
            _contentBuilder.BuildCommentLikeNotification("user", 1, 2),
            _contentBuilder.BuildSystemMessageNotification("title", "message"),
            _contentBuilder.BuildVideoProcessingCompletedNotification(1),
            _contentBuilder.BuildTestNotification()
        };

        foreach (var content in testCases)
        {
            Assert.True(content.Data.ContainsKey("type"), $"Notification type '{content.NotificationType}' missing 'type' in data");
            Assert.Equal(content.NotificationType, content.Data["type"]);
        }
    }


}
