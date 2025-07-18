using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Yapplr.Api.Services;
using Yapplr.Api.Services.EmailTemplates;

namespace Yapplr.Api.Tests;

public class EmailServiceTests
{
    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldCallEmailSender_WithCorrectParameters()
    {
        // Arrange
        var mockEmailSender = new Mock<IEmailSender>();
        var mockLogger = new Mock<ILogger<EmailService>>();
        
        mockEmailSender.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync(true);

        var emailService = new EmailService(mockEmailSender.Object, mockLogger.Object);

        var toEmail = "test@example.com";
        var username = "testuser";
        var resetToken = "reset123";
        var resetUrl = "https://example.com/reset?token=reset123";

        // Act
        var result = await emailService.SendPasswordResetEmailAsync(toEmail, username, resetToken, resetUrl);

        // Assert
        Assert.True(result);
        mockEmailSender.Verify(x => x.SendEmailAsync(
            toEmail,
            "Reset Your Yapplr Password",
            It.Is<string>(html => html.Contains(username) && html.Contains(resetUrl)),
            It.Is<string>(text => text.Contains(username) && text.Contains(resetUrl))
        ), Times.Once);
    }

    [Fact]
    public async Task SendEmailVerificationAsync_ShouldCallEmailSender_WithCorrectParameters()
    {
        // Arrange
        var mockEmailSender = new Mock<IEmailSender>();
        var mockLogger = new Mock<ILogger<EmailService>>();
        
        mockEmailSender.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync(true);

        var emailService = new EmailService(mockEmailSender.Object, mockLogger.Object);

        var toEmail = "test@example.com";
        var username = "testuser";
        var verificationToken = "verify123";
        var verificationUrl = "https://example.com/verify?token=verify123";

        // Act
        var result = await emailService.SendEmailVerificationAsync(toEmail, username, verificationToken, verificationUrl);

        // Assert
        Assert.True(result);
        mockEmailSender.Verify(x => x.SendEmailAsync(
            toEmail,
            "Verify Your Yapplr Email Address",
            It.Is<string>(html => html.Contains(username) && html.Contains(verificationToken) && html.Contains(verificationUrl)),
            It.Is<string>(text => text.Contains(username) && text.Contains(verificationToken) && text.Contains(verificationUrl))
        ), Times.Once);
    }

    [Fact]
    public async Task SendUserSuspensionEmailAsync_ShouldCallEmailSender_WithCorrectParameters()
    {
        // Arrange
        var mockEmailSender = new Mock<IEmailSender>();
        var mockLogger = new Mock<ILogger<EmailService>>();
        
        mockEmailSender.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync(true);

        var emailService = new EmailService(mockEmailSender.Object, mockLogger.Object);

        var toEmail = "test@example.com";
        var username = "testuser";
        var reason = "Violation of community guidelines";
        var suspendedUntil = DateTime.UtcNow.AddDays(7);
        var moderatorUsername = "moderator";
        var appealUrl = "https://example.com/appeal";

        // Act
        var result = await emailService.SendUserSuspensionEmailAsync(toEmail, username, reason, suspendedUntil, moderatorUsername, appealUrl);

        // Assert
        Assert.True(result);
        mockEmailSender.Verify(x => x.SendEmailAsync(
            toEmail,
            "Important: Your Yapplr Account Has Been Suspended",
            It.Is<string>(html => html.Contains(username) && html.Contains(reason) && html.Contains(moderatorUsername) && html.Contains(appealUrl)),
            It.Is<string>(text => text.Contains(username) && text.Contains(reason) && text.Contains(moderatorUsername) && text.Contains(appealUrl))
        ), Times.Once);
    }

    [Fact]
    public void PasswordResetEmailTemplate_ShouldGenerateCorrectContent()
    {
        // Arrange
        var username = "testuser";
        var resetUrl = "https://example.com/reset?token=reset123";
        var template = new PasswordResetEmailTemplate(username, resetUrl);

        // Act
        var htmlBody = template.GenerateHtmlBody();
        var textBody = template.GenerateTextBody();

        // Assert
        Assert.Equal("Reset Your Yapplr Password", template.Subject);
        Assert.Contains(username, htmlBody);
        Assert.Contains(resetUrl, htmlBody);
        Assert.Contains("<!DOCTYPE html>", htmlBody);
        Assert.Contains(username, textBody);
        Assert.Contains(resetUrl, textBody);
        Assert.DoesNotContain("<!DOCTYPE html>", textBody);
    }

    [Fact]
    public void EmailVerificationTemplate_ShouldGenerateCorrectContent()
    {
        // Arrange
        var username = "testuser";
        var verificationToken = "verify123";
        var verificationUrl = "https://example.com/verify?token=verify123";
        var template = new EmailVerificationTemplate(username, verificationToken, verificationUrl);

        // Act
        var htmlBody = template.GenerateHtmlBody();
        var textBody = template.GenerateTextBody();

        // Assert
        Assert.Equal("Verify Your Yapplr Email Address", template.Subject);
        Assert.Contains(username, htmlBody);
        Assert.Contains(verificationToken, htmlBody);
        Assert.Contains(verificationUrl, htmlBody);
        Assert.Contains("<!DOCTYPE html>", htmlBody);
        Assert.Contains(username, textBody);
        Assert.Contains(verificationToken, textBody);
        Assert.Contains(verificationUrl, textBody);
        Assert.DoesNotContain("<!DOCTYPE html>", textBody);
    }

    [Fact]
    public void UserSuspensionEmailTemplate_ShouldGenerateCorrectContent()
    {
        // Arrange
        var username = "testuser";
        var reason = "Violation of community guidelines";
        var suspendedUntil = DateTime.UtcNow.AddDays(7);
        var moderatorUsername = "moderator";
        var appealUrl = "https://example.com/appeal";
        var template = new UserSuspensionEmailTemplate(username, reason, suspendedUntil, moderatorUsername, appealUrl);

        // Act
        var htmlBody = template.GenerateHtmlBody();
        var textBody = template.GenerateTextBody();

        // Assert
        Assert.Equal("Important: Your Yapplr Account Has Been Suspended", template.Subject);
        Assert.Contains(username, htmlBody);
        Assert.Contains(reason, htmlBody);
        Assert.Contains(moderatorUsername, htmlBody);
        Assert.Contains(appealUrl, htmlBody);
        Assert.Contains("<!DOCTYPE html>", htmlBody);
        Assert.Contains(username, textBody);
        Assert.Contains(reason, textBody);
        Assert.Contains(moderatorUsername, textBody);
        Assert.Contains(appealUrl, textBody);
        Assert.DoesNotContain("<!DOCTYPE html>", textBody);
    }

    [Fact]
    public void NotificationEmailTemplate_ShouldGenerateCorrectContent()
    {
        // Arrange
        var username = "testuser";
        var title = "You have a new like!";
        var body = "Someone liked your post about technology.";
        var notificationType = "like";
        var actionUrl = "https://yapplr.com/post/123";
        var template = new NotificationEmailTemplate(username, title, body, notificationType, actionUrl);

        // Act
        var htmlBody = template.GenerateHtmlBody();
        var textBody = template.GenerateTextBody();

        // Assert
        Assert.Equal(title, template.Subject);
        Assert.Contains("testuser", htmlBody);
        Assert.Contains("You have a new like!", htmlBody);
        Assert.Contains("Someone liked your post about technology.", htmlBody);
        Assert.Contains("❤️", htmlBody); // Like icon
        Assert.Contains("https://yapplr.com/post/123", htmlBody);
        Assert.Contains("View on Yapplr", htmlBody);

        Assert.Contains("testuser", textBody);
        Assert.Contains("You have a new like!", textBody);
        Assert.Contains("Someone liked your post about technology.", textBody);
        Assert.Contains("https://yapplr.com/post/123", textBody);
    }

    [Fact]
    public void NotificationEmailTemplate_WithoutActionUrl_ShouldNotIncludeButton()
    {
        // Arrange
        var template = new NotificationEmailTemplate("testuser", "System Message", "Your account has been updated.", "system");

        // Act
        var htmlBody = template.GenerateHtmlBody();
        var textBody = template.GenerateTextBody();

        // Assert
        Assert.DoesNotContain("View on Yapplr", htmlBody);
        Assert.Contains("🔔", htmlBody); // System notification icon
        Assert.DoesNotContain("View on Yapplr:", textBody);
    }

    [Theory]
    [InlineData("message", "💬")]
    [InlineData("mention", "🏷️")]
    [InlineData("reply", "↩️")]
    [InlineData("comment", "💭")]
    [InlineData("follow", "👥")]
    [InlineData("like", "❤️")]
    [InlineData("repost", "🔄")]
    [InlineData("follow_request", "👋")]
    [InlineData("system", "🔔")]
    [InlineData("unknown", "🔔")]
    public void NotificationEmailTemplate_ShouldUseCorrectIcon(string notificationType, string expectedIcon)
    {
        // Arrange
        var template = new NotificationEmailTemplate("testuser", "Test", "Test message", notificationType);

        // Act
        var htmlBody = template.GenerateHtmlBody();

        // Assert
        Assert.Contains(expectedIcon, htmlBody);
    }
}
