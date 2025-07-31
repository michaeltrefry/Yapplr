using Yapplr.Api.Services.EmailTemplates;

namespace Yapplr.Api.Services.Notifications;

public interface INotificationDigestService
{
    /// <summary>
    /// Generate and send digest emails for users who have digest enabled
    /// </summary>
    Task ProcessDigestEmailsAsync(int frequencyHours);

    /// <summary>
    /// Generate digest for a specific user
    /// </summary>
    Task<List<DigestNotification>> GenerateUserDigestAsync(int userId, DateTime periodStart, DateTime periodEnd);

    /// <summary>
    /// Send digest email to a specific user
    /// </summary>
    Task SendDigestEmailAsync(int userId, List<DigestNotification> notifications, DateTime periodStart, DateTime periodEnd);

    /// <summary>
    /// Check if user should receive digest email
    /// </summary>
    Task<bool> ShouldSendDigestAsync(int userId, int frequencyHours);
}
