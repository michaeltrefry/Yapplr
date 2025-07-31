using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Models.Analytics;
using Yapplr.Api.Services;
using Yapplr.Api.Services.Analytics;

namespace Yapplr.Api.CQRS.Handlers;

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