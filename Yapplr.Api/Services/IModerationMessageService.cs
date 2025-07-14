namespace Yapplr.Api.Services;

public interface IModerationMessageService
{
    Task SendContentHiddenMessageAsync(int userId, string contentType, int contentId, string content, string reason, string moderatorUsername);
    Task SendContentDeletedMessageAsync(int userId, string contentType, int contentId, string content, string reason, string moderatorUsername);
    Task SendUserSuspensionMessageAsync(int userId, string reason, DateTime? suspendedUntil, string moderatorUsername);
    Task SendUserBanMessageAsync(int userId, string reason, string moderatorUsername);
    Task SendReportActionTakenMessageAsync(int reportingUserId, string contentType, int contentId, string contentPreview, string reason, string moderatorUsername);
    Task SendReportDismissedMessageAsync(int reportingUserId, string contentType, int contentId, string contentPreview, string moderatorUsername);
}