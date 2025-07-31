using Xunit;
using Yapplr.Api.Services;
using Yapplr.Api.Models;
using Yapplr.Api.Services.Notifications;

namespace Yapplr.Api.Tests.Services;

public class NotificationContentBuilderTests
{
    private readonly NotificationContentBuilder _builder;

    public NotificationContentBuilderTests()
    {
        _builder = new NotificationContentBuilder();
    }

    [Fact]
    public void BuildLikeNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var likerUsername = "testuser";
        var postId = 123;

        // Act
        var content = _builder.BuildLikeNotification(likerUsername, postId);

        // Assert
        Assert.Equal("New React", content.Title);
        Assert.Equal("@testuser liked your post", content.Body);
        Assert.Equal("like", content.NotificationType);
        Assert.Equal("like", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("testuser", content.Data["likerUsername"]);
    }

    [Fact]
    public void BuildCommentLikeNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var likerUsername = "testuser";
        var postId = 123;
        var commentId = 456;

        // Act
        var content = _builder.BuildCommentLikeNotification(likerUsername, postId, commentId);

        // Assert
        Assert.Equal("New React", content.Title);
        Assert.Equal("@testuser liked your comment", content.Body);
        Assert.Equal("comment_like", content.NotificationType);
        Assert.Equal("comment_like", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("456", content.Data["commentId"]);
        Assert.Equal("testuser", content.Data["likerUsername"]);
    }

    [Fact]
    public void BuildCommentNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var commenterUsername = "testuser";
        var postId = 123;
        var commentId = 456;

        // Act
        var content = _builder.BuildCommentNotification(commenterUsername, postId, commentId);

        // Assert
        Assert.Equal("New Comment", content.Title);
        Assert.Equal("@testuser commented on your post", content.Body);
        Assert.Equal("comment", content.NotificationType);
        Assert.Equal("comment", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("456", content.Data["commentId"]);
        Assert.Equal("testuser", content.Data["commenterUsername"]);
    }

    [Fact]
    public void BuildFollowNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var followerUsername = "testuser";

        // Act
        var content = _builder.BuildFollowNotification(followerUsername);

        // Assert
        Assert.Equal("New Follower", content.Title);
        Assert.Equal("@testuser started following you", content.Body);
        Assert.Equal("follow", content.NotificationType);
        Assert.Equal("follow", content.Data["type"]);
        Assert.Equal("testuser", content.Data["followerUsername"]);
    }

    [Fact]
    public void BuildFollowRequestNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var requesterUsername = "testuser";

        // Act
        var content = _builder.BuildFollowRequestNotification(requesterUsername);

        // Assert
        Assert.Equal("Follow Request", content.Title);
        Assert.Equal("@testuser wants to follow you", content.Body);
        Assert.Equal("follow_request", content.NotificationType);
        Assert.Equal("follow_request", content.Data["type"]);
        Assert.Equal("testuser", content.Data["requesterUsername"]);
    }

    [Fact]
    public void BuildFollowRequestApprovedNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var approverUsername = "testuser";

        // Act
        var content = _builder.BuildFollowRequestApprovedNotification(approverUsername);

        // Assert
        Assert.Equal("Follow Request Approved", content.Title);
        Assert.Equal("@testuser approved your follow request", content.Body);
        Assert.Equal("follow_request_approved", content.NotificationType);
        Assert.Equal("follow_request_approved", content.Data["type"]);
        Assert.Equal("testuser", content.Data["approverUsername"]);
    }

    [Fact]
    public void BuildMessageNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var senderUsername = "testuser";
        var messageContent = "Hello, this is a test message!";
        var conversationId = 789;

        // Act
        var content = _builder.BuildMessageNotification(senderUsername, messageContent, conversationId);

        // Assert
        Assert.Equal("New Message", content.Title);
        Assert.Equal("@testuser: Hello, this is a test message!", content.Body);
        Assert.Equal("message", content.NotificationType);
        Assert.Equal("message", content.Data["type"]);
        Assert.Equal("789", content.Data["conversationId"]);
        Assert.Equal("testuser", content.Data["senderUsername"]);
    }

    [Fact]
    public void BuildMessageNotification_ShouldTruncateLongMessages()
    {
        // Arrange
        var senderUsername = "testuser";
        var longMessage = "This is a very long message that should be truncated because it exceeds the maximum length limit";
        var conversationId = 789;

        // Act
        var content = _builder.BuildMessageNotification(senderUsername, longMessage, conversationId);

        // Assert
        Assert.Equal("New Message", content.Title);
        Assert.Equal("@testuser: This is a very long message that should be truncat...", content.Body);
        Assert.Equal("message", content.NotificationType);
    }

    [Fact]
    public void BuildMentionNotification_InPost_ShouldReturnConsistentContent()
    {
        // Arrange
        var mentionerUsername = "testuser";
        var postId = 123;

        // Act
        var content = _builder.BuildMentionNotification(mentionerUsername, postId);

        // Assert
        Assert.Equal("You were mentioned", content.Title);
        Assert.Equal("@testuser mentioned you in a post", content.Body);
        Assert.Equal("mention", content.NotificationType);
        Assert.Equal("mention", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("testuser", content.Data["mentionerUsername"]);
        Assert.False(content.Data.ContainsKey("commentId"));
    }

    [Fact]
    public void BuildMentionNotification_InComment_ShouldReturnConsistentContent()
    {
        // Arrange
        var mentionerUsername = "testuser";
        var postId = 123;
        var commentId = 456;

        // Act
        var content = _builder.BuildMentionNotification(mentionerUsername, postId, commentId);

        // Assert
        Assert.Equal("You were mentioned", content.Title);
        Assert.Equal("@testuser mentioned you in a comment", content.Body);
        Assert.Equal("mention", content.NotificationType);
        Assert.Equal("mention", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("456", content.Data["commentId"]);
        Assert.Equal("testuser", content.Data["mentionerUsername"]);
    }

    [Fact]
    public void BuildReplyNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var replierUsername = "testuser";
        var postId = 123;
        var commentId = 456;

        // Act
        var content = _builder.BuildReplyNotification(replierUsername, postId, commentId);

        // Assert
        Assert.Equal("New Reply", content.Title);
        Assert.Equal("@testuser replied to your comment", content.Body);
        Assert.Equal("reply", content.NotificationType);
        Assert.Equal("reply", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("456", content.Data["commentId"]);
        Assert.Equal("testuser", content.Data["replierUsername"]);
    }

    [Fact]
    public void BuildRepostNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var reposterUsername = "testuser";
        var postId = 123;

        // Act
        var content = _builder.BuildRepostNotification(reposterUsername, postId);

        // Assert
        Assert.Equal("New Repost", content.Title);
        Assert.Equal("@testuser reposted your post", content.Body);
        Assert.Equal("repost", content.NotificationType);
        Assert.Equal("repost", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
        Assert.Equal("testuser", content.Data["reposterUsername"]);
    }

    [Fact]
    public void BuildSystemMessageNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var title = "System Alert";
        var message = "This is a system message";
        var additionalData = new Dictionary<string, string> { ["priority"] = "high" };

        // Act
        var content = _builder.BuildSystemMessageNotification(title, message, additionalData);

        // Assert
        Assert.Equal("System Alert", content.Title);
        Assert.Equal("This is a system message", content.Body);
        Assert.Equal("systemMessage", content.NotificationType);
        Assert.Equal("systemMessage", content.Data["type"]);
        Assert.Equal("high", content.Data["priority"]);
    }

    [Fact]
    public void BuildVideoProcessingCompletedNotification_ShouldReturnConsistentContent()
    {
        // Arrange
        var postId = 123;

        // Act
        var content = _builder.BuildVideoProcessingCompletedNotification(postId);

        // Assert
        Assert.Equal("Video Ready", content.Title);
        Assert.Equal("Your video has been processed and is now available", content.Body);
        Assert.Equal("VideoProcessingCompleted", content.NotificationType);
        Assert.Equal("VideoProcessingCompleted", content.Data["type"]);
        Assert.Equal("123", content.Data["postId"]);
    }

    [Fact]
    public void BuildTestNotification_ShouldReturnConsistentContent()
    {
        // Act
        var content = _builder.BuildTestNotification();

        // Assert
        Assert.Equal("Test", content.Title);
        Assert.Equal("Test notification", content.Body);
        Assert.Equal("test", content.NotificationType);
        Assert.Equal("test", content.Data["type"]);
    }

    [Fact]
    public void BuildTestNotification_WithCustomMessage_ShouldReturnConsistentContent()
    {
        // Arrange
        var customMessage = "Custom test message";

        // Act
        var content = _builder.BuildTestNotification(customMessage);

        // Assert
        Assert.Equal("Test", content.Title);
        Assert.Equal("Custom test message", content.Body);
        Assert.Equal("test", content.NotificationType);
        Assert.Equal("test", content.Data["type"]);
    }

    [Fact]
    public void WithData_ShouldAddDataCorrectly()
    {
        // Arrange
        var content = _builder.BuildTestNotification();

        // Act
        content.WithData("customKey", "customValue");

        // Assert
        Assert.Equal("customValue", content.Data["customKey"]);
        Assert.Equal("test", content.Data["type"]); // Should preserve existing data
    }

    [Fact]
    public void WithData_Dictionary_ShouldAddMultipleDataCorrectly()
    {
        // Arrange
        var content = _builder.BuildTestNotification();
        var additionalData = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        content.WithData(additionalData);

        // Assert
        Assert.Equal("value1", content.Data["key1"]);
        Assert.Equal("value2", content.Data["key2"]);
        Assert.Equal("test", content.Data["type"]); // Should preserve existing data
    }
}
