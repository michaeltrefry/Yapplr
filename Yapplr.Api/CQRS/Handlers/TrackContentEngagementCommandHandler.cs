using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Analytics;

namespace Yapplr.Api.CQRS.Handlers;

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