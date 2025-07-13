using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Common;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for user activity tracking commands
/// </summary>
public class TrackUserActivityCommandHandler : BaseCommandHandler<TrackUserActivityCommand>
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICachingService _cachingService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TrackUserActivityCommandHandler(
        IAnalyticsService analyticsService,
        ICachingService cachingService,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TrackUserActivityCommandHandler> logger) : base(logger)
    {
        _analyticsService = analyticsService;
        _cachingService = cachingService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task HandleAsync(TrackUserActivityCommand command, ConsumeContext<TrackUserActivityCommand> context)
    {
        try
        {
            // Get user to validate they exist
            var user = await _cachingService.GetUserByIdAsync(command.TargetUserId, _serviceScopeFactory);
            if (user == null)
            {
                Logger.LogWarning("User {UserId} not found for activity tracking", command.TargetUserId);
                return;
            }

            // Convert string activity type to enum
            if (!Enum.TryParse<ActivityType>(command.ActivityType, true, out var activityType))
            {
                Logger.LogWarning("Unknown activity type: {ActivityType}", command.ActivityType);
                return;
            }

            // Track the activity using the analytics service
            await _analyticsService.TrackUserActivityAsync(
                userId: command.TargetUserId,
                activityType: activityType,
                targetEntityType: null, // Not available in current command
                targetEntityId: null, // Not available in current command
                metadata: command.Metadata?.ToString(), // Convert dictionary to string
                sessionId: null); // Not available in current command

            Logger.LogInformation("Tracked user activity: User {UserId} performed {ActivityType} at {Timestamp}",
                command.TargetUserId, command.ActivityType, command.Timestamp);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to track user activity for user {UserId}: {Error}",
                command.TargetUserId, ex.Message);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }
}

/// <summary>
/// Handler for content engagement tracking commands
/// </summary>
public class TrackContentEngagementCommandHandler : BaseCommandHandler<TrackContentEngagementCommand>
{
    private readonly IAnalyticsService _analyticsService;

    public TrackContentEngagementCommandHandler(
        IAnalyticsService analyticsService,
        ILogger<TrackContentEngagementCommandHandler> logger) : base(logger)
    {
        _analyticsService = analyticsService;
    }

    protected override async Task HandleAsync(TrackContentEngagementCommand command, ConsumeContext<TrackContentEngagementCommand> context)
    {
        try
        {
            // Convert string types to enums
            if (!Enum.TryParse<ContentType>(command.ContentType, true, out var contentType))
            {
                Logger.LogWarning("Unknown content type: {ContentType}", command.ContentType);
                return;
            }

            if (!Enum.TryParse<EngagementType>(command.EngagementType, true, out var engagementType))
            {
                Logger.LogWarning("Unknown engagement type: {EngagementType}", command.EngagementType);
                return;
            }

            // Track the engagement using the analytics service
            await _analyticsService.TrackContentEngagementAsync(
                userId: command.EngagingUserId ?? 0, // Use 0 for anonymous users
                contentType: contentType,
                contentId: command.ContentId,
                engagementType: engagementType,
                contentOwnerId: command.AuthorId, // Use AuthorId as ContentOwnerId
                source: null, // Not available in current command
                metadata: null, // Not available in current command
                sessionId: null); // Not available in current command

            Logger.LogInformation("Tracked content engagement: {ContentType} {ContentId} received {EngagementType} from user {UserId}",
                command.ContentType, command.ContentId, command.EngagementType, command.EngagingUserId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to track content engagement for {ContentType} {ContentId}: {Error}",
                command.ContentType, command.ContentId, ex.Message);
            // Don't throw - analytics failures shouldn't break the main flow
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
    private readonly IAnalyticsService _analyticsService;

    public UpdateUserTrustScoreCommandHandler(
        IAnalyticsService analyticsService,
        ILogger<UpdateUserTrustScoreCommandHandler> logger) : base(logger)
    {
        _analyticsService = analyticsService;
    }

    protected override async Task HandleAsync(UpdateUserTrustScoreCommand command, ConsumeContext<UpdateUserTrustScoreCommand> context)
    {
        try
        {
            // Convert string reason to enum, or use action as fallback
            var reasonText = command.Reason ?? command.Action;
            if (!Enum.TryParse<TrustScoreChangeReason>(reasonText, true, out var reason))
            {
                Logger.LogWarning("Unknown trust score change reason: {Reason}", reasonText);
                reason = TrustScoreChangeReason.AdminAdjustment; // Default fallback
            }

            // Update trust score using the analytics service
            await _analyticsService.UpdateUserTrustScoreAsync(
                userId: command.TargetUserId,
                scoreChange: command.ScoreChange,
                reason: reason,
                details: command.Reason, // Use reason as details
                relatedEntityType: null, // Not available in current command
                relatedEntityId: null, // Not available in current command
                triggeredByUserId: null, // Not available in current command
                calculatedBy: "system", // Default to system
                isAutomatic: true, // Default to automatic
                confidence: null); // Not available in current command

            Logger.LogInformation("Updated trust score for user {UserId}: change {Change}, reason: {Reason}",
                command.TargetUserId, command.ScoreChange, command.Reason);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update trust score for user {UserId}: {Error}",
                command.TargetUserId, ex.Message);
            // Don't throw - trust score updates shouldn't break the main flow
        }
    }
}
