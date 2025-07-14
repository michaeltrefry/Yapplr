namespace Yapplr.Api.Services;

/// <summary>
/// Service for filtering and validating notification content
/// </summary>
public interface INotificationContentFilterService
{
    Task<ContentValidationResult> ValidateContentAsync(string content, string contentType = "text");
    Task<ContentValidationResult> ValidateNotificationAsync(string title, string body, Dictionary<string, string>? data = null);
    Task<bool> IsContentSafeAsync(string content);
    Task<string> SanitizeContentAsync(string content);
    Task<List<string>> DetectSuspiciousLinksAsync(string content);
    Task<Dictionary<string, object>> GetFilterStatsAsync();
    Task UpdateFilterConfigAsync(ContentFilterConfig config);
}