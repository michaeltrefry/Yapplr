using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for user activity tracking commands
/// </summary>
public class TrackUserActivityCommandHandler : BaseCommandHandler<TrackUserActivityCommand>
{
    private readonly YapplrDbContext _dbContext;
    private readonly IUserCacheService _userCacheService;

    public TrackUserActivityCommandHandler(
        YapplrDbContext dbContext,
        IUserCacheService userCacheService,
        ILogger<TrackUserActivityCommandHandler> logger) : base(logger)
    {
        _dbContext = dbContext;
        _userCacheService = userCacheService;
    }

    protected override async Task HandleAsync(TrackUserActivityCommand command, ConsumeContext<TrackUserActivityCommand> context)
    {
        try
        {
            // Get user to validate they exist
            var user = await _userCacheService.GetUserByIdAsync(command.TargetUserId);
            if (user == null)
            {
                Logger.LogWarning("User {UserId} not found for activity tracking", command.TargetUserId);
                return;
            }

            // Store detailed activity record for analytics
            // Note: In a real implementation, you might want to use a separate analytics database
            // or a time-series database like InfluxDB for better performance
            
            Logger.LogInformation("Tracked user activity: User {UserId} performed {ActivityType} at {Timestamp}",
                command.TargetUserId, command.ActivityType, command.Timestamp);

            // For high-volume activities like "view" or "scroll", you might want to batch these
            // or use a different storage mechanism to avoid overwhelming the database
            if (ShouldPersistActivity(command.ActivityType))
            {
                // Here you would typically insert into an analytics table
                // For now, we'll just log it
                Logger.LogDebug("Persisting activity record for user {UserId}: {ActivityType}",
                    command.TargetUserId, command.ActivityType);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to track user activity for user {UserId}: {Error}",
                command.TargetUserId, ex.Message);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }

    private static bool ShouldPersistActivity(string activityType)
    {
        // Only persist significant activities to avoid database overload
        return activityType switch
        {
            "login" => true,
            "logout" => true,
            "post_created" => true,
            "comment_created" => true,
            "like_given" => true,
            "follow" => true,
            "unfollow" => true,
            _ => false
        };
    }
}

/// <summary>
/// Handler for content engagement tracking commands
/// </summary>
public class TrackContentEngagementCommandHandler : BaseCommandHandler<TrackContentEngagementCommand>
{
    private readonly YapplrDbContext _dbContext;

    public TrackContentEngagementCommandHandler(
        YapplrDbContext dbContext,
        ILogger<TrackContentEngagementCommandHandler> logger) : base(logger)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleAsync(TrackContentEngagementCommand command, ConsumeContext<TrackContentEngagementCommand> context)
    {
        try
        {
            // Update engagement metrics
            // In a real implementation, you'd likely have dedicated analytics tables
            
            Logger.LogInformation("Tracked content engagement: {ContentType} {ContentId} received {EngagementType} from user {UserId}",
                command.ContentType, command.ContentId, command.EngagementType, command.EngagingUserId);

            // Update real-time engagement counters if needed
            if (command.EngagementType == "like")
            {
                await UpdateLikeCountAsync(command.ContentType, command.ContentId);
            }
            else if (command.EngagementType == "comment")
            {
                await UpdateCommentCountAsync(command.ContentType, command.ContentId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to track content engagement for {ContentType} {ContentId}: {Error}",
                command.ContentType, command.ContentId, ex.Message);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }

    private async Task UpdateLikeCountAsync(string contentType, int contentId)
    {
        if (contentType == "post")
        {
            // For now, just log the update since the counts are computed properties
            // In a real implementation, you might cache these counts or update denormalized fields
            Logger.LogInformation("Like count update requested for post {PostId}", contentId);
        }
    }

    private async Task UpdateCommentCountAsync(string contentType, int contentId)
    {
        if (contentType == "post")
        {
            // For now, just log the update since the counts are computed properties
            // In a real implementation, you might cache these counts or update denormalized fields
            Logger.LogInformation("Comment count update requested for post {PostId}", contentId);
        }
    }
}

/// <summary>
/// Handler for tag analytics updates
/// </summary>
public class UpdateTagAnalyticsCommandHandler : BaseCommandHandler<UpdateTagAnalyticsCommand>
{
    private readonly ITagAnalyticsService _tagAnalyticsService;

    public UpdateTagAnalyticsCommandHandler(
        ITagAnalyticsService tagAnalyticsService,
        ILogger<UpdateTagAnalyticsCommandHandler> logger) : base(logger)
    {
        _tagAnalyticsService = tagAnalyticsService;
    }

    protected override async Task HandleAsync(UpdateTagAnalyticsCommand command, ConsumeContext<UpdateTagAnalyticsCommand> context)
    {
        try
        {
            await _tagAnalyticsService.UpdateTagMetricsAsync(
                command.TagId,
                command.Action,
                command.Timestamp);

            Logger.LogInformation("Updated tag analytics for tag {TagId}: {Action}",
                command.TagId, command.Action);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update tag analytics for tag {TagId}: {Error}",
                command.TagId, ex.Message);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }
}

/// <summary>
/// Handler for trust score updates
/// </summary>
public class UpdateUserTrustScoreCommandHandler : BaseCommandHandler<UpdateUserTrustScoreCommand>
{
    private readonly YapplrDbContext _dbContext;

    public UpdateUserTrustScoreCommandHandler(
        YapplrDbContext dbContext,
        ILogger<UpdateUserTrustScoreCommandHandler> logger) : base(logger)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleAsync(UpdateUserTrustScoreCommand command, ConsumeContext<UpdateUserTrustScoreCommand> context)
    {
        try
        {
            var user = await _dbContext.Users.FindAsync(command.TargetUserId);
            if (user == null)
            {
                Logger.LogWarning("User {UserId} not found for trust score update", command.TargetUserId);
                return;
            }

            // Update trust score (assuming there's a TrustScore property on User)
            // In a real implementation, you might have a separate UserTrustScore table
            var currentScore = user.TrustScore ?? 1.0f;
            var newScore = Math.Max(0.0f, Math.Min(1.0f, currentScore + command.ScoreChange));
            
            user.TrustScore = newScore;
            await _dbContext.SaveChangesAsync();

            Logger.LogInformation("Updated trust score for user {UserId}: {OldScore} -> {NewScore} (change: {Change}, reason: {Reason})",
                command.TargetUserId, currentScore, newScore, command.ScoreChange, command.Reason);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update trust score for user {UserId}: {Error}",
                command.TargetUserId, ex.Message);
            // Don't throw - trust score updates shouldn't break the main flow
        }
    }
}
