using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for managing user notification preferences
/// </summary>
public interface INotificationPreferencesService
{
    Task<NotificationPreferences> GetUserPreferencesAsync(int userId);
    Task<NotificationPreferences> UpdateUserPreferencesAsync(int userId, UpdateNotificationPreferencesDto updateDto);
    Task<bool> ShouldSendNotificationAsync(int userId, string notificationType);
    Task<NotificationDeliveryMethod> GetPreferredDeliveryMethodAsync(int userId, string notificationType);
    Task<bool> IsInQuietHoursAsync(int userId);
    Task<bool> HasReachedFrequencyLimitAsync(int userId);
    Task RecordNotificationSentAsync(int userId);
}