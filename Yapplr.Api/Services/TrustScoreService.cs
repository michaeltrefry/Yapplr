using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Models.Analytics;
using Serilog.Context;

namespace Yapplr.Api.Services;

/// <summary>
/// Service for calculating and managing user trust scores based on behavior patterns
/// </summary>
public class TrustScoreService : ITrustScoreService
{
    private readonly YapplrDbContext _context;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<TrustScoreService> _logger;

    // Trust score calculation weights
    private readonly Dictionary<string, float> _weights = new()
    {
        ["baseScore"] = 1.0f,
        ["accountAge"] = 0.1f,
        ["emailVerified"] = 0.05f,
        ["profileCompleteness"] = 0.05f,
        ["contentQuality"] = 0.3f,
        ["engagementRatio"] = 0.2f,
        ["moderationHistory"] = -0.4f,
        ["reportingAccuracy"] = 0.1f,
        ["activityConsistency"] = 0.1f
    };

    public TrustScoreService(YapplrDbContext context, IAnalyticsService analyticsService, ILogger<TrustScoreService> logger)
    {
        _context = context;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<float> CalculateUserTrustScoreAsync(int userId, bool recalculateFromScratch = false)
    {
        using var operationScope = LogContext.PushProperty("Operation", "CalculateTrustScore");
        using var userScope = LogContext.PushProperty("UserId", userId);
        using var recalcScope = LogContext.PushProperty("RecalculateFromScratch", recalculateFromScratch);

        try
        {
            _logger.LogDebug("Starting trust score calculation for user {UserId} (recalculate: {RecalculateFromScratch})",
                userId, recalculateFromScratch);

            var user = await _context.Users
                .Include(u => u.Posts)
                .Include(u => u.Likes)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("Cannot calculate trust score for non-existent user {UserId}", userId);
                return 1.0f; // Default score
            }

            using var usernameScope = LogContext.PushProperty("Username", user.Username);

            var factors = await CalculateTrustFactorsAsync(user);
            var trustScore = CalculateWeightedScore(factors);

            // Clamp score between 0.0 and 1.0
            trustScore = Math.Max(0.0f, Math.Min(1.0f, trustScore));

            _logger.LogDebug("Calculated trust score for user {UserId}: {TrustScore}", userId, trustScore);
            return trustScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating trust score for user {UserId}", userId);
            // Try to get current score from database on error
            var currentUser = await _context.Users.FindAsync(userId);
            return currentUser?.TrustScore ?? 1.0f; // Return current score on error
        }
    }

    public async Task<float> UpdateTrustScoreForActionAsync(int userId, TrustScoreAction action, 
        string? relatedEntityType = null, int? relatedEntityId = null, string? metadata = null)
    {
        try
        {
            var scoreChange = GetScoreChangeForAction(action);
            var reason = MapActionToReason(action);

            await _analyticsService.UpdateUserTrustScoreAsync(
                userId: userId,
                scoreChange: scoreChange,
                reason: reason,
                details: $"Action: {action}",
                relatedEntityType: relatedEntityType,
                relatedEntityId: relatedEntityId,
                calculatedBy: "TrustScoreService",
                isAutomatic: true,
                confidence: GetConfidenceForAction(action)
            );

            var newScore = await _analyticsService.GetCurrentUserTrustScoreAsync(userId);
            _logger.LogInformation("Updated trust score for user {UserId} due to {Action}: change {Change}, new score {NewScore}",
                userId, action, scoreChange, newScore);

            return newScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trust score for user {UserId} action {Action}", userId, action);
            return await _analyticsService.GetCurrentUserTrustScoreAsync(userId);
        }
    }

    public async Task<Dictionary<string, object>> GetTrustScoreFactorsAsync(int userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Posts)
                .Include(u => u.Likes)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new Dictionary<string, object> { ["error"] = "User not found" };
            }

            return await CalculateTrustFactorsAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trust score factors for user {UserId}", userId);
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    private async Task<Dictionary<string, object>> CalculateTrustFactorsAsync(User user)
    {
        var factors = new Dictionary<string, object>();

        // Base score
        factors["baseScore"] = 1.0f;

        // Account age factor (newer accounts have slightly lower trust)
        var accountAgeDays = (DateTime.UtcNow - user.CreatedAt).TotalDays;
        factors["accountAge"] = Math.Min(1.0f, (float)(accountAgeDays / 30.0)); // Full trust after 30 days

        // Email verification
        factors["emailVerified"] = user.EmailVerified ? 1.0f : 0.0f;

        // Profile completeness
        var completeness = CalculateProfileCompleteness(user);
        factors["profileCompleteness"] = completeness;

        // Content quality (based on engagement ratios)
        var contentQuality = await CalculateContentQualityAsync(user.Id);
        factors["contentQuality"] = contentQuality;

        // Engagement ratio (likes given vs received)
        var engagementRatio = await CalculateEngagementRatioAsync(user.Id);
        factors["engagementRatio"] = engagementRatio;

        // Moderation history (negative impact)
        var moderationPenalty = await CalculateModerationPenaltyAsync(user.Id);
        factors["moderationHistory"] = moderationPenalty;

        // Reporting accuracy
        var reportingAccuracy = await CalculateReportingAccuracyAsync(user.Id);
        factors["reportingAccuracy"] = reportingAccuracy;

        // Activity consistency
        var activityConsistency = await CalculateActivityConsistencyAsync(user.Id);
        factors["activityConsistency"] = activityConsistency;

        return factors;
    }

    private float CalculateWeightedScore(Dictionary<string, object> factors)
    {
        float score = 0.0f;

        foreach (var factor in factors)
        {
            if (_weights.TryGetValue(factor.Key, out var weight) && factor.Value is float value)
            {
                score += weight * value;
            }
        }

        return score;
    }

    private float CalculateProfileCompleteness(User user)
    {
        var completeness = 0.0f;
        var totalFields = 5.0f;

        if (!string.IsNullOrEmpty(user.Bio)) completeness += 1.0f;
        if (user.Birthday.HasValue) completeness += 1.0f;
        if (!string.IsNullOrEmpty(user.Pronouns)) completeness += 1.0f;
        if (!string.IsNullOrEmpty(user.Tagline)) completeness += 1.0f;
        if (!string.IsNullOrEmpty(user.ProfileImageFileName)) completeness += 1.0f;

        return completeness / totalFields;
    }

    private async Task<float> CalculateContentQualityAsync(int userId)
    {
        // Calculate based on engagement rates of user's content
        var posts = await _context.Posts
            .Where(p => p.UserId == userId && !p.IsHidden && p.PostType == PostType.Post)
            .Include(p => p.Likes)
            .Include(p => p.Children.Where(c => c.PostType == PostType.Comment))
            .ToListAsync();

        if (!posts.Any()) return 0.5f; // Neutral for users with no content

        var totalEngagement = posts.Sum(p => p.Likes.Count + p.Children.Count(c => c.PostType == PostType.Comment));
        var avgEngagementPerPost = (float)totalEngagement / posts.Count;

        // Normalize to 0-1 scale (assuming 5+ engagements per post is high quality)
        return Math.Min(1.0f, avgEngagementPerPost / 5.0f);
    }

    private async Task<float> CalculateEngagementRatioAsync(int userId)
    {
        var likesGiven = await _context.Likes.CountAsync(l => l.UserId == userId);
        var likesReceived = await _context.Likes
            .Where(l => l.Post.UserId == userId)
            .CountAsync();

        if (likesReceived == 0) return 0.5f; // Neutral for new users

        var ratio = (float)likesGiven / likesReceived;
        // Ideal ratio is around 1:1 to 2:1 (giving as much as receiving)
        return Math.Min(1.0f, ratio / 2.0f);
    }

    private async Task<float> CalculateModerationPenaltyAsync(int userId)
    {
        var moderationActions = await _context.AuditLogs
            .Where(al => al.TargetUserId == userId && 
                        (al.Action == AuditAction.PostHidden || 
                         al.Action == AuditAction.PostDeleted ||
                         al.Action == AuditAction.CommentHidden ||
                         al.Action == AuditAction.CommentDeleted ||
                         al.Action == AuditAction.UserSuspended))
            .CountAsync();

        // Each moderation action reduces trust
        return Math.Max(0.0f, 1.0f - (moderationActions * 0.1f));
    }

    private async Task<float> CalculateReportingAccuracyAsync(int userId)
    {
        var reportsSubmitted = await _context.UserReports
            .Where(ur => ur.ReportedByUserId == userId)
            .ToListAsync();

        if (!reportsSubmitted.Any()) return 0.5f; // Neutral for users who haven't reported

        var actionTakenReports = reportsSubmitted.Count(r => r.Status == UserReportStatus.ActionTaken);
        var accuracy = (float)actionTakenReports / reportsSubmitted.Count;

        return accuracy;
    }

    private async Task<float> CalculateActivityConsistencyAsync(int userId)
    {
        var activities = await _context.UserActivities
            .Where(ua => ua.UserId == userId && ua.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(ua => ua.CreatedAt.Date)
            .CountAsync();

        // Consistent activity over 30 days (aim for 15+ active days)
        return Math.Min(1.0f, activities / 15.0f);
    }

    private float GetScoreChangeForAction(TrustScoreAction action)
    {
        return action switch
        {
            TrustScoreAction.PostCreated => 0.01f,
            TrustScoreAction.CommentCreated => 0.005f,
            TrustScoreAction.LikeGiven => 0.001f,
            TrustScoreAction.QualityContentCreated => 0.05f,
            TrustScoreAction.HelpfulReport => 0.02f,
            TrustScoreAction.EmailVerified => 0.05f,
            TrustScoreAction.ProfileCompleted => 0.03f,
            TrustScoreAction.ConsistentActivity => 0.01f,
            
            TrustScoreAction.ContentReported => -0.02f,
            TrustScoreAction.ContentHidden => -0.1f,
            TrustScoreAction.ContentDeleted => -0.15f,
            TrustScoreAction.SpamDetected => -0.2f,
            TrustScoreAction.UserSuspended => -0.3f,
            TrustScoreAction.UserBanned => -0.5f,
            TrustScoreAction.FalseReport => -0.05f,
            TrustScoreAction.ExcessiveReporting => -0.1f,
            TrustScoreAction.InactivityDecay => -0.005f,
            
            TrustScoreAction.AppealApproved => 0.1f,
            TrustScoreAction.AppealDenied => -0.05f,
            TrustScoreAction.AdminAdjustment => 0.0f, // Handled separately
            
            _ => 0.0f
        };
    }

    private TrustScoreChangeReason MapActionToReason(TrustScoreAction action)
    {
        return action switch
        {
            TrustScoreAction.PostCreated or TrustScoreAction.CommentCreated or TrustScoreAction.LikeGiven => TrustScoreChangeReason.PositiveEngagement,
            TrustScoreAction.QualityContentCreated => TrustScoreChangeReason.QualityContent,
            TrustScoreAction.HelpfulReport => TrustScoreChangeReason.CommunityFeedback,
            TrustScoreAction.EmailVerified => TrustScoreChangeReason.VerificationComplete,
            TrustScoreAction.ContentReported => TrustScoreChangeReason.UserReport,
            TrustScoreAction.ContentHidden or TrustScoreAction.ContentDeleted => TrustScoreChangeReason.ContentModeration,
            TrustScoreAction.SpamDetected => TrustScoreChangeReason.SpamDetection,
            TrustScoreAction.UserSuspended or TrustScoreAction.UserBanned => TrustScoreChangeReason.ContentModeration,
            TrustScoreAction.FalseReport or TrustScoreAction.ExcessiveReporting => TrustScoreChangeReason.UserReport,
            TrustScoreAction.InactivityDecay => TrustScoreChangeReason.AutomaticDecay,
            TrustScoreAction.AppealApproved => TrustScoreChangeReason.SuccessfulAppeal,
            TrustScoreAction.AdminAdjustment => TrustScoreChangeReason.AdminAdjustment,
            _ => TrustScoreChangeReason.AdminAdjustment
        };
    }

    private float GetConfidenceForAction(TrustScoreAction action)
    {
        return action switch
        {
            TrustScoreAction.UserSuspended or TrustScoreAction.UserBanned => 0.95f,
            TrustScoreAction.ContentHidden or TrustScoreAction.ContentDeleted => 0.9f,
            TrustScoreAction.SpamDetected => 0.85f,
            TrustScoreAction.EmailVerified => 1.0f,
            TrustScoreAction.QualityContentCreated => 0.8f,
            TrustScoreAction.HelpfulReport => 0.75f,
            _ => 0.7f
        };
    }

    public async Task<int> ApplyInactivityDecayAsync(int inactiveDays = 30, float decayRate = 0.005f)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-inactiveDays);

            var inactiveUsers = await _context.Users
                .Where(u => u.LastSeenAt < cutoffDate && u.TrustScore > 0.1f) // Don't decay below 0.1
                .ToListAsync();

            var processedCount = 0;

            foreach (var user in inactiveUsers)
            {
                var daysSinceLastSeen = (DateTime.UtcNow - user.LastSeenAt).TotalDays;
                var decayDays = Math.Max(0, daysSinceLastSeen - inactiveDays);
                var totalDecay = (float)(decayDays * decayRate);

                if (totalDecay > 0)
                {
                    await UpdateTrustScoreForActionAsync(user.Id, TrustScoreAction.InactivityDecay,
                        metadata: $"Inactive for {daysSinceLastSeen:F0} days");
                    processedCount++;
                }
            }

            _logger.LogInformation("Applied inactivity decay to {Count} users", processedCount);
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying inactivity decay");
            return 0;
        }
    }

    public async Task<int> RecalculateAllTrustScoresAsync(int batchSize = 100)
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var processedCount = 0;

            for (int skip = 0; skip < totalUsers; skip += batchSize)
            {
                var userBatch = await _context.Users
                    .OrderBy(u => u.Id)
                    .Skip(skip)
                    .Take(batchSize)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var userId in userBatch)
                {
                    try
                    {
                        var newScore = await CalculateUserTrustScoreAsync(userId, recalculateFromScratch: true);

                        // Update the user's trust score directly
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null && Math.Abs(user.TrustScore ?? 1.0f - newScore) > 0.01f) // Only update if significant change
                        {
                            var oldScore = user.TrustScore ?? 1.0f;
                            user.TrustScore = newScore;

                            // Record the recalculation in history
                            var history = new UserTrustScoreHistory
                            {
                                UserId = userId,
                                PreviousScore = oldScore,
                                NewScore = newScore,
                                ScoreChange = newScore - oldScore,
                                Reason = TrustScoreChangeReason.AdminAdjustment,
                                Details = "Bulk recalculation",
                                CalculatedBy = "TrustScoreService",
                                IsAutomatic = true,
                                Confidence = 0.9f,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.UserTrustScoreHistories.Add(history);
                        }

                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error recalculating trust score for user {UserId}", userId);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogDebug("Processed batch {Skip}-{End} of {Total} users", skip, skip + userBatch.Count, totalUsers);
            }

            _logger.LogInformation("Recalculated trust scores for {Count} users", processedCount);
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk trust score recalculation");
            return 0;
        }
    }

    public async Task<List<int>> GetUsersWithLowTrustScoresAsync(float threshold = 0.3f, int limit = 100)
    {
        try
        {
            return await _context.Users
                .Where(u => u.TrustScore < threshold)
                .OrderBy(u => u.TrustScore)
                .Take(limit)
                .Select(u => u.Id)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with low trust scores");
            return new List<int>();
        }
    }

    public async Task<Dictionary<string, object>> GetTrustScoreStatisticsAsync()
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.TrustScore.HasValue)
                .Select(u => u.TrustScore!.Value)
                .ToListAsync();

            if (!users.Any())
            {
                return new Dictionary<string, object>
                {
                    ["totalUsers"] = 0,
                    ["averageScore"] = 0.0f,
                    ["medianScore"] = 0.0f,
                    ["distribution"] = new Dictionary<string, int>()
                };
            }

            var sortedScores = users.OrderBy(s => s).ToList();
            var median = sortedScores.Count % 2 == 0
                ? (sortedScores[sortedScores.Count / 2 - 1] + sortedScores[sortedScores.Count / 2]) / 2.0f
                : sortedScores[sortedScores.Count / 2];

            var distribution = new Dictionary<string, int>
            {
                ["0.0-0.2"] = users.Count(s => s < 0.2f),
                ["0.2-0.4"] = users.Count(s => s >= 0.2f && s < 0.4f),
                ["0.4-0.6"] = users.Count(s => s >= 0.4f && s < 0.6f),
                ["0.6-0.8"] = users.Count(s => s >= 0.6f && s < 0.8f),
                ["0.8-1.0"] = users.Count(s => s >= 0.8f)
            };

            return new Dictionary<string, object>
            {
                ["totalUsers"] = users.Count,
                ["averageScore"] = users.Average(),
                ["medianScore"] = median,
                ["minScore"] = users.Min(),
                ["maxScore"] = users.Max(),
                ["distribution"] = distribution
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trust score statistics");
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }
}
