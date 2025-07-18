using Yapplr.Api.Data;

namespace Yapplr.Api.Services;

public class TrustBasedModerationService : ITrustBasedModerationService
{
    private readonly YapplrDbContext _context;
    private readonly ITrustScoreService _trustScoreService;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<TrustBasedModerationService> _logger;

    // Trust score thresholds
    private const float HIGH_TRUST_THRESHOLD = 0.8f;
    private const float MEDIUM_TRUST_THRESHOLD = 0.5f;
    private const float LOW_TRUST_THRESHOLD = 0.3f;
    private const float VERY_LOW_TRUST_THRESHOLD = 0.1f;

    public TrustBasedModerationService(
        YapplrDbContext context,
        ITrustScoreService trustScoreService,
        IAnalyticsService analyticsService,
        ILogger<TrustBasedModerationService> logger)
    {
        _context = context;
        _trustScoreService = trustScoreService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<float> GetRateLimitMultiplierAsync(int userId)
    {
        try
        {
            var trustScore = await _analyticsService.GetCurrentUserTrustScoreAsync(userId);

            // Higher trust = more lenient rate limits (higher multiplier)
            return trustScore switch
            {
                >= HIGH_TRUST_THRESHOLD => 2.0f,      // Double the rate limit
                >= MEDIUM_TRUST_THRESHOLD => 1.5f,    // 50% more requests
                >= LOW_TRUST_THRESHOLD => 1.0f,       // Normal rate limit
                >= VERY_LOW_TRUST_THRESHOLD => 0.5f,  // Half the rate limit
                _ => 0.25f                             // Quarter rate limit for very low trust
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit multiplier for user {UserId}", userId);
            return 1.0f; // Default to normal rate limit on error
        }
    }

    public async Task<bool> ShouldAutoHideContentAsync(int authorId, string? contentType)
    {
        if (contentType == null) return false;
        try
        {
            var trustScore = await _analyticsService.GetCurrentUserTrustScoreAsync(authorId);

            // Auto-hide content from very low trust users
            if (trustScore < VERY_LOW_TRUST_THRESHOLD)
            {
                _logger.LogInformation("Auto-hiding {ContentType} from low trust user {UserId} (score: {TrustScore})",
                    contentType, authorId, trustScore);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking auto-hide for user {UserId}", authorId);
            return false; // Don't auto-hide on error
        }
    }

    public async Task<int> GetModerationPriorityAsync(int authorId, string contentType)
    {
        try
        {
            var trustScore = await _analyticsService.GetCurrentUserTrustScoreAsync(authorId);

            // Lower trust = higher priority for moderation (lower number = higher priority)
            return trustScore switch
            {
                < VERY_LOW_TRUST_THRESHOLD => 1,  // Highest priority
                < LOW_TRUST_THRESHOLD => 2,       // High priority
                < MEDIUM_TRUST_THRESHOLD => 3,    // Medium priority
                < HIGH_TRUST_THRESHOLD => 4,      // Low priority
                _ => 5                             // Lowest priority for high trust users
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation priority for user {UserId}", authorId);
            return 3; // Default to medium priority on error
        }
    }

    public async Task<bool> CanPerformActionAsync(int userId, TrustRequiredAction action)
    {
        try
        {
            var trustScore = await _analyticsService.GetCurrentUserTrustScoreAsync(userId);

            return action switch
            {
                TrustRequiredAction.CreatePost => trustScore >= 0.1f,
                TrustRequiredAction.CreateComment => trustScore >= 0.1f,
                TrustRequiredAction.LikeContent => trustScore >= 0.05f,
                TrustRequiredAction.ReportContent => trustScore >= 0.2f,
                TrustRequiredAction.SendMessage => trustScore >= 0.3f,
                TrustRequiredAction.FollowUsers => trustScore >= 0.15f,
                TrustRequiredAction.CreateMultiplePosts => trustScore >= 0.4f,
                TrustRequiredAction.MentionUsers => trustScore >= 0.25f,
                _ => true // Allow unknown actions by default
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking action permission for user {UserId}", userId);
            return true; // Allow action on error to avoid blocking users
        }
    }

    public async Task<ContentVisibilityLevel> GetContentVisibilityLevelAsync(int authorId)
    {
        try
        {
            var trustScore = await _analyticsService.GetCurrentUserTrustScoreAsync(authorId);

            return trustScore switch
            {
                >= HIGH_TRUST_THRESHOLD => ContentVisibilityLevel.FullVisibility,
                >= MEDIUM_TRUST_THRESHOLD => ContentVisibilityLevel.NormalVisibility,
                >= LOW_TRUST_THRESHOLD => ContentVisibilityLevel.ReducedVisibility,
                >= VERY_LOW_TRUST_THRESHOLD => ContentVisibilityLevel.LimitedVisibility,
                _ => ContentVisibilityLevel.Hidden
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content visibility for user {UserId}", authorId);
            return ContentVisibilityLevel.NormalVisibility; // Default to normal on error
        }
    }

    public async Task<float> GetReportReviewThresholdAsync(int reporterId)
    {
        try
        {
            var trustScore = await _analyticsService.GetCurrentUserTrustScoreAsync(reporterId);

            // Higher trust reporters get lower thresholds (reports are taken more seriously)
            return trustScore switch
            {
                >= HIGH_TRUST_THRESHOLD => 0.3f,      // Low threshold - trust their reports
                >= MEDIUM_TRUST_THRESHOLD => 0.5f,    // Medium threshold
                >= LOW_TRUST_THRESHOLD => 0.7f,       // Higher threshold
                _ => 0.9f                              // Very high threshold for low trust users
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report review threshold for user {UserId}", reporterId);
            return 0.5f; // Default threshold on error
        }
    }
}