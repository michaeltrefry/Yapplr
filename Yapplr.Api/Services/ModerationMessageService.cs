using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IModerationMessageService
{
    Task SendContentHiddenMessageAsync(int userId, string contentType, int contentId, string content, string reason, string moderatorUsername);
    Task SendContentDeletedMessageAsync(int userId, string contentType, int contentId, string content, string reason, string moderatorUsername);
    Task SendUserSuspensionMessageAsync(int userId, string reason, DateTime? suspendedUntil, string moderatorUsername);
    Task SendUserBanMessageAsync(int userId, string reason, string moderatorUsername);
}

public class ModerationMessageService : IModerationMessageService
{
    private readonly IMessageService _messageService;
    private readonly ILogger<ModerationMessageService> _logger;

    public ModerationMessageService(
        IMessageService messageService,
        ILogger<ModerationMessageService> logger)
    {
        _messageService = messageService;
        _logger = logger;
    }

    public async Task SendContentHiddenMessageAsync(int userId, string contentType, int contentId, string content, string reason, string moderatorUsername)
    {
        var contentPreview = TruncateContent(content, 100);
        var appealInfo = GetAppealInformation(contentType, contentId);
        
        var message = $@"Hello,

Your {contentType} has been hidden by our moderation team.

**Content:** {contentPreview}

**Reason:** {reason}

**Moderator:** @{moderatorUsername}

**What this means:**
• Your {contentType} is no longer visible to other users
• This action was taken to maintain community standards
• You can still see your {contentType} in your profile

**Think this was a mistake?**
{appealInfo}

If you have questions about our community guidelines, please review our Terms of Service.

Best regards,
The Yapplr Moderation Team";

        try
        {
            await _messageService.SendSystemMessageAsync(userId, message);
            _logger.LogInformation("Sent content hidden message to user {UserId} for {ContentType} {ContentId}", userId, contentType, contentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send content hidden message to user {UserId}", userId);
        }
    }

    public async Task SendContentDeletedMessageAsync(int userId, string contentType, int contentId, string content, string reason, string moderatorUsername)
    {
        var contentPreview = TruncateContent(content, 100);
        var appealInfo = GetAppealInformation(contentType, contentId);
        
        var message = $@"Hello,

Your {contentType} has been removed by our moderation team.

**Content:** {contentPreview}

**Reason:** {reason}

**Moderator:** @{moderatorUsername}

**What this means:**
• Your {contentType} has been permanently removed
• This action was taken due to a violation of our community guidelines
• The content cannot be restored

**Think this was a mistake?**
{appealInfo}

Please review our community guidelines to avoid future violations.

Best regards,
The Yapplr Moderation Team";

        try
        {
            await _messageService.SendSystemMessageAsync(userId, message);
            _logger.LogInformation("Sent content deleted message to user {UserId} for {ContentType} {ContentId}", userId, contentType, contentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send content deleted message to user {UserId}", userId);
        }
    }

    public async Task SendUserSuspensionMessageAsync(int userId, string reason, DateTime? suspendedUntil, string moderatorUsername)
    {
        var suspensionDuration = suspendedUntil.HasValue 
            ? $"until {suspendedUntil.Value:yyyy-MM-dd HH:mm} UTC"
            : "indefinitely";

        var message = $@"Hello,

Your account has been suspended {suspensionDuration}.

**Reason:** {reason}

**Moderator:** @{moderatorUsername}

**What this means:**
• You cannot post, comment, or interact with content
• Your existing content remains visible
• You can still view content but cannot participate

**Think this was a mistake?**
You can appeal this suspension by visiting the Appeals page in your account settings.

Please review our community guidelines before your suspension ends.

Best regards,
The Yapplr Moderation Team";

        try
        {
            await _messageService.SendSystemMessageAsync(userId, message);
            _logger.LogInformation("Sent suspension message to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send suspension message to user {UserId}", userId);
        }
    }

    public async Task SendUserBanMessageAsync(int userId, string reason, string moderatorUsername)
    {
        var message = $@"Hello,

Your account has been permanently banned from Yapplr.

**Reason:** {reason}

**Moderator:** @{moderatorUsername}

**What this means:**
• Your account access has been permanently revoked
• Your content may be hidden or removed
• You cannot create new accounts

**Think this was a mistake?**
You can appeal this ban by visiting the Appeals page. Please note that ban appeals are reviewed carefully and may take time to process.

Best regards,
The Yapplr Moderation Team";

        try
        {
            await _messageService.SendSystemMessageAsync(userId, message);
            _logger.LogInformation("Sent ban message to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ban message to user {UserId}", userId);
        }
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return "[No content]";

        if (content.Length <= maxLength)
            return content;

        return content.Substring(0, maxLength) + "...";
    }

    private static string GetAppealInformation(string contentType, int contentId)
    {
        return $@"You can appeal this decision by:
1. Going to your Account Settings
2. Clicking on ""Appeals""
3. Selecting ""Appeal Content Moderation""
4. Referencing {contentType} #{contentId}

Appeals are reviewed by our moderation team and you'll receive a response within 48 hours.";
    }
}
