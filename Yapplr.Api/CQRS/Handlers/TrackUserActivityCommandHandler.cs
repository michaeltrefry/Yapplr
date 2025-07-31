using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Common;
using Yapplr.Api.Services.Analytics;

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