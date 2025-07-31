using InfluxDB.Client;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services.Analytics;

/// <summary>
/// Admin analytics service using InfluxDB as the data source
/// </summary>
public class InfluxAdminAnalyticsService : IInfluxAdminAnalyticsService
{
    private readonly InfluxDBClient _influxClient;
    private readonly ILogger<InfluxAdminAnalyticsService> _logger;
    private readonly string _bucket;
    private readonly string _organization;
    private readonly bool _isEnabled;

    public InfluxAdminAnalyticsService(
        IConfiguration configuration,
        ILogger<InfluxAdminAnalyticsService> logger)
    {
        _logger = logger;
        
        var influxConfig = configuration.GetSection("InfluxDB");
        var url = influxConfig["Url"] ?? "http://localhost:8086";
        var token = influxConfig["Token"] ?? "";
        _bucket = influxConfig["Bucket"] ?? "analytics";
        _organization = influxConfig["Organization"] ?? "yapplr";
        _isEnabled = influxConfig.GetValue<bool>("Enabled", false);

        if (_isEnabled && !string.IsNullOrEmpty(token))
        {
            _influxClient = new InfluxDBClient(url, token);
            _logger.LogInformation("InfluxAdminAnalyticsService initialized with URL: {Url}", url);
        }
        else
        {
            _influxClient = null!;
            _logger.LogWarning("InfluxAdminAnalyticsService disabled - InfluxDB not configured");
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (!_isEnabled || _influxClient == null) return false;

        try
        {
            return await _influxClient.PingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InfluxDB health check failed");
            return false;
        }
    }

    public async Task<UserGrowthStatsDto> GetUserGrowthStatsAsync(int days = 30)
    {
        if (!_isEnabled || _influxClient == null)
        {
            return new UserGrowthStatsDto
            {
                TotalNewUsers = 0,
                TotalActiveUsers = 0,
                GrowthRate = 0,
                DailyStats = new List<DailyStatsDto>()
            };
        }

        try
        {
            var queryApi = _influxClient.GetQueryApi();
            
            // Query for user registrations (new users)
            var newUsersQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""Register"")
                  |> aggregateWindow(every: 1d, fn: count, createEmpty: false)
                  |> yield(name: ""new_users"")";

            // Query for active users
            var activeUsersQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""Login"")
                  |> aggregateWindow(every: 1d, fn: count, createEmpty: false)
                  |> yield(name: ""active_users"")";

            var newUsersResult = await queryApi.QueryAsync(newUsersQuery, _organization);
            var activeUsersResult = await queryApi.QueryAsync(activeUsersQuery, _organization);

            var dailyStats = new List<DailyStatsDto>();
            var totalNewUsers = 0;
            var totalActiveUsers = 0;

            // Process new users data
            foreach (var table in newUsersResult)
            {
                foreach (var record in table.Records)
                {
                    var date = record.GetTime()?.ToDateTimeUtc().Date ?? DateTime.UtcNow.Date;
                    var count = Convert.ToInt32(record.GetValue());
                    totalNewUsers += count;
                    
                    dailyStats.Add(new DailyStatsDto
                    {
                        Date = date,
                        Count = count,
                        Label = "New Users"
                    });
                }
            }

            // Process active users data
            foreach (var table in activeUsersResult)
            {
                foreach (var record in table.Records)
                {
                    var date = record.GetTime()?.ToDateTimeUtc().Date ?? DateTime.UtcNow.Date;
                    var count = Convert.ToInt32(record.GetValue());
                    totalActiveUsers += count;
                    
                    // Add to existing stats or create new entry
                    var existingStat = dailyStats.FirstOrDefault(s => s.Date.Date == date && s.Label == "Active Users");
                    if (existingStat == null)
                    {
                        dailyStats.Add(new DailyStatsDto
                        {
                            Date = date,
                            Count = count,
                            Label = "Active Users"
                        });
                    }
                }
            }

            // Calculate growth rate from new users data
            var newUsersData = dailyStats.Where(s => s.Label == "New Users").Select(s => s.Count).ToList();
            var growthRate = CalculateGrowthRate(newUsersData);

            return new UserGrowthStatsDto
            {
                TotalNewUsers = totalNewUsers,
                TotalActiveUsers = totalActiveUsers,
                GrowthRate = growthRate,
                DailyStats = dailyStats.OrderBy(s => s.Date).ToList()
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
        if (!_isEnabled || _influxClient == null)
        {
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

        try
        {
            var queryApi = _influxClient.GetQueryApi();
            
            // Query for post creation
            var postsQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""CreatePost"")
                  |> aggregateWindow(every: 1d, fn: count, createEmpty: false)
                  |> yield(name: ""posts"")";

            // Query for comment creation
            var commentsQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""CreateComment"")
                  |> aggregateWindow(every: 1d, fn: count, createEmpty: false)
                  |> yield(name: ""comments"")";

            var postsResult = await queryApi.QueryAsync(postsQuery, _organization);
            var commentsResult = await queryApi.QueryAsync(commentsQuery, _organization);

            var dailyPosts = new List<DailyStatsDto>();
            var dailyComments = new List<DailyStatsDto>();
            var totalPosts = 0;
            var totalComments = 0;

            // Process posts data
            foreach (var table in postsResult)
            {
                foreach (var record in table.Records)
                {
                    var date = record.GetTime()?.ToDateTimeUtc().Date ?? DateTime.UtcNow.Date;
                    var count = Convert.ToInt32(record.GetValue());
                    totalPosts += count;
                    
                    dailyPosts.Add(new DailyStatsDto
                    {
                        Date = date,
                        Count = count
                    });
                }
            }

            // Process comments data
            foreach (var table in commentsResult)
            {
                foreach (var record in table.Records)
                {
                    var date = record.GetTime()?.ToDateTimeUtc().Date ?? DateTime.UtcNow.Date;
                    var count = Convert.ToInt32(record.GetValue());
                    totalComments += count;
                    
                    dailyComments.Add(new DailyStatsDto
                    {
                        Date = date,
                        Count = count
                    });
                }
            }

            return new ContentStatsDto
            {
                TotalPosts = totalPosts,
                TotalComments = totalComments,
                AveragePostsPerDay = days > 0 ? (int)Math.Round((double)totalPosts / days) : 0,
                AverageCommentsPerDay = days > 0 ? (int)Math.Round((double)totalComments / days) : 0,
                DailyPosts = dailyPosts.OrderBy(s => s.Date).ToList(),
                DailyComments = dailyComments.OrderBy(s => s.Date).ToList()
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
        if (!_isEnabled || _influxClient == null)
        {
            return new ModerationTrendsDto
            {
                TotalActions = 0,
                DailyActions = new List<DailyStatsDto>(),
                ActionBreakdown = new List<ActionBreakdownDto>()
            };
        }

        try
        {
            var queryApi = _influxClient.GetQueryApi();

            // Query for moderation actions
            var moderationQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type =~ /Hide|Ban|Suspend|Moderate/)
                  |> aggregateWindow(every: 1d, fn: count, createEmpty: false)
                  |> yield(name: ""moderation"")";

            var result = await queryApi.QueryAsync(moderationQuery, _organization);

            var dailyActions = new List<DailyStatsDto>();
            var totalActions = 0;

            foreach (var table in result)
            {
                foreach (var record in table.Records)
                {
                    var date = record.GetTime()?.ToDateTimeUtc().Date ?? DateTime.UtcNow.Date;
                    var count = Convert.ToInt32(record.GetValue());
                    totalActions += count;

                    dailyActions.Add(new DailyStatsDto
                    {
                        Date = date,
                        Count = count
                    });
                }
            }

            return new ModerationTrendsDto
            {
                TotalActions = totalActions,
                DailyActions = dailyActions.OrderBy(s => s.Date).ToList(),
                ActionBreakdown = new List<ActionBreakdownDto>() // Would need more complex query for breakdown
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
        if (!_isEnabled || _influxClient == null)
        {
            return new SystemHealthDto
            {
                UptimePercentage = 0,
                ActiveUsers24h = 0,
                ErrorCount24h = 0,
                AverageResponseTime = 0,
                DatabaseConnections = 0,
                MemoryUsage = 0,
                CpuUsage = 0,
                Alerts = new List<SystemAlertDto>()
            };
        }

        try
        {
            var queryApi = _influxClient.GetQueryApi();

            // Query for active users in last 24h
            var activeUsersQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -24h)
                  |> filter(fn: (r) => r._measurement == ""user_activities"")
                  |> filter(fn: (r) => r.activity_type == ""Login"")
                  |> group(columns: [""user_id""])
                  |> count()
                  |> group()
                  |> sum()";

            // Query for performance metrics
            var performanceQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -24h)
                  |> filter(fn: (r) => r._measurement == ""performance_metrics"")
                  |> filter(fn: (r) => r.metric_type == ""ResponseTime"")
                  |> mean()";

            var activeUsersResult = await queryApi.QueryAsync(activeUsersQuery, _organization);
            var performanceResult = await queryApi.QueryAsync(performanceQuery, _organization);

            var activeUsers24h = 0;
            var averageResponseTime = 0.0;

            // Process active users
            foreach (var table in activeUsersResult)
            {
                foreach (var record in table.Records)
                {
                    activeUsers24h = Convert.ToInt32(record.GetValue());
                    break;
                }
            }

            // Process performance metrics
            foreach (var table in performanceResult)
            {
                foreach (var record in table.Records)
                {
                    averageResponseTime = Convert.ToDouble(record.GetValue());
                    break;
                }
            }

            return new SystemHealthDto
            {
                UptimePercentage = 99.9, // Would calculate from actual uptime data
                ActiveUsers24h = activeUsers24h,
                ErrorCount24h = 0, // Would query from error metrics
                AverageResponseTime = averageResponseTime,
                DatabaseConnections = 25, // Would get from system metrics
                MemoryUsage = 1024 * 1024 * 512, // Would get from system metrics
                CpuUsage = 15.5, // Would get from system metrics
                Alerts = new List<SystemAlertDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health from InfluxDB");
            return new SystemHealthDto
            {
                UptimePercentage = 0,
                ActiveUsers24h = 0,
                ErrorCount24h = 0,
                AverageResponseTime = 0,
                DatabaseConnections = 0,
                MemoryUsage = 0,
                CpuUsage = 0,
                Alerts = new List<SystemAlertDto>()
            };
        }
    }

    public Task<TopModeratorsDto> GetTopModeratorsAsync(int days = 30, int limit = 10)
    {
        if (!_isEnabled || _influxClient == null)
        {
            return Task.FromResult(new TopModeratorsDto
            {
                Moderators = new List<ModeratorStatsDto>(),
                TotalModerators = 0,
                TotalActions = 0
            });
        }

        try
        {
            // This would require more complex queries to get moderator statistics
            // For now, return empty result
            return Task.FromResult(new TopModeratorsDto
            {
                Moderators = new List<ModeratorStatsDto>(),
                TotalModerators = 0,
                TotalActions = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top moderators from InfluxDB");
            return Task.FromResult(new TopModeratorsDto
            {
                Moderators = new List<ModeratorStatsDto>(),
                TotalModerators = 0,
                TotalActions = 0
            });
        }
    }

    public Task<ContentTrendsDto> GetContentTrendsAsync(int days = 30)
    {
        if (!_isEnabled || _influxClient == null)
        {
            return Task.FromResult(new ContentTrendsDto
            {
                TrendingHashtags = new List<HashtagStatsDto>(),
                EngagementTrends = new List<DailyStatsDto>(),
                Trends = new List<ContentTrendDto>(),
                TotalHashtags = 0,
                AverageEngagementRate = 0
            });
        }

        try
        {
            // This would require complex queries for trending analysis
            // For now, return empty result
            return Task.FromResult(new ContentTrendsDto
            {
                TrendingHashtags = new List<HashtagStatsDto>(),
                EngagementTrends = new List<DailyStatsDto>(),
                Trends = new List<ContentTrendDto>(),
                TotalHashtags = 0,
                AverageEngagementRate = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content trends from InfluxDB");
            return Task.FromResult(new ContentTrendsDto
            {
                TrendingHashtags = new List<HashtagStatsDto>(),
                EngagementTrends = new List<DailyStatsDto>(),
                Trends = new List<ContentTrendDto>(),
                TotalHashtags = 0,
                AverageEngagementRate = 0
            });
        }
    }

    public async Task<UserEngagementStatsDto> GetUserEngagementStatsAsync(int days = 30)
    {
        if (!_isEnabled || _influxClient == null)
        {
            return new UserEngagementStatsDto
            {
                DailyEngagement = new List<DailyStatsDto>(),
                AverageSessionDuration = 0,
                TotalSessions = 0,
                RetentionRate = 0,
                EngagementBreakdown = new List<EngagementTypeStatsDto>()
            };
        }

        try
        {
            var queryApi = _influxClient.GetQueryApi();

            // Query for content engagements
            var engagementQuery = $@"
                from(bucket: ""{_bucket}"")
                  |> range(start: -{days}d)
                  |> filter(fn: (r) => r._measurement == ""content_engagement"")
                  |> aggregateWindow(every: 1d, fn: count, createEmpty: false)
                  |> yield(name: ""engagement"")";

            var result = await queryApi.QueryAsync(engagementQuery, _organization);

            var dailyEngagement = new List<DailyStatsDto>();
            var totalSessions = 0;

            foreach (var table in result)
            {
                foreach (var record in table.Records)
                {
                    var date = record.GetTime()?.ToDateTimeUtc().Date ?? DateTime.UtcNow.Date;
                    var count = Convert.ToInt32(record.GetValue());
                    totalSessions += count;

                    dailyEngagement.Add(new DailyStatsDto
                    {
                        Date = date,
                        Count = count
                    });
                }
            }

            return new UserEngagementStatsDto
            {
                DailyEngagement = dailyEngagement.OrderBy(s => s.Date).ToList(),
                AverageSessionDuration = 0, // Would calculate from session data
                TotalSessions = totalSessions,
                RetentionRate = 0, // Would calculate from user retention data
                EngagementBreakdown = new List<EngagementTypeStatsDto>() // Would calculate from engagement types
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user engagement stats from InfluxDB");
            return new UserEngagementStatsDto
            {
                DailyEngagement = new List<DailyStatsDto>(),
                AverageSessionDuration = 0,
                TotalSessions = 0,
                RetentionRate = 0,
                EngagementBreakdown = new List<EngagementTypeStatsDto>()
            };
        }
    }

    public async Task<AnalyticsDataSourceDto> GetDataSourceInfoAsync()
    {
        var isAvailable = await IsAvailableAsync();

        return new AnalyticsDataSourceDto
        {
            ConfiguredSource = "InfluxDB",
            InfluxAvailable = isAvailable,
            ActualSource = isAvailable ? "InfluxDB" : "Database",
            DualWriteEnabled = true, // Would get from configuration
            LastChecked = DateTime.UtcNow,
            HealthMetrics = new Dictionary<string, object>
            {
                ["influx_enabled"] = _isEnabled,
                ["influx_available"] = isAvailable,
                ["bucket"] = _bucket,
                ["organization"] = _organization
            },
            Issues = isAvailable ? new List<string>() : new List<string> { "InfluxDB is not available" }
        };
    }

    private double CalculateGrowthRate(List<int> values)
    {
        if (values.Count < 2) return 0;

        var firstHalf = values.Take(values.Count / 2).Sum();
        var secondHalf = values.Skip(values.Count / 2).Sum();

        if (firstHalf == 0) return secondHalf > 0 ? 100 : 0;

        return ((double)(secondHalf - firstHalf) / firstHalf) * 100;
    }
}
