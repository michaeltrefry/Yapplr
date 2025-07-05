using System.Collections.Concurrent;

namespace Yapplr.Api.Services;

/// <summary>
/// Rate limit configuration for different notification types
/// </summary>
public class RateLimitConfig
{
    public int MaxRequestsPerMinute { get; set; } = 10;
    public int MaxRequestsPerHour { get; set; } = 100;
    public int MaxRequestsPerDay { get; set; } = 1000;
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);
    public bool EnableBurstProtection { get; set; } = true;
    public int BurstThreshold { get; set; } = 5; // Max requests in 10 seconds
}

/// <summary>
/// Rate limit violation information
/// </summary>
public class RateLimitViolation
{
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public DateTime ViolationTime { get; set; } = DateTime.UtcNow;
    public string LimitType { get; set; } = string.Empty; // "minute", "hour", "day", "burst"
    public int RequestCount { get; set; }
    public int Limit { get; set; }
    public TimeSpan RetryAfter { get; set; }
}

/// <summary>
/// Rate limit result
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RemainingRequests { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public string? ViolationType { get; set; }
    public DateTime? ResetTime { get; set; }
}

/// <summary>
/// Request tracking for rate limiting
/// </summary>
internal class RequestTracker
{
    public Queue<DateTime> Requests { get; set; } = new();
    public DateTime LastRequest { get; set; }
    public int TotalRequests { get; set; }
}

/// <summary>
/// Service for rate limiting notification requests to prevent spam
/// </summary>
public interface INotificationRateLimitService
{
    Task<RateLimitResult> CheckRateLimitAsync(int userId, string notificationType);
    Task RecordRequestAsync(int userId, string notificationType);
    Task<List<RateLimitViolation>> GetRecentViolationsAsync(int userId);
    Task<Dictionary<string, object>> GetRateLimitStatsAsync();
    Task ResetUserLimitsAsync(int userId);
    Task<bool> IsUserBlockedAsync(int userId);
    Task BlockUserAsync(int userId, TimeSpan duration, string reason);
    Task UnblockUserAsync(int userId);
}

public class NotificationRateLimitService : INotificationRateLimitService
{
    private readonly ILogger<NotificationRateLimitService> _logger;
    
    // Rate limit configurations per notification type
    private readonly Dictionary<string, RateLimitConfig> _rateLimitConfigs = new()
    {
        ["message"] = new()
        {
            MaxRequestsPerMinute = 20,
            MaxRequestsPerHour = 200,
            MaxRequestsPerDay = 2000,
            BurstThreshold = 10
        },
        ["mention"] = new()
        {
            MaxRequestsPerMinute = 15,
            MaxRequestsPerHour = 150,
            MaxRequestsPerDay = 1500,
            BurstThreshold = 8
        },
        ["comment"] = new()
        {
            MaxRequestsPerMinute = 30,
            MaxRequestsPerHour = 300,
            MaxRequestsPerDay = 3000,
            BurstThreshold = 15
        },
        ["like"] = new()
        {
            MaxRequestsPerMinute = 60,
            MaxRequestsPerHour = 600,
            MaxRequestsPerDay = 6000,
            BurstThreshold = 30
        },
        ["follow"] = new()
        {
            MaxRequestsPerMinute = 10,
            MaxRequestsPerHour = 100,
            MaxRequestsPerDay = 1000,
            BurstThreshold = 5
        },
        ["default"] = new()
        {
            MaxRequestsPerMinute = 10,
            MaxRequestsPerHour = 100,
            MaxRequestsPerDay = 1000,
            BurstThreshold = 5
        }
    };

    // In-memory tracking (in production, use Redis or similar)
    private readonly ConcurrentDictionary<string, RequestTracker> _requestTrackers = new();
    private readonly ConcurrentDictionary<int, List<RateLimitViolation>> _violations = new();
    private readonly ConcurrentDictionary<int, DateTime> _blockedUsers = new();
    
    // Statistics
    private long _totalRequests = 0;
    private long _totalViolations = 0;
    private long _totalBlocked = 0;

    public NotificationRateLimitService(ILogger<NotificationRateLimitService> logger)
    {
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(int userId, string notificationType)
    {
        await Task.CompletedTask;

        // Check if user is blocked
        if (_blockedUsers.TryGetValue(userId, out var blockedUntil))
        {
            if (DateTime.UtcNow < blockedUntil)
            {
                return new RateLimitResult
                {
                    IsAllowed = false,
                    RetryAfter = blockedUntil - DateTime.UtcNow,
                    ViolationType = "blocked"
                };
            }
            else
            {
                // Unblock user if time has passed
                _blockedUsers.TryRemove(userId, out _);
            }
        }

        var config = GetRateLimitConfig(notificationType);
        var key = $"{userId}:{notificationType}";
        var tracker = _requestTrackers.GetOrAdd(key, _ => new RequestTracker());
        var now = DateTime.UtcNow;

        lock (tracker)
        {
            // Clean old requests outside the window
            CleanOldRequests(tracker, now, TimeSpan.FromDays(1)); // Keep 24 hours for daily limits

            // Check burst protection (last 10 seconds)
            if (config.EnableBurstProtection)
            {
                var burstRequests = tracker.Requests.Count(r => r > now.AddSeconds(-10));
                if (burstRequests >= config.BurstThreshold)
                {
                    RecordViolation(userId, notificationType, "burst", burstRequests, config.BurstThreshold);
                    return new RateLimitResult
                    {
                        IsAllowed = false,
                        RetryAfter = TimeSpan.FromSeconds(10),
                        ViolationType = "burst"
                    };
                }
            }

            // Check minute limit
            var minuteRequests = tracker.Requests.Count(r => r > now.AddMinutes(-1));
            if (minuteRequests >= config.MaxRequestsPerMinute)
            {
                RecordViolation(userId, notificationType, "minute", minuteRequests, config.MaxRequestsPerMinute);
                return new RateLimitResult
                {
                    IsAllowed = false,
                    RemainingRequests = 0,
                    RetryAfter = TimeSpan.FromMinutes(1),
                    ViolationType = "minute",
                    ResetTime = now.AddMinutes(1)
                };
            }

            // Check hour limit
            var hourRequests = tracker.Requests.Count(r => r > now.AddHours(-1));
            if (hourRequests >= config.MaxRequestsPerHour)
            {
                RecordViolation(userId, notificationType, "hour", hourRequests, config.MaxRequestsPerHour);
                return new RateLimitResult
                {
                    IsAllowed = false,
                    RemainingRequests = 0,
                    RetryAfter = TimeSpan.FromHours(1),
                    ViolationType = "hour",
                    ResetTime = now.AddHours(1)
                };
            }

            // Check day limit
            var dayRequests = tracker.Requests.Count(r => r > now.AddDays(-1));
            if (dayRequests >= config.MaxRequestsPerDay)
            {
                RecordViolation(userId, notificationType, "day", dayRequests, config.MaxRequestsPerDay);
                return new RateLimitResult
                {
                    IsAllowed = false,
                    RemainingRequests = 0,
                    RetryAfter = TimeSpan.FromDays(1),
                    ViolationType = "day",
                    ResetTime = now.AddDays(1)
                };
            }

            // Request is allowed
            return new RateLimitResult
            {
                IsAllowed = true,
                RemainingRequests = config.MaxRequestsPerMinute - minuteRequests - 1,
                ResetTime = now.AddMinutes(1)
            };
        }
    }

    public async Task RecordRequestAsync(int userId, string notificationType)
    {
        await Task.CompletedTask;

        var key = $"{userId}:{notificationType}";
        var tracker = _requestTrackers.GetOrAdd(key, _ => new RequestTracker());
        var now = DateTime.UtcNow;

        lock (tracker)
        {
            tracker.Requests.Enqueue(now);
            tracker.LastRequest = now;
            tracker.TotalRequests++;
        }

        Interlocked.Increment(ref _totalRequests);
    }

    public async Task<List<RateLimitViolation>> GetRecentViolationsAsync(int userId)
    {
        await Task.CompletedTask;

        if (_violations.TryGetValue(userId, out var userViolations))
        {
            var cutoff = DateTime.UtcNow.AddHours(-24); // Last 24 hours
            return userViolations.Where(v => v.ViolationTime > cutoff).ToList();
        }

        return new List<RateLimitViolation>();
    }

    public async Task<Dictionary<string, object>> GetRateLimitStatsAsync()
    {
        await Task.CompletedTask;

        var activeTrackers = _requestTrackers.Count;
        var blockedUsers = _blockedUsers.Count;
        var recentViolations = _violations.Values
            .SelectMany(v => v)
            .Count(v => v.ViolationTime > DateTime.UtcNow.AddHours(-1));

        return new Dictionary<string, object>
        {
            ["total_requests"] = _totalRequests,
            ["total_violations"] = _totalViolations,
            ["total_blocked"] = _totalBlocked,
            ["active_trackers"] = activeTrackers,
            ["currently_blocked_users"] = blockedUsers,
            ["recent_violations_last_hour"] = recentViolations,
            ["rate_limit_configs"] = _rateLimitConfigs.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    max_per_minute = kvp.Value.MaxRequestsPerMinute,
                    max_per_hour = kvp.Value.MaxRequestsPerHour,
                    max_per_day = kvp.Value.MaxRequestsPerDay,
                    burst_threshold = kvp.Value.BurstThreshold
                })
        };
    }

    public async Task ResetUserLimitsAsync(int userId)
    {
        await Task.CompletedTask;

        // Remove all trackers for this user
        var keysToRemove = _requestTrackers.Keys
            .Where(k => k.StartsWith($"{userId}:"))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _requestTrackers.TryRemove(key, out _);
        }

        // Clear violations
        _violations.TryRemove(userId, out _);

        // Unblock user
        _blockedUsers.TryRemove(userId, out _);

        _logger.LogInformation("Reset rate limits for user {UserId}", userId);
    }

    public async Task<bool> IsUserBlockedAsync(int userId)
    {
        await Task.CompletedTask;

        if (_blockedUsers.TryGetValue(userId, out var blockedUntil))
        {
            if (DateTime.UtcNow < blockedUntil)
            {
                return true;
            }
            else
            {
                // Auto-unblock if time has passed
                _blockedUsers.TryRemove(userId, out _);
                return false;
            }
        }

        return false;
    }

    public async Task BlockUserAsync(int userId, TimeSpan duration, string reason)
    {
        await Task.CompletedTask;

        var blockedUntil = DateTime.UtcNow.Add(duration);
        _blockedUsers[userId] = blockedUntil;

        Interlocked.Increment(ref _totalBlocked);

        _logger.LogWarning("Blocked user {UserId} for {Duration} due to: {Reason}", 
            userId, duration, reason);
    }

    public async Task UnblockUserAsync(int userId)
    {
        await Task.CompletedTask;

        if (_blockedUsers.TryRemove(userId, out _))
        {
            _logger.LogInformation("Unblocked user {UserId}", userId);
        }
    }

    private RateLimitConfig GetRateLimitConfig(string notificationType)
    {
        return _rateLimitConfigs.TryGetValue(notificationType.ToLower(), out var config)
            ? config
            : _rateLimitConfigs["default"];
    }

    private void CleanOldRequests(RequestTracker tracker, DateTime now, TimeSpan maxAge)
    {
        var cutoff = now - maxAge;
        while (tracker.Requests.Count > 0 && tracker.Requests.Peek() < cutoff)
        {
            tracker.Requests.Dequeue();
        }
    }

    private void RecordViolation(int userId, string notificationType, string limitType, int requestCount, int limit)
    {
        var violation = new RateLimitViolation
        {
            UserId = userId,
            NotificationType = notificationType,
            LimitType = limitType,
            RequestCount = requestCount,
            Limit = limit,
            RetryAfter = limitType switch
            {
                "burst" => TimeSpan.FromSeconds(10),
                "minute" => TimeSpan.FromMinutes(1),
                "hour" => TimeSpan.FromHours(1),
                "day" => TimeSpan.FromDays(1),
                _ => TimeSpan.FromMinutes(1)
            }
        };

        var userViolations = _violations.GetOrAdd(userId, _ => new List<RateLimitViolation>());
        lock (userViolations)
        {
            userViolations.Add(violation);
            
            // Keep only recent violations (last 24 hours)
            var cutoff = DateTime.UtcNow.AddHours(-24);
            userViolations.RemoveAll(v => v.ViolationTime < cutoff);
        }

        Interlocked.Increment(ref _totalViolations);

        _logger.LogWarning("Rate limit violation for user {UserId}, type {NotificationType}, limit {LimitType}: {RequestCount}/{Limit}",
            userId, notificationType, limitType, requestCount, limit);

        // Auto-block users with too many violations
        if (userViolations.Count >= 10) // 10 violations in 24 hours
        {
            _ = Task.Run(() => BlockUserAsync(userId, TimeSpan.FromHours(1), "Too many rate limit violations"));
        }
    }
}
