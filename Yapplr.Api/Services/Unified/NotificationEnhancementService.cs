using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Services;

namespace Yapplr.Api.Services.Unified;

/// <summary>
/// Unified enhancement service that consolidates NotificationMetricsService, NotificationAuditService,
/// NotificationRateLimitService, NotificationContentFilterService, and NotificationCompressionService.
/// Provides optional cross-cutting concerns for notifications.
/// </summary>
public class NotificationEnhancementService : INotificationEnhancementService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<NotificationEnhancementService> _logger;

    #region Configuration
    private readonly EnhancementConfiguration _config = new()
    {
        EnableMetrics = true,
        EnableAuditing = true,
        EnableRateLimiting = true,
        EnableContentFiltering = true,
        EnableCompression = true
    };
    #endregion

    #region Metrics Collections
    // Metrics tracking
    private readonly ConcurrentDictionary<string, DeliveryEvent> _activeDeliveries = new();
    private readonly ConcurrentQueue<DeliveryEvent> _completedDeliveries = new();
    private readonly ConcurrentDictionary<string, long> _notificationTypeCounts = new();
    private readonly ConcurrentDictionary<string, long> _providerCounts = new();
    private readonly ConcurrentDictionary<string, List<double>> _providerLatencies = new();
    
    private long _totalSent = 0;
    private long _totalDelivered = 0;
    private long _totalFailed = 0;
    private const int MaxCompletedDeliveries = 1000;
    private const int MaxLatencyHistory = 100;
    #endregion

    #region Rate Limiting Collections
    private readonly ConcurrentDictionary<string, RequestTracker> _requestTrackers = new();
    private readonly ConcurrentDictionary<int, List<RateLimitViolation>> _violations = new();
    private readonly ConcurrentDictionary<int, DateTime> _blockedUsers = new();
    private long _totalRequests = 0;
    private long _totalViolations = 0;
    #endregion

    #region Content Filtering Collections
    private readonly ContentFilterConfig _filterConfig = new()
    {
        EnableProfanityFilter = true,
        EnableSpamDetection = true,
        EnablePhishingDetection = true,
        EnableMaliciousLinkDetection = true,
        EnableContentSanitization = true,
        MaxContentLength = 2000
    };
    
    private long _totalValidations = 0;
    private long _totalViolationsContent = 0;
    private long _totalBlocked = 0;
    private long _totalSanitized = 0;
    #endregion

    #region Compression Collections
    private long _totalPayloadsProcessed = 0;
    private long _totalOriginalBytes = 0;
    private long _totalCompressedBytes = 0;
    private readonly object _statsLock = new object();
    #endregion

    public NotificationEnhancementService(
        YapplrDbContext context,
        ILogger<NotificationEnhancementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Metrics and Analytics

    public async Task RecordNotificationEventAsync(NotificationEvent notificationEvent)
    {
        if (!_config.EnableMetrics) return;

        try
        {
            var deliveryEvent = new DeliveryEvent
            {
                Id = notificationEvent.TrackingId ?? Guid.NewGuid().ToString(),
                UserId = notificationEvent.UserId,
                NotificationType = notificationEvent.NotificationType,
                Provider = notificationEvent.Provider,
                StartTime = notificationEvent.Timestamp,
                Success = notificationEvent.Success,
                ErrorMessage = notificationEvent.ErrorMessage,
                LatencyMs = notificationEvent.LatencyMs ?? 0
            };

            if (notificationEvent.EventType == "start")
            {
                _activeDeliveries[deliveryEvent.Id] = deliveryEvent;
                Interlocked.Increment(ref _totalSent);
                _notificationTypeCounts.AddOrUpdate(notificationEvent.NotificationType, 1, (key, value) => value + 1);
                _providerCounts.AddOrUpdate(notificationEvent.Provider, 1, (key, value) => value + 1);
            }
            else if (notificationEvent.EventType == "complete")
            {
                if (_activeDeliveries.TryRemove(deliveryEvent.Id, out var activeEvent))
                {
                    activeEvent.Success = notificationEvent.Success;
                    activeEvent.ErrorMessage = notificationEvent.ErrorMessage;
                    activeEvent.LatencyMs = (DateTime.UtcNow - activeEvent.StartTime).TotalMilliseconds;

                    if (notificationEvent.Success)
                    {
                        Interlocked.Increment(ref _totalDelivered);
                    }
                    else
                    {
                        Interlocked.Increment(ref _totalFailed);
                    }

                    // Track latency for provider
                    _providerLatencies.AddOrUpdate(
                        activeEvent.Provider,
                        new List<double> { activeEvent.LatencyMs },
                        (key, latencies) =>
                        {
                            lock (latencies)
                            {
                                latencies.Add(activeEvent.LatencyMs);
                                if (latencies.Count > MaxLatencyHistory)
                                {
                                    latencies.RemoveAt(0);
                                }
                            }
                            return latencies;
                        });

                    _completedDeliveries.Enqueue(activeEvent);
                    while (_completedDeliveries.Count > MaxCompletedDeliveries)
                    {
                        _completedDeliveries.TryDequeue(out _);
                    }
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record notification event");
        }
    }

    public async Task<NotificationMetrics> GetMetricsAsync(TimeSpan? timeWindow = null)
    {
        if (!_config.EnableMetrics)
        {
            return new NotificationMetrics { LastUpdated = DateTime.UtcNow };
        }

        try
        {
            var cutoffTime = timeWindow.HasValue ? DateTime.UtcNow - timeWindow.Value : DateTime.MinValue;

            var recentDeliveries = _completedDeliveries
                .Where(d => d.StartTime >= cutoffTime)
                .ToList();

            // Include active deliveries that started within the time window
            var recentActiveDeliveries = timeWindow.HasValue
                ? _activeDeliveries.Values.Where(d => d.StartTime >= cutoffTime).ToList()
                : new List<DeliveryEvent>();

            var totalSent = timeWindow.HasValue
                ? recentDeliveries.Count + recentActiveDeliveries.Count
                : _totalSent;
            var totalDelivered = timeWindow.HasValue 
                ? recentDeliveries.Count(d => d.Success) 
                : _totalDelivered;
            var totalFailed = timeWindow.HasValue 
                ? recentDeliveries.Count(d => !d.Success) 
                : _totalFailed;

            var averageLatency = recentDeliveries.Any() 
                ? recentDeliveries.Average(d => d.LatencyMs) 
                : 0;

            var successRate = totalSent > 0 ? (double)totalDelivered / totalSent * 100 : 0;

            // Type breakdown - include both completed and active deliveries for time window
            var typeBreakdown = timeWindow.HasValue
                ? recentDeliveries.Concat(recentActiveDeliveries)
                    .GroupBy(d => d.NotificationType)
                    .ToDictionary(g => g.Key, g => (long)g.Count())
                : _notificationTypeCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Provider breakdown - include both completed and active deliveries for time window
            var providerBreakdown = timeWindow.HasValue
                ? recentDeliveries.Concat(recentActiveDeliveries)
                    .GroupBy(d => d.Provider)
                    .ToDictionary(g => g.Key, g => (long)g.Count())
                : _providerCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Provider latencies
            var providerLatencies = new Dictionary<string, double>();
            foreach (var kvp in _providerLatencies)
            {
                lock (kvp.Value)
                {
                    if (kvp.Value.Any())
                    {
                        providerLatencies[kvp.Key] = kvp.Value.Average();
                    }
                }
            }

            var metrics = new NotificationMetrics
            {
                TotalNotificationsSent = totalSent,
                TotalNotificationsDelivered = totalDelivered,
                TotalNotificationsFailed = totalFailed,
                AverageDeliveryTimeMs = averageLatency,
                DeliverySuccessRate = successRate,
                NotificationTypeBreakdown = typeBreakdown,
                ProviderBreakdown = providerBreakdown,
                ProviderAverageLatency = providerLatencies,
                LastUpdated = DateTime.UtcNow
            };

            await Task.CompletedTask;
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification metrics");
            return new NotificationMetrics { LastUpdated = DateTime.UtcNow };
        }
    }

    public async Task<List<DeliveryEvent>> GetRecentDeliveryEventsAsync(int count = 100)
    {
        if (!_config.EnableMetrics)
        {
            return new List<DeliveryEvent>();
        }

        try
        {
            var recentDeliveries = _completedDeliveries
                .TakeLast(count)
                .OrderByDescending(d => d.StartTime)
                .ToList();

            await Task.CompletedTask;
            return recentDeliveries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent delivery events");
            return new List<DeliveryEvent>();
        }
    }

    public async Task<PerformanceInsights> GetPerformanceInsightsAsync()
    {
        if (!_config.EnableMetrics)
        {
            return new PerformanceInsights();
        }

        try
        {
            var metrics = await GetMetricsAsync(TimeSpan.FromHours(24));
            var recommendations = new List<string>();

            // Analyze performance and generate recommendations
            if (metrics.DeliverySuccessRate < 95)
            {
                recommendations.Add("Consider investigating delivery failures - success rate is below 95%");
            }

            if (metrics.AverageDeliveryTimeMs > 5000)
            {
                recommendations.Add("Average delivery time is high - consider optimizing provider performance");
            }

            // Find best and worst performing providers
            var bestProvider = metrics.ProviderAverageLatency.OrderBy(kvp => kvp.Value).FirstOrDefault();
            var worstProvider = metrics.ProviderAverageLatency.OrderByDescending(kvp => kvp.Value).FirstOrDefault();

            return new PerformanceInsights
            {
                OverallSuccessRate = metrics.DeliverySuccessRate,
                AverageDeliveryTime = metrics.AverageDeliveryTimeMs,
                BestPerformingProvider = bestProvider.Key ?? "Unknown",
                WorstPerformingProvider = worstProvider.Key ?? "Unknown",
                Recommendations = recommendations,
                DetailedMetrics = new Dictionary<string, object>
                {
                    ["total_sent"] = metrics.TotalNotificationsSent,
                    ["total_delivered"] = metrics.TotalNotificationsDelivered,
                    ["total_failed"] = metrics.TotalNotificationsFailed,
                    ["provider_breakdown"] = metrics.ProviderBreakdown,
                    ["type_breakdown"] = metrics.NotificationTypeBreakdown
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance insights");
            return new PerformanceInsights();
        }
    }

    #endregion

    #region Security and Auditing

    public async Task<bool> ShouldAllowNotificationAsync(int userId, string notificationType, string content)
    {
        if (!_config.EnableAuditing) return true;

        try
        {
            // Check if user is blocked
            if (_blockedUsers.TryGetValue(userId, out var blockedUntil) && DateTime.UtcNow < blockedUntil)
            {
                await LogSecurityEventAsync(new SecurityEvent
                {
                    UserId = userId,
                    EventType = "blocked_user_attempt",
                    Description = $"Blocked user {userId} attempted to send {notificationType} notification",
                    Severity = SecurityEventSeverity.Medium,
                    Timestamp = DateTime.UtcNow
                });
                return false;
            }

            // Check content safety
            if (_config.EnableContentFiltering)
            {
                var contentResult = await FilterContentAsync(content);
                if (!contentResult.IsValid)
                {
                    await LogSecurityEventAsync(new SecurityEvent
                    {
                        UserId = userId,
                        EventType = "unsafe_content",
                        Description = $"Unsafe content detected in {notificationType} notification: {string.Join(", ", contentResult.Violations)}",
                        Severity = SecurityEventSeverity.High,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["content_violations"] = contentResult.Violations,
                            ["risk_level"] = contentResult.RiskLevel.ToString()
                        }
                    });
                    return false;
                }
            }

            // Check rate limits
            if (_config.EnableRateLimiting)
            {
                var rateLimitResult = await CheckRateLimitAsync(userId, notificationType);
                if (!rateLimitResult.IsAllowed)
                {
                    await LogSecurityEventAsync(new SecurityEvent
                    {
                        UserId = userId,
                        EventType = "rate_limit_exceeded",
                        Description = $"Rate limit exceeded for user {userId} on {notificationType} notification",
                        Severity = SecurityEventSeverity.Medium,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["violation_type"] = rateLimitResult.ViolationType ?? "unknown",
                            ["retry_after"] = rateLimitResult.RetryAfter?.TotalSeconds ?? 0
                        }
                    });
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if notification should be allowed for user {UserId}", userId);
            return true; // Fail open for availability
        }
    }

    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        if (!_config.EnableAuditing) return;

        try
        {
            // Log to application logs
            var logLevel = securityEvent.Severity switch
            {
                SecurityEventSeverity.Low => LogLevel.Information,
                SecurityEventSeverity.Medium => LogLevel.Warning,
                SecurityEventSeverity.High => LogLevel.Error,
                SecurityEventSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, "Security Event: {EventType} for user {UserId} - {Description}",
                securityEvent.EventType, securityEvent.UserId, securityEvent.Description);

            // Store in database for audit trail
            var auditRecord = new Models.NotificationAuditLog
            {
                EventId = Guid.NewGuid().ToString(),
                UserId = securityEvent.UserId,
                EventType = securityEvent.EventType,
                Description = securityEvent.Description,
                Severity = securityEvent.Severity.ToString(),
                Timestamp = securityEvent.Timestamp,
                AdditionalData = securityEvent.AdditionalData != null
                    ? System.Text.Json.JsonSerializer.Serialize(securityEvent.AdditionalData)
                    : null
            };

            _context.NotificationAuditLogs.Add(auditRecord);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {EventType}", securityEvent.EventType);
        }
    }

    public async Task<List<SecurityEvent>> GetRecentSecurityEventsAsync(int? userId = null, int count = 100)
    {
        if (!_config.EnableAuditing)
        {
            return new List<SecurityEvent>();
        }

        try
        {
            var query = _context.NotificationAuditLogs.AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(log => log.UserId == userId.Value);
            }

            var auditLogs = await query
                .OrderByDescending(log => log.Timestamp)
                .Take(count)
                .ToListAsync();

            return auditLogs.Select(log => new SecurityEvent
            {
                UserId = log.UserId,
                EventType = log.EventType,
                Description = log.Description,
                Severity = Enum.Parse<SecurityEventSeverity>(log.Severity),
                Timestamp = log.Timestamp,
                AdditionalData = !string.IsNullOrEmpty(log.AdditionalData)
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(log.AdditionalData)
                    : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent security events");
            return new List<SecurityEvent>();
        }
    }

    public async Task<List<Models.NotificationAuditLog>> GetAuditLogsAsync(AuditQueryParams queryParams)
    {
        if (!_config.EnableAuditing)
        {
            return new List<Models.NotificationAuditLog>();
        }

        try
        {
            var query = _context.NotificationAuditLogs.AsQueryable();

            if (queryParams.StartDate.HasValue)
                query = query.Where(log => log.Timestamp >= queryParams.StartDate.Value);

            if (queryParams.EndDate.HasValue)
                query = query.Where(log => log.Timestamp <= queryParams.EndDate.Value);

            if (queryParams.UserId.HasValue)
                query = query.Where(log => log.UserId == queryParams.UserId.Value);

            if (!string.IsNullOrEmpty(queryParams.EventType))
                query = query.Where(log => log.EventType == queryParams.EventType);

            return await query
                .OrderByDescending(log => log.Timestamp)
                .Take(queryParams.PageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs");
            return new List<Models.NotificationAuditLog>();
        }
    }

    public async Task<SecurityStats> GetSecurityStatsAsync()
    {
        if (!_config.EnableAuditing)
        {
            return new SecurityStats();
        }

        try
        {
            var last24Hours = DateTime.UtcNow.AddHours(-24);

            var recentEvents = await _context.NotificationAuditLogs
                .Where(log => log.Timestamp >= last24Hours)
                .GroupBy(log => log.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToListAsync();

            var severityBreakdown = await _context.NotificationAuditLogs
                .Where(log => log.Timestamp >= last24Hours)
                .GroupBy(log => log.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToListAsync();

            var blockedUsersCount = _blockedUsers.Count(kvp => DateTime.UtcNow < kvp.Value);

            return new SecurityStats
            {
                TotalEvents24h = recentEvents.Sum(e => e.Count),
                EventTypeBreakdown = recentEvents.ToDictionary(e => e.EventType, e => e.Count),
                SeverityBreakdown = severityBreakdown.ToDictionary(s => s.Severity, s => s.Count),
                CurrentlyBlockedUsers = blockedUsersCount,
                TotalViolations = _totalViolations,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security statistics");
            return new SecurityStats { LastUpdated = DateTime.UtcNow };
        }
    }

    #endregion

    #region Rate Limiting

    public async Task<RateLimitResult> CheckRateLimitAsync(int userId, string notificationType)
    {
        if (!_config.EnableRateLimiting)
        {
            return new RateLimitResult { IsAllowed = true };
        }

        await Task.CompletedTask;

        try
        {
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
                CleanOldRequests(tracker, now, TimeSpan.FromDays(1));

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for user {UserId}", userId);
            return new RateLimitResult { IsAllowed = true }; // Fail open
        }
    }

    public async Task RecordNotificationSentAsync(int userId, string notificationType)
    {
        if (!_config.EnableRateLimiting) return;

        await Task.CompletedTask;

        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording notification sent for user {UserId}", userId);
        }
    }

    public async Task<List<RateLimitViolation>> GetRecentViolationsAsync(int? userId = null)
    {
        if (!_config.EnableRateLimiting)
        {
            return new List<RateLimitViolation>();
        }

        await Task.CompletedTask;

        try
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            var allViolations = new List<RateLimitViolation>();

            if (userId.HasValue)
            {
                if (_violations.TryGetValue(userId.Value, out var userViolations))
                {
                    lock (userViolations)
                    {
                        allViolations.AddRange(userViolations.Where(v => v.ViolationTime > cutoff));
                    }
                }
            }
            else
            {
                foreach (var kvp in _violations)
                {
                    lock (kvp.Value)
                    {
                        allViolations.AddRange(kvp.Value.Where(v => v.ViolationTime > cutoff));
                    }
                }
            }

            return allViolations.OrderByDescending(v => v.ViolationTime).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent violations");
            return new List<RateLimitViolation>();
        }
    }

    public async Task ResetUserRateLimitsAsync(int userId)
    {
        if (!_config.EnableRateLimiting) return;

        await Task.CompletedTask;

        try
        {
            // Remove all tracking for this user
            var keysToRemove = _requestTrackers.Keys.Where(k => k.StartsWith($"{userId}:")).ToList();
            foreach (var key in keysToRemove)
            {
                _requestTrackers.TryRemove(key, out _);
            }

            // Remove violations
            _violations.TryRemove(userId, out _);

            // Unblock user
            _blockedUsers.TryRemove(userId, out _);

            _logger.LogInformation("Reset rate limits for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limits for user {UserId}", userId);
        }
    }

    #endregion

    #region Content Processing

    public async Task<ContentValidationResult> FilterContentAsync(string content)
    {
        if (!_config.EnableContentFiltering)
        {
            return new ContentValidationResult { IsValid = true, SanitizedContent = content };
        }

        await Task.CompletedTask;
        Interlocked.Increment(ref _totalValidations);

        try
        {
            var result = new ContentValidationResult
            {
                IsValid = true,
                SanitizedContent = content,
                RiskLevel = ContentRiskLevel.Low,
                Violations = new List<string>()
            };

            // Check content length
            if (content.Length > _filterConfig.MaxContentLength)
            {
                result.Violations.Add($"Content exceeds maximum length of {_filterConfig.MaxContentLength} characters");
                result.RiskLevel = ContentRiskLevel.Medium;
            }

            // Profanity filter
            if (_filterConfig.EnableProfanityFilter && ContainsProfanity(content))
            {
                result.Violations.Add("Content contains inappropriate language");
                result.RiskLevel = ContentRiskLevel.High;
            }

            // Spam detection
            if (_filterConfig.EnableSpamDetection && IsSpamContent(content))
            {
                result.Violations.Add("Content appears to be spam");
                result.RiskLevel = ContentRiskLevel.High;
            }

            // Phishing detection
            if (_filterConfig.EnablePhishingDetection && ContainsPhishingIndicators(content))
            {
                result.Violations.Add("Content contains potential phishing indicators");
                result.RiskLevel = ContentRiskLevel.Critical;
            }

            // Malicious link detection
            if (_filterConfig.EnableMaliciousLinkDetection)
            {
                var suspiciousLinks = await DetectSuspiciousLinksAsync(content);
                if (suspiciousLinks.Any())
                {
                    result.Violations.Add($"Content contains suspicious links: {string.Join(", ", suspiciousLinks)}");
                    result.RiskLevel = ContentRiskLevel.Critical;
                }
            }

            // Content sanitization
            if (_filterConfig.EnableContentSanitization)
            {
                result.SanitizedContent = await SanitizeContentAsync(content);
                if (result.SanitizedContent != content)
                {
                    Interlocked.Increment(ref _totalSanitized);
                }
            }

            // Determine if content should be blocked
            if (result.Violations.Any())
            {
                Interlocked.Increment(ref _totalViolationsContent);

                if (result.RiskLevel >= ContentRiskLevel.High)
                {
                    result.IsValid = false;
                    Interlocked.Increment(ref _totalBlocked);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering content");
            return new ContentValidationResult { IsValid = true, SanitizedContent = content };
        }
    }

    public async Task<bool> IsContentSafeAsync(string content)
    {
        var result = await FilterContentAsync(content);
        return result.IsValid;
    }

    public async Task<string> SanitizeContentAsync(string content)
    {
        if (!_config.EnableContentFiltering) return content;

        await Task.CompletedTask;

        try
        {
            var sanitized = content;

            // Remove HTML tags
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, "<[^>]*>", "");

            // Remove script tags and content
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove potentially dangerous URLs
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"javascript:|data:|vbscript:", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Normalize whitespace
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", " ").Trim();

            // Limit length
            if (sanitized.Length > _filterConfig.MaxContentLength)
            {
                sanitized = sanitized[.._filterConfig.MaxContentLength] + "...";
            }

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing content");
            return content;
        }
    }

    public async Task<List<string>> DetectSuspiciousLinksAsync(string content)
    {
        await Task.CompletedTask;

        try
        {
            var suspiciousLinks = new List<string>();

            // Simple URL regex
            var urlPattern = @"https?://[^\s]+";
            var matches = System.Text.RegularExpressions.Regex.Matches(content, urlPattern);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var url = match.Value;

                // Check for suspicious patterns
                if (IsSuspiciousUrl(url))
                {
                    suspiciousLinks.Add(url);
                }
            }

            return suspiciousLinks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting suspicious links");
            return new List<string>();
        }
    }

    public async Task<OptimizedContent> OptimizeContentAsync(string content, string deliveryMethod)
    {
        if (!_config.EnableCompression)
        {
            return new OptimizedContent
            {
                Content = content,
                DeliveryMethod = deliveryMethod,
                OriginalLength = content.Length,
                OptimizedLength = content.Length
            };
        }

        await Task.CompletedTask;

        try
        {
            var optimized = content;
            var optimizations = new Dictionary<string, object>();

            // Delivery method specific optimizations
            switch (deliveryMethod.ToLower())
            {
                case "sms":
                    // Truncate for SMS
                    if (optimized.Length > 160)
                    {
                        optimized = optimized[..157] + "...";
                        optimizations["truncated_for_sms"] = true;
                    }
                    break;

                case "push":
                    // Optimize for push notifications
                    if (optimized.Length > 100)
                    {
                        optimized = optimized[..97] + "...";
                        optimizations["truncated_for_push"] = true;
                    }
                    break;

                case "email":
                    // Email can handle longer content, but still optimize
                    optimized = optimized.Replace("\r\n", "\n").Replace("\r", "\n");
                    optimizations["normalized_line_endings"] = true;
                    break;
            }

            return new OptimizedContent
            {
                Content = optimized,
                DeliveryMethod = deliveryMethod,
                Optimizations = optimizations,
                OriginalLength = content.Length,
                OptimizedLength = optimized.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing content for delivery method {DeliveryMethod}", deliveryMethod);
            return new OptimizedContent
            {
                Content = content,
                DeliveryMethod = deliveryMethod,
                OriginalLength = content.Length,
                OptimizedLength = content.Length
            };
        }
    }

    #endregion

    #region Compression and Payload Optimization

    public async Task<CompressedNotificationPayload> CompressPayloadAsync(object payload, OptimizationSettings? settings = null)
    {
        if (!_config.EnableCompression)
        {
            var fallbackJson = System.Text.Json.JsonSerializer.Serialize(payload);
            var fallbackBytes = System.Text.Encoding.UTF8.GetBytes(fallbackJson);

            return new CompressedNotificationPayload
            {
                CompressedData = Convert.ToBase64String(fallbackBytes),
                CompressionMethod = "none",
                OriginalSize = fallbackBytes.Length,
                CompressedSize = fallbackBytes.Length
            };
        }

        settings ??= new OptimizationSettings();

        try
        {
            // First optimize the payload
            var optimizedPayload = await OptimizePayloadAsync(payload, settings);

            // Serialize to JSON
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var jsonString = System.Text.Json.JsonSerializer.Serialize(optimizedPayload, jsonOptions);
            var originalBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
            var originalSize = originalBytes.Length;

            // Check if compression is beneficial
            if (!settings.EnableCompression || originalSize < settings.CompressionThreshold)
            {
                UpdateCompressionStats(originalSize, originalSize);

                return new CompressedNotificationPayload
                {
                    CompressedData = Convert.ToBase64String(originalBytes),
                    CompressionMethod = "none",
                    OriginalSize = originalSize,
                    CompressedSize = originalSize
                };
            }

            // Compress using Gzip
            var compressedBytes = await CompressWithGzipAsync(originalBytes);
            var compressedSize = compressedBytes.Length;

            // Only use compression if it actually reduces size significantly
            if (compressedSize >= originalSize * 0.9) // Less than 10% savings
            {
                UpdateCompressionStats(originalSize, originalSize);

                return new CompressedNotificationPayload
                {
                    CompressedData = Convert.ToBase64String(originalBytes),
                    CompressionMethod = "none",
                    OriginalSize = originalSize,
                    CompressedSize = originalSize
                };
            }

            UpdateCompressionStats(originalSize, compressedSize);

            return new CompressedNotificationPayload
            {
                CompressedData = Convert.ToBase64String(compressedBytes),
                CompressionMethod = "gzip",
                OriginalSize = originalSize,
                CompressedSize = compressedSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress notification payload");

            // Fallback to uncompressed
            var fallbackJson = System.Text.Json.JsonSerializer.Serialize(payload);
            var fallbackBytes = System.Text.Encoding.UTF8.GetBytes(fallbackJson);

            return new CompressedNotificationPayload
            {
                CompressedData = Convert.ToBase64String(fallbackBytes),
                CompressionMethod = "none",
                OriginalSize = fallbackBytes.Length,
                CompressedSize = fallbackBytes.Length
            };
        }
    }

    public async Task<T> DecompressPayloadAsync<T>(CompressedNotificationPayload compressedPayload)
    {
        try
        {
            var compressedBytes = Convert.FromBase64String(compressedPayload.CompressedData);

            byte[] originalBytes;
            if (compressedPayload.CompressionMethod == "gzip")
            {
                originalBytes = await DecompressWithGzipAsync(compressedBytes);
            }
            else
            {
                originalBytes = compressedBytes;
            }

            var jsonString = System.Text.Encoding.UTF8.GetString(originalBytes);
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            var result = System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, jsonOptions);

            if (result == null)
            {
                throw new InvalidOperationException("Deserialization resulted in null");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decompress notification payload");
            throw;
        }
    }

    private async Task<object> OptimizePayloadAsync(object payload, OptimizationSettings settings)
    {
        try
        {
            // Convert to dictionary for manipulation
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            var jsonString = System.Text.Json.JsonSerializer.Serialize(payload, jsonOptions);
            var payloadDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, jsonOptions);

            if (payloadDict == null)
                return payload;

            var optimizedDict = new Dictionary<string, object>();

            foreach (var kvp in payloadDict)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // Use short field names to save bandwidth
                if (settings.UseShortFieldNames)
                {
                    key = GetShortFieldName(key);
                }

                // Remove unnecessary fields
                if (settings.RemoveUnnecessaryFields && IsUnnecessaryField(key, value))
                {
                    continue;
                }

                // Truncate long messages
                if (settings.TruncateLongMessages && IsMessageField(key) && value is string stringValue)
                {
                    if (stringValue.Length > settings.MaxMessageLength)
                    {
                        value = stringValue[..settings.MaxMessageLength] + "...";
                    }
                }

                optimizedDict[key] = value;
            }

            await Task.CompletedTask;
            return optimizedDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize notification payload");
            return payload;
        }
    }

    #endregion

    #region Additional Security and Audit Methods

    public async Task<List<RateLimitViolation>> GetRateLimitViolationsAsync(int userId)
    {
        if (!_config.EnableRateLimiting)
        {
            return new List<RateLimitViolation>();
        }

        try
        {
            // Get violations from in-memory tracking
            var violations = new List<RateLimitViolation>();

            // Get violations from the violations collection
            if (_violations.TryGetValue(userId, out var userViolations))
            {
                lock (userViolations)
                {
                    violations.AddRange(userViolations);
                }
            }

            await Task.CompletedTask;
            return violations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rate limit violations for user {UserId}", userId);
            return new List<RateLimitViolation>();
        }
    }

    public async Task<Dictionary<string, object>> GetRateLimitStatsAsync()
    {
        if (!_config.EnableRateLimiting)
        {
            return new Dictionary<string, object>();
        }

        try
        {
            await Task.CompletedTask;
            return new Dictionary<string, object>
            {
                ["total_requests"] = _totalRequests,
                ["total_violations"] = _totalViolations,
                ["blocked_users_count"] = _blockedUsers.Count,
                ["active_trackers_count"] = _requestTrackers.Count,
                ["last_updated"] = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rate limit stats");
            return new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["last_updated"] = DateTime.UtcNow
            };
        }
    }

    public async Task<ContentValidationResult> ValidateContentAsync(string content, string contentType)
    {
        if (!_config.EnableContentFiltering)
        {
            return new ContentValidationResult
            {
                IsValid = true,
                SanitizedContent = content
            };
        }

        try
        {
            // Use the existing FilterContentAsync method
            var result = await FilterContentAsync(content);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate content of type {ContentType}", contentType);
            return new ContentValidationResult
            {
                IsValid = false,
                SanitizedContent = content,
                Violations = new List<string> { $"Validation failed: {ex.Message}" }
            };
        }
    }

    public async Task<Dictionary<string, object>> GetContentFilterStatsAsync()
    {
        if (!_config.EnableContentFiltering)
        {
            return new Dictionary<string, object>();
        }

        try
        {
            await Task.CompletedTask;
            return new Dictionary<string, object>
            {
                ["total_validations"] = _totalValidations,
                ["total_violations"] = _totalViolationsContent,
                ["total_blocked"] = _totalBlocked,
                ["total_sanitized"] = _totalSanitized,
                ["last_updated"] = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content filter stats");
            return new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["last_updated"] = DateTime.UtcNow
            };
        }
    }

    public async Task<List<Models.NotificationAuditLog>> GetUserAuditLogsAsync(int userId, int count)
    {
        if (!_config.EnableAuditing)
        {
            return new List<Models.NotificationAuditLog>();
        }

        try
        {
            var auditLogs = await _context.NotificationAuditLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.Timestamp)
                .Take(count)
                .ToListAsync();

            return auditLogs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs for user {UserId}", userId);
            return new List<Models.NotificationAuditLog>();
        }
    }

    public async Task<Dictionary<string, object>> GetAuditStatsAsync(DateTime? startDate, DateTime? endDate)
    {
        if (!_config.EnableAuditing)
        {
            return new Dictionary<string, object>();
        }

        try
        {
            var query = _context.NotificationAuditLogs.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(log => log.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(log => log.Timestamp <= endDate.Value);
            }

            var totalLogs = await query.CountAsync();
            var eventTypeBreakdown = await query
                .GroupBy(log => log.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventType, x => x.Count);

            return new Dictionary<string, object>
            {
                ["total_audit_logs"] = totalLogs,
                ["event_type_breakdown"] = eventTypeBreakdown,
                ["start_date"] = startDate,
                ["end_date"] = endDate,
                ["last_updated"] = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit stats");
            return new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["last_updated"] = DateTime.UtcNow
            };
        }
    }

    public async Task<List<SecurityEvent>> GetSecurityEventsAsync(DateTime? since)
    {
        if (!_config.EnableAuditing)
        {
            return new List<SecurityEvent>();
        }

        try
        {
            var query = _context.NotificationAuditLogs.AsQueryable();

            if (since.HasValue)
            {
                query = query.Where(log => log.Timestamp >= since.Value);
            }

            var auditLogs = await query
                .Where(log => log.EventType.Contains("security") || log.EventType.Contains("violation") || log.EventType.Contains("block"))
                .OrderByDescending(log => log.Timestamp)
                .Take(100)
                .ToListAsync();

            var securityEvents = auditLogs.Select(log => new SecurityEvent
            {
                EventId = log.Id.ToString(),
                Timestamp = log.Timestamp,
                EventType = log.EventType,
                UserId = log.UserId,
                Description = log.Description ?? "Security event",
                Severity = DetermineSecuritySeverity(log.EventType),
                AdditionalData = new Dictionary<string, object>
                {
                    ["audit_log_id"] = log.Id,
                    ["notification_type"] = "unknown" // NotificationAuditLog doesn't have NotificationType field
                }
            }).ToList();

            return securityEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events");
            return new List<SecurityEvent>();
        }
    }

    public async Task<bool> BlockUserAsync(int userId, TimeSpan duration, string reason)
    {
        if (!_config.EnableRateLimiting)
        {
            return false;
        }

        try
        {
            var blockUntil = DateTime.UtcNow.Add(duration);
            _blockedUsers[userId] = blockUntil;

            // Log the blocking action
            await LogSecurityEventAsync(new SecurityEvent
            {
                EventType = "user_blocked",
                UserId = userId,
                Description = $"User blocked for {duration.TotalMinutes} minutes. Reason: {reason}",
                Severity = SecurityEventSeverity.High,
                AdditionalData = new Dictionary<string, object>
                {
                    ["duration_minutes"] = duration.TotalMinutes,
                    ["reason"] = reason,
                    ["blocked_until"] = blockUntil
                }
            });

            _logger.LogWarning("Blocked user {UserId} for {Duration} minutes. Reason: {Reason}", userId, duration.TotalMinutes, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnblockUserAsync(int userId)
    {
        if (!_config.EnableRateLimiting)
        {
            return false;
        }

        try
        {
            var wasBlocked = _blockedUsers.TryRemove(userId, out _);

            if (wasBlocked)
            {
                // Log the unblocking action
                await LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = "user_unblocked",
                    UserId = userId,
                    Description = "User manually unblocked",
                    Severity = SecurityEventSeverity.Medium
                });

                _logger.LogInformation("Unblocked user {UserId}", userId);
            }

            return wasBlocked;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unblock user {UserId}", userId);
            return false;
        }
    }

    public async Task<int> CleanupOldLogsAsync(TimeSpan maxAge)
    {
        if (!_config.EnableAuditing)
        {
            return 0;
        }

        try
        {
            var cutoffDate = DateTime.UtcNow - maxAge;
            var oldLogs = await _context.NotificationAuditLogs
                .Where(log => log.Timestamp < cutoffDate)
                .ToListAsync();

            if (oldLogs.Any())
            {
                _context.NotificationAuditLogs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} old audit logs older than {MaxAge}", oldLogs.Count, maxAge);
            }

            return oldLogs.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old audit logs");
            return 0;
        }
    }

    private static SecurityEventSeverity DetermineSecuritySeverity(string eventType)
    {
        return eventType.ToLower() switch
        {
            var type when type.Contains("critical") || type.Contains("attack") => SecurityEventSeverity.Critical,
            var type when type.Contains("violation") || type.Contains("block") => SecurityEventSeverity.High,
            var type when type.Contains("warning") || type.Contains("suspicious") => SecurityEventSeverity.Medium,
            _ => SecurityEventSeverity.Low
        };
    }

    #endregion

    #region Configuration and Health

    public async Task<EnhancementConfiguration> GetConfigurationAsync()
    {
        await Task.CompletedTask;
        return _config;
    }

    public async Task UpdateConfigurationAsync(EnhancementConfiguration configuration)
    {
        await Task.CompletedTask;

        _config.EnableMetrics = configuration.EnableMetrics;
        _config.EnableAuditing = configuration.EnableAuditing;
        _config.EnableRateLimiting = configuration.EnableRateLimiting;
        _config.EnableContentFiltering = configuration.EnableContentFiltering;
        _config.EnableCompression = configuration.EnableCompression;
        _config.FeatureSettings = configuration.FeatureSettings;

        _logger.LogInformation("Updated enhancement configuration");
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();

            // Check if any critical errors occurred recently
            var recentErrors = _completedDeliveries
                .Where(d => !d.Success && d.StartTime > DateTime.UtcNow.AddMinutes(-5))
                .Count();

            // Consider unhealthy if more than 50% of recent deliveries failed
            var recentDeliveries = _completedDeliveries
                .Where(d => d.StartTime > DateTime.UtcNow.AddMinutes(-5))
                .Count();

            if (recentDeliveries > 10 && recentErrors > recentDeliveries * 0.5)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking enhancement service health");
            return false;
        }
    }

    public async Task<EnhancementHealthReport> GetHealthReportAsync()
    {
        try
        {
            var isHealthy = await IsHealthyAsync();
            var report = new EnhancementHealthReport
            {
                IsHealthy = isHealthy,
                LastChecked = DateTime.UtcNow,
                FeaturesEnabled = new Dictionary<string, bool>
                {
                    ["metrics"] = _config.EnableMetrics,
                    ["auditing"] = _config.EnableAuditing,
                    ["rate_limiting"] = _config.EnableRateLimiting,
                    ["content_filtering"] = _config.EnableContentFiltering,
                    ["compression"] = _config.EnableCompression
                },
                Issues = new List<string>()
            };

            if (_config.EnableMetrics)
            {
                report.MetricsStats = new Dictionary<string, object>
                {
                    ["total_sent"] = _totalSent,
                    ["total_delivered"] = _totalDelivered,
                    ["total_failed"] = _totalFailed,
                    ["active_deliveries"] = _activeDeliveries.Count,
                    ["completed_deliveries"] = _completedDeliveries.Count
                };
            }

            if (_config.EnableRateLimiting)
            {
                report.RateLimitStats = new Dictionary<string, object>
                {
                    ["total_requests"] = _totalRequests,
                    ["total_violations"] = _totalViolations,
                    ["blocked_users"] = _blockedUsers.Count,
                    ["active_trackers"] = _requestTrackers.Count
                };
            }

            if (_config.EnableContentFiltering)
            {
                report.ContentFilterStats = new Dictionary<string, object>
                {
                    ["total_validations"] = _totalValidations,
                    ["total_violations"] = _totalViolationsContent,
                    ["total_blocked"] = _totalBlocked,
                    ["total_sanitized"] = _totalSanitized
                };
            }

            if (_config.EnableCompression)
            {
                lock (_statsLock)
                {
                    var totalSavings = _totalOriginalBytes - _totalCompressedBytes;
                    var compressionRatio = _totalOriginalBytes > 0 ? (double)_totalCompressedBytes / _totalOriginalBytes : 1.0;

                    report.CompressionStats = new Dictionary<string, object>
                    {
                        ["total_payloads_processed"] = _totalPayloadsProcessed,
                        ["total_original_bytes"] = _totalOriginalBytes,
                        ["total_compressed_bytes"] = _totalCompressedBytes,
                        ["total_bytes_saved"] = totalSavings,
                        ["compression_ratio"] = compressionRatio
                    };
                }
            }

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating health report");
            return new EnhancementHealthReport
            {
                IsHealthy = false,
                LastChecked = DateTime.UtcNow,
                Issues = new List<string> { $"Health check failed: {ex.Message}" }
            };
        }
    }

    #endregion

    #region Helper Methods

    private RateLimitConfig GetRateLimitConfig(string notificationType)
    {
        // Default configuration - can be made configurable
        return notificationType.ToLower() switch
        {
            "critical" => new RateLimitConfig
            {
                MaxRequestsPerMinute = 10,
                MaxRequestsPerHour = 100,
                MaxRequestsPerDay = 500,
                EnableBurstProtection = true,
                BurstThreshold = 3
            },
            "marketing" => new RateLimitConfig
            {
                MaxRequestsPerMinute = 2,
                MaxRequestsPerHour = 10,
                MaxRequestsPerDay = 50,
                EnableBurstProtection = true,
                BurstThreshold = 1
            },
            _ => new RateLimitConfig
            {
                MaxRequestsPerMinute = 5,
                MaxRequestsPerHour = 50,
                MaxRequestsPerDay = 200,
                EnableBurstProtection = true,
                BurstThreshold = 2
            }
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

    private void RecordViolation(int userId, string notificationType, string limitType, int requestCount, int limit)
    {
        var violation = new RateLimitViolation
        {
            UserId = userId,
            NotificationType = notificationType,
            LimitType = limitType,
            RequestCount = requestCount,
            Limit = limit,
            ViolationTime = DateTime.UtcNow
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
            _blockedUsers.TryAdd(userId, DateTime.UtcNow.AddHours(1));
            _logger.LogWarning("Auto-blocked user {UserId} for 1 hour due to excessive rate limit violations", userId);
        }
    }

    private bool ContainsProfanity(string content)
    {
        // Simple profanity detection - in production, use a proper library
        var profanityWords = new[] { "spam", "scam", "fake", "fraud" };
        var lowerContent = content.ToLowerInvariant();
        return profanityWords.Any(word => lowerContent.Contains(word));
    }

    private bool IsSpamContent(string content)
    {
        // Simple spam detection heuristics
        var spamIndicators = new[]
        {
            "click here", "act now", "limited time", "free money", "guaranteed",
            "no risk", "100% free", "make money fast", "work from home"
        };

        var lowerContent = content.ToLowerInvariant();
        var indicatorCount = spamIndicators.Count(indicator => lowerContent.Contains(indicator));

        // Consider spam if multiple indicators are present
        return indicatorCount >= 2;
    }

    private bool ContainsPhishingIndicators(string content)
    {
        // Simple phishing detection
        var phishingIndicators = new[]
        {
            "verify your account", "suspended account", "click to verify",
            "update payment", "confirm identity", "urgent action required"
        };

        var lowerContent = content.ToLowerInvariant();
        return phishingIndicators.Any(indicator => lowerContent.Contains(indicator));
    }

    private bool IsSuspiciousUrl(string url)
    {
        try
        {
            var uri = new Uri(url);

            // Check for suspicious patterns
            var suspiciousPatterns = new[]
            {
                "bit.ly", "tinyurl", "t.co", // URL shorteners
                "phishing", "malware", "virus", // Obvious bad words
                "secure-bank", "paypal-verify" // Common phishing patterns
            };

            return suspiciousPatterns.Any(pattern => uri.Host.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // If URL parsing fails, consider it suspicious
            return true;
        }
    }

    private async Task<byte[]> CompressWithGzipAsync(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
        {
            await gzip.WriteAsync(data);
        }
        return output.ToArray();
    }

    private async Task<byte[]> DecompressWithGzipAsync(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();

        await gzip.CopyToAsync(output);
        return output.ToArray();
    }

    private void UpdateCompressionStats(int originalSize, int compressedSize)
    {
        lock (_statsLock)
        {
            _totalPayloadsProcessed++;
            _totalOriginalBytes += originalSize;
            _totalCompressedBytes += compressedSize;
        }
    }

    private static string GetShortFieldName(string fieldName)
    {
        return fieldName.ToLower() switch
        {
            "title" => "t",
            "body" => "b",
            "message" => "m",
            "username" => "u",
            "timestamp" => "ts",
            "notification_type" => "nt",
            "conversation_id" => "cid",
            "post_id" => "pid",
            "user_id" => "uid",
            "data" => "d",
            "metadata" => "md",
            _ => fieldName
        };
    }

    private static bool IsUnnecessaryField(string fieldName, object? value)
    {
        // Remove null or empty values
        if (value == null)
            return true;

        if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            return true;

        // Remove debug/internal fields in production
        var unnecessaryFields = new[] { "debug", "internal", "trace_id", "request_id" };
        return unnecessaryFields.Contains(fieldName.ToLower());
    }

    private static bool IsMessageField(string fieldName)
    {
        var messageFields = new[] { "body", "message", "content", "text", "description" };
        return messageFields.Contains(fieldName.ToLower());
    }

    #endregion
}
