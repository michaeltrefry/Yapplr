using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yapplr.Api.Configuration;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class ApiRateLimitService : IApiRateLimitService
{
    private readonly ILogger<ApiRateLimitService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly RateLimitingConfiguration _rateLimitingConfig;
    
    // Base rate limit configurations per API operation type
    private readonly Dictionary<ApiOperation, RateLimitConfig> _baseRateLimitConfigs = new()
    {
        [ApiOperation.CreatePost] = new()
        {
            MaxRequestsPerMinute = 5,
            MaxRequestsPerHour = 50,
            MaxRequestsPerDay = 500,
            BurstThreshold = 3
        },
        [ApiOperation.CreateComment] = new()
        {
            MaxRequestsPerMinute = 10,
            MaxRequestsPerHour = 100,
            MaxRequestsPerDay = 1000,
            BurstThreshold = 5
        },
        [ApiOperation.LikePost] = new()
        {
            MaxRequestsPerMinute = 30,
            MaxRequestsPerHour = 300,
            MaxRequestsPerDay = 3000,
            BurstThreshold = 15
        },
        [ApiOperation.UnlikePost] = new()
        {
            MaxRequestsPerMinute = 30,
            MaxRequestsPerHour = 300,
            MaxRequestsPerDay = 3000,
            BurstThreshold = 15
        },
        [ApiOperation.FollowUser] = new()
        {
            MaxRequestsPerMinute = 5,
            MaxRequestsPerHour = 50,
            MaxRequestsPerDay = 500,
            BurstThreshold = 3
        },
        [ApiOperation.UnfollowUser] = new()
        {
            MaxRequestsPerMinute = 10,
            MaxRequestsPerHour = 100,
            MaxRequestsPerDay = 1000,
            BurstThreshold = 5
        },
        [ApiOperation.ReportContent] = new()
        {
            MaxRequestsPerMinute = 2,
            MaxRequestsPerHour = 20,
            MaxRequestsPerDay = 100,
            BurstThreshold = 1
        },
        [ApiOperation.SendMessage] = new()
        {
            MaxRequestsPerMinute = 10,
            MaxRequestsPerHour = 100,
            MaxRequestsPerDay = 1000,
            BurstThreshold = 5
        },
        [ApiOperation.UploadMedia] = new()
        {
            MaxRequestsPerMinute = 3,
            MaxRequestsPerHour = 30,
            MaxRequestsPerDay = 200,
            BurstThreshold = 2
        },
        [ApiOperation.UpdateProfile] = new()
        {
            MaxRequestsPerMinute = 2,
            MaxRequestsPerHour = 20,
            MaxRequestsPerDay = 100,
            BurstThreshold = 1
        },
        [ApiOperation.Search] = new()
        {
            MaxRequestsPerMinute = 20,
            MaxRequestsPerHour = 200,
            MaxRequestsPerDay = 2000,
            BurstThreshold = 10
        },
        [ApiOperation.General] = new()
        {
            MaxRequestsPerMinute = 60,
            MaxRequestsPerHour = 600,
            MaxRequestsPerDay = 6000,
            BurstThreshold = 30
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

    public ApiRateLimitService(
        ILogger<ApiRateLimitService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<RateLimitingConfiguration> rateLimitingOptions)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _rateLimitingConfig = rateLimitingOptions.Value;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(int userId, ApiOperation operation)
    {
        // Check if rate limiting is globally disabled
        if (!_rateLimitingConfig.Enabled)
        {
            return new RateLimitResult
            {
                IsAllowed = true,
                RemainingRequests = int.MaxValue
            };
        }

        // Check user-specific and role-based rate limiting settings
        bool shouldApplyRateLimit;
        bool shouldUseTrustBased;

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
            var user = await dbContext.Users.FindAsync(userId);

            if (user == null)
            {
                // Default to allowing if user not found
                return new RateLimitResult
                {
                    IsAllowed = true,
                    RemainingRequests = int.MaxValue
                };
            }

            // Check role-based exemptions
            if (user.Role == UserRole.Admin && !_rateLimitingConfig.ApplyToAdmins)
            {
                return new RateLimitResult
                {
                    IsAllowed = true,
                    RemainingRequests = int.MaxValue
                };
            }

            if (user.Role == UserRole.Moderator && !_rateLimitingConfig.ApplyToModerators)
            {
                return new RateLimitResult
                {
                    IsAllowed = true,
                    RemainingRequests = int.MaxValue
                };
            }

            // Check user-specific rate limiting override
            shouldApplyRateLimit = user.RateLimitingEnabled ?? _rateLimitingConfig.Enabled;
            shouldUseTrustBased = user.TrustBasedRateLimitingEnabled ?? _rateLimitingConfig.TrustBasedEnabled;
        }

        if (!shouldApplyRateLimit)
        {
            return new RateLimitResult
            {
                IsAllowed = true,
                RemainingRequests = int.MaxValue
            };
        }

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

        // Get base configuration and apply trust-based multiplier
        var baseConfig = GetBaseRateLimitConfig(operation);
        float trustMultiplier = _rateLimitingConfig.FallbackMultiplier;

        if (shouldUseTrustBased)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var trustBasedModerationService = scope.ServiceProvider.GetRequiredService<ITrustBasedModerationService>();
                trustMultiplier = await trustBasedModerationService.GetRateLimitMultiplierAsync(userId);
            }
        }

        var adjustedConfig = ApplyTrustMultiplier(baseConfig, trustMultiplier);

        var key = $"{userId}:{operation}";
        var tracker = _requestTrackers.GetOrAdd(key, _ => new RequestTracker());
        var now = DateTime.UtcNow;

        lock (tracker)
        {
            // Clean old requests outside the window
            CleanOldRequests(tracker, now, TimeSpan.FromDays(1)); // Keep 24 hours for daily limits

            // Check burst protection (last 10 seconds)
            if (_rateLimitingConfig.BurstProtectionEnabled && adjustedConfig.EnableBurstProtection)
            {
                var burstRequests = tracker.Requests.Count(r => r > now.AddSeconds(-10));
                if (burstRequests >= adjustedConfig.BurstThreshold)
                {
                    RecordViolation(userId, operation.ToString(), "burst", burstRequests, adjustedConfig.BurstThreshold);
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
            if (minuteRequests >= adjustedConfig.MaxRequestsPerMinute)
            {
                RecordViolation(userId, operation.ToString(), "minute", minuteRequests, adjustedConfig.MaxRequestsPerMinute);
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
            if (hourRequests >= adjustedConfig.MaxRequestsPerHour)
            {
                RecordViolation(userId, operation.ToString(), "hour", hourRequests, adjustedConfig.MaxRequestsPerHour);
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
            if (dayRequests >= adjustedConfig.MaxRequestsPerDay)
            {
                RecordViolation(userId, operation.ToString(), "day", dayRequests, adjustedConfig.MaxRequestsPerDay);
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
                RemainingRequests = adjustedConfig.MaxRequestsPerMinute - minuteRequests - 1,
                ResetTime = now.AddMinutes(1)
            };
        }
    }

    public async Task RecordRequestAsync(int userId, ApiOperation operation)
    {
        await Task.CompletedTask;

        var key = $"{userId}:{operation}";
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
        var blockedUsers = _blockedUsers.Count(kvp => kvp.Value > DateTime.UtcNow);
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
            ["base_rate_limit_configs"] = _baseRateLimitConfigs.ToDictionary(
                kvp => kvp.Key.ToString(),
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

        // Remove all tracking for this user
        var keysToRemove = _requestTrackers.Keys.Where(k => k.StartsWith($"{userId}:")).ToList();
        foreach (var key in keysToRemove)
        {
            _requestTrackers.TryRemove(key, out _);
        }

        // Clear violations
        _violations.TryRemove(userId, out _);

        // Unblock if blocked
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

    private RateLimitConfig GetBaseRateLimitConfig(ApiOperation operation)
    {
        return _baseRateLimitConfigs.TryGetValue(operation, out var config)
            ? config
            : _baseRateLimitConfigs[ApiOperation.General];
    }

    private RateLimitConfig ApplyTrustMultiplier(RateLimitConfig baseConfig, float multiplier)
    {
        return new RateLimitConfig
        {
            MaxRequestsPerMinute = Math.Max(1, (int)(baseConfig.MaxRequestsPerMinute * multiplier)),
            MaxRequestsPerHour = Math.Max(1, (int)(baseConfig.MaxRequestsPerHour * multiplier)),
            MaxRequestsPerDay = Math.Max(1, (int)(baseConfig.MaxRequestsPerDay * multiplier)),
            BurstThreshold = Math.Max(1, (int)(baseConfig.BurstThreshold * multiplier)),
            EnableBurstProtection = baseConfig.EnableBurstProtection,
            WindowSize = baseConfig.WindowSize
        };
    }

    private void CleanOldRequests(RequestTracker tracker, DateTime now, TimeSpan maxAge)
    {
        var cutoff = now - maxAge;
        while (tracker.Requests.Count > 0 && tracker.Requests.Peek() < cutoff)
        {
            tracker.Requests.Dequeue();
        }
    }

    private void RecordViolation(int userId, string operationType, string limitType, int requestCount, int limit)
    {
        var violation = new RateLimitViolation
        {
            UserId = userId,
            NotificationType = operationType, // Reusing this field for operation type
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

        _logger.LogWarning("API rate limit violation for user {UserId}, operation {OperationType}, limit {LimitType}: {RequestCount}/{Limit}",
            userId, operationType, limitType, requestCount, limit);

        // Auto-block users with too many violations
        if (_rateLimitingConfig.AutoBlockingEnabled && userViolations.Count >= _rateLimitingConfig.AutoBlockViolationThreshold)
        {
            var blockDuration = TimeSpan.FromHours(_rateLimitingConfig.AutoBlockDurationHours);
            _ = Task.Run(() => BlockUserAsync(userId, blockDuration, "Too many API rate limit violations"));
        }
    }
}
