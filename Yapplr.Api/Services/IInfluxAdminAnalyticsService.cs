using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for admin analytics using InfluxDB as the data source
/// </summary>
public interface IInfluxAdminAnalyticsService
{
    /// <summary>
    /// Check if InfluxDB is available and accessible
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get user growth statistics from InfluxDB
    /// </summary>
    Task<UserGrowthStatsDto> GetUserGrowthStatsAsync(int days = 30);

    /// <summary>
    /// Get content creation statistics from InfluxDB
    /// </summary>
    Task<ContentStatsDto> GetContentStatsAsync(int days = 30);

    /// <summary>
    /// Get moderation trends from InfluxDB
    /// </summary>
    Task<ModerationTrendsDto> GetModerationTrendsAsync(int days = 30);

    /// <summary>
    /// Get system health metrics from InfluxDB
    /// </summary>
    Task<SystemHealthDto> GetSystemHealthAsync();

    /// <summary>
    /// Get top moderators statistics from InfluxDB
    /// </summary>
    Task<TopModeratorsDto> GetTopModeratorsAsync(int days = 30, int limit = 10);

    /// <summary>
    /// Get content trends from InfluxDB
    /// </summary>
    Task<ContentTrendsDto> GetContentTrendsAsync(int days = 30);

    /// <summary>
    /// Get user engagement statistics from InfluxDB
    /// </summary>
    Task<UserEngagementStatsDto> GetUserEngagementStatsAsync(int days = 30);

    /// <summary>
    /// Get analytics data source information
    /// </summary>
    Task<AnalyticsDataSourceDto> GetDataSourceInfoAsync();
}
