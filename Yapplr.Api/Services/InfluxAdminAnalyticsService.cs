using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models.Analytics;

namespace Yapplr.Api.Services;

/// <summary>
/// InfluxDB implementation of admin analytics service
/// Provides admin dashboard analytics using InfluxDB instead of database queries
/// </summary>
public class InfluxAdminAnalyticsService : IInfluxAdminAnalyticsService
{
    private readonly IInfluxDBClient _influxClient;
    private readonly ILogger<InfluxAdminAnalyticsService> _logger;
    private readonly string _bucket;
    private readonly string _organization;
    private readonly bool _isEnabled;

    public InfluxAdminAnalyticsService(
        IInfluxDBClient influxClient,
        ILogger<InfluxAdminAnalyticsService> logger,
        IConfiguration configuration)
    {
        _influxClient = influxClient;
        _logger = logger;
        _bucket = configuration.GetValue<string>("InfluxDB:Bucket", "analytics")!;
        _organization = configuration.GetValue<string>("InfluxDB:Organization", "yapplr")!;
        _isEnabled = configuration.GetValue<bool>("InfluxDB:Enabled", true);

        _logger.LogInformation("InfluxDB Admin Analytics Service initialized. Enabled: {IsEnabled}", _isEnabled);
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (!_isEnabled) return false;

        try
        {
            var health = await _influxClient.HealthAsync();
            return health.Status == HealthCheck.StatusEnum.Pass;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InfluxDB health check failed");
            return false;
        }
    }

    public async Task<UserGrowthStatsDto> GetUserGrowthStatsAsync(int days = 30)
    {
        try
        {
            var query = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""UserRegistered"")
                  |> filter(fn: (r) => r._field == ""count"")
                  |> aggregateWindow(every: 1d, fn: sum, createEmpty: false)
                  |> yield(name: ""daily_registrations"")";

            var queryApi = _influxClient.GetQueryApi();
            var tables = await queryApi.QueryAsync(query, _organization);

            var dailyStats = new List<DailyStatsDto>();
            var totalNewUsers = 0;

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var date = ((DateTime)record.GetTime()!).Date;
                    var count = Convert.ToInt32(record.GetValue());
                    
                    dailyStats.Add(new DailyStatsDto
                    {
                        Date = date,
                        Count = count,
                        Label = date.ToString("MMM dd")
                    });
                    
                    totalNewUsers += count;
                }
            }

            // Calculate growth rate (simplified - you might want to make this more sophisticated)
            var growthRate = dailyStats.Count > 1 ? 
                ((double)(dailyStats.TakeLast(7).Sum(d => d.Count) - dailyStats.Take(7).Sum(d => d.Count)) / 
                 Math.Max(dailyStats.Take(7).Sum(d => d.Count), 1)) * 100 : 0;

            return new UserGrowthStatsDto
            {
                TotalNewUsers = totalNewUsers,
                TotalActiveUsers = await GetActiveUsersCountAsync(days),
                GrowthRate = (float)growthRate,
                DailyStats = dailyStats.OrderBy(d => d.Date).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user growth stats from InfluxDB");
            return new UserGrowthStatsDto
            {
                TotalNewUsers = 0,
                TotalActiveUsers = 0,
                GrowthRate = 0,
                DailyStats = new List<DailyStatsDto>()
            };
        }
    }

    public async Task<ContentStatsDto> GetContentStatsAsync(int days = 30)
    {
        try
        {
            // Query for post creation activities
            var postQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""PostCreated"")
                  |> filter(fn: (r) => r._field == ""count"")
                  |> aggregateWindow(every: 1d, fn: sum, createEmpty: false)";

            // Query for comment creation activities
            var commentQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""CommentCreated"")
                  |> filter(fn: (r) => r._field == ""count"")
                  |> aggregateWindow(every: 1d, fn: sum, createEmpty: false)";

            var queryApi = _influxClient.GetQueryApi();
            
            var postTables = await queryApi.QueryAsync(postQuery, _organization);
            var commentTables = await queryApi.QueryAsync(commentQuery, _organization);

            var dailyPosts = ExtractDailyStats(postTables);
            var dailyComments = ExtractDailyStats(commentTables);

            var totalPosts = dailyPosts.Sum(d => d.Count);
            var totalComments = dailyComments.Sum(d => d.Count);
            var averagePostsPerDay = days > 0 ? totalPosts / days : 0;
            var averageCommentsPerDay = days > 0 ? totalComments / days : 0;

            return new ContentStatsDto
            {
                TotalPosts = totalPosts,
                TotalComments = totalComments,
                AveragePostsPerDay = averagePostsPerDay,
                AverageCommentsPerDay = averageCommentsPerDay,
                DailyPosts = dailyPosts,
                DailyComments = dailyComments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content stats from InfluxDB");
            return new ContentStatsDto
            {
                TotalPosts = 0,
                TotalComments = 0,
                AveragePostsPerDay = 0,
                AverageCommentsPerDay = 0,
                DailyPosts = new List<DailyStatsDto>(),
                DailyComments = new List<DailyStatsDto>()
            };
        }
    }

    public async Task<ModerationTrendsDto> GetModerationTrendsAsync(int days = 30)
    {
        try
        {
            // Query for moderation activities
            var query = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""ContentModerated"" or r.activity_type == ""UserSuspended"" or r.activity_type == ""ContentHidden"")
                  |> filter(fn: (r) => r._field == ""count"")
                  |> aggregateWindow(every: 1d, fn: sum, createEmpty: false)";

            var queryApi = _influxClient.GetQueryApi();
            var tables = await queryApi.QueryAsync(query, _organization);

            var dailyActions = ExtractDailyStats(tables);
            var totalActions = dailyActions.Sum(d => d.Count);

            // For action breakdown, you'd need separate queries for each action type
            var actionBreakdown = new List<ActionBreakdownDto>
            {
                new() { Action = "ContentModerated", Count = totalActions / 3 }, // Simplified
                new() { Action = "UserSuspended", Count = totalActions / 3 },
                new() { Action = "ContentHidden", Count = totalActions / 3 }
            };

            return new ModerationTrendsDto
            {
                TotalActions = totalActions,
                DailyActions = dailyActions,
                ActionBreakdown = actionBreakdown
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get moderation trends from InfluxDB");
            return new ModerationTrendsDto
            {
                TotalActions = 0,
                DailyActions = new List<DailyStatsDto>(),
                ActionBreakdown = new List<ActionBreakdownDto>()
            };
        }
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        try
        {
            // Query for performance metrics
            var query = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -1h)
                  |> filter(fn: (r) => r._measurement == ""performance_metrics"")
                  |> filter(fn: (r) => r.metric_type == ""ResponseTime"")
                  |> filter(fn: (r) => r._field == ""value"")
                  |> mean()";

            var queryApi = _influxClient.GetQueryApi();
            var tables = await queryApi.QueryAsync(query, _organization);

            var avgResponseTime = 0.0;
            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    avgResponseTime = Convert.ToDouble(record.GetValue());
                    break;
                }
            }

            return new SystemHealthDto
            {
                IsHealthy = avgResponseTime < 1000, // Consider healthy if avg response time < 1s
                AverageResponseTime = avgResponseTime,
                ActiveConnections = 0, // Would need separate query
                QueueDepth = 0, // Would need separate query
                ErrorRate = 0, // Would need separate query
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health from InfluxDB");
            return new SystemHealthDto
            {
                IsHealthy = false,
                AverageResponseTime = 0,
                ActiveConnections = 0,
                QueueDepth = 0,
                ErrorRate = 0,
                LastChecked = DateTime.UtcNow
            };
        }
    }

    public async Task<TopModeratorsDto> GetTopModeratorsAsync(int days = 30, int limit = 10)
    {
        // This would require user_id to be stored in the analytics data
        // For now, return empty data
        return new TopModeratorsDto
        {
            Moderators = new List<ModeratorStatsDto>()
        };
    }

    public async Task<ContentTrendsDto> GetContentTrendsAsync(int days = 30)
    {
        try
        {
            // Query for content engagement trends
            var query = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""content_engagement"")
                  |> filter(fn: (r) => r._field == ""count"")
                  |> group(columns: [""engagement_type""])
                  |> sum()";

            var queryApi = _influxClient.GetQueryApi();
            var tables = await queryApi.QueryAsync(query, _organization);

            var trends = new List<ContentTrendDto>();
            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var engagementType = record.GetValueByKey("engagement_type")?.ToString() ?? "Unknown";
                    var count = Convert.ToInt32(record.GetValue());
                    
                    trends.Add(new ContentTrendDto
                    {
                        Type = engagementType,
                        Count = count,
                        Percentage = 0 // Calculate after getting all data
                    });
                }
            }

            // Calculate percentages
            var total = trends.Sum(t => t.Count);
            if (total > 0)
            {
                foreach (var trend in trends)
                {
                    trend.Percentage = (float)(trend.Count * 100.0 / total);
                }
            }

            return new ContentTrendsDto
            {
                Trends = trends
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content trends from InfluxDB");
            return new ContentTrendsDto
            {
                Trends = new List<ContentTrendDto>()
            };
        }
    }

    public async Task<UserEngagementStatsDto> GetUserEngagementStatsAsync(int days = 30)
    {
        try
        {
            // Query for user engagement activities
            var query = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""Login"" or r.activity_type == ""PostCreated"" or r.activity_type == ""CommentCreated"")
                  |> filter(fn: (r) => r._field == ""count"")
                  |> aggregateWindow(every: 1d, fn: sum, createEmpty: false)";

            var queryApi = _influxClient.GetQueryApi();
            var tables = await queryApi.QueryAsync(query, _organization);

            var dailyEngagement = ExtractDailyStats(tables);
            var totalEngagements = dailyEngagement.Sum(d => d.Count);
            var averageEngagementsPerDay = days > 0 ? totalEngagements / days : 0;

            return new UserEngagementStatsDto
            {
                TotalEngagements = totalEngagements,
                AverageEngagementsPerDay = averageEngagementsPerDay,
                DailyEngagement = dailyEngagement
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user engagement stats from InfluxDB");
            return new UserEngagementStatsDto
            {
                TotalEngagements = 0,
                AverageEngagementsPerDay = 0,
                DailyEngagement = new List<DailyStatsDto>()
            };
        }
    }

    private async Task<int> GetActiveUsersCountAsync(int days)
    {
        try
        {
            var query = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""Login"")
                  |> filter(fn: (r) => r._field == ""count"")
                  |> group(columns: [""user_id""])
                  |> sum()
                  |> group()
                  |> count()";

            var queryApi = _influxClient.GetQueryApi();
            var tables = await queryApi.QueryAsync(query, _organization);

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    return Convert.ToInt32(record.GetValue());
                }
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private List<DailyStatsDto> ExtractDailyStats(List<FluxTable> tables)
    {
        var dailyStats = new List<DailyStatsDto>();

        foreach (var table in tables)
        {
            foreach (var record in table.Records)
            {
                var date = ((DateTime)record.GetTime()!).Date;
                var count = Convert.ToInt32(record.GetValue());
                
                dailyStats.Add(new DailyStatsDto
                {
                    Date = date,
                    Count = count,
                    Label = date.ToString("MMM dd")
                });
            }
        }

        return dailyStats.OrderBy(d => d.Date).ToList();
    }
}
