using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Data;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for content analysis commands
/// </summary>
public class AnalyzeContentCommandHandler : BaseCommandHandler<AnalyzeContentCommand>
{
    private readonly IContentModerationService _contentModerationService;
    private readonly ICommandPublisher _commandPublisher;
    private readonly YapplrDbContext _dbContext;

    public AnalyzeContentCommandHandler(
        IContentModerationService contentModerationService,
        ICommandPublisher commandPublisher,
        YapplrDbContext dbContext,
        ILogger<AnalyzeContentCommandHandler> logger) : base(logger)
    {
        _contentModerationService = contentModerationService;
        _commandPublisher = commandPublisher;
        _dbContext = dbContext;
    }

    protected override async Task HandleAsync(AnalyzeContentCommand command, ConsumeContext<AnalyzeContentCommand> context)
    {
        try
        {
            // Analyze content using AI moderation service
            var result = await _contentModerationService.AnalyzeContentAsync(command.Content);

            if (result.RequiresReview)
            {
                // Determine action based on risk assessment
                var action = result.RiskAssessment.Level.ToLower() switch
                {
                    "high" => "hide",
                    "medium" => "flag",
                    _ => "approve"
                };

                if (action != "approve")
                {
                    // Publish command to apply moderation action
                    var moderationCommand = new ApplyModerationActionCommand
                    {
                        UserId = command.UserId,
                        ContentType = command.ContentType,
                        ContentId = command.ContentId,
                        Action = action,
                        Reason = $"AI moderation: {result.RiskAssessment.Level} risk content",
                        Source = "ai",
                        ConfidenceScore = (float)result.RiskAssessment.Score,
                        Metadata = new Dictionary<string, object>
                        {
                            ["riskLevel"] = result.RiskAssessment.Level,
                            ["riskScore"] = result.RiskAssessment.Score,
                            ["suggestedTags"] = result.SuggestedTags,
                            ["isEdit"] = command.IsEdit
                        }
                    };

                    await _commandPublisher.PublishAsync(moderationCommand);
                }
            }

            // Track analytics
            var analyticsCommand = new TrackContentEngagementCommand
            {
                UserId = command.UserId,
                ContentType = command.ContentType,
                ContentId = command.ContentId,
                AuthorId = command.AuthorId,
                EngagementType = "moderation_analysis",
                Timestamp = DateTime.UtcNow
            };

            await _commandPublisher.PublishAsync(analyticsCommand);

            Logger.LogInformation("Content analysis completed for {ContentType} {ContentId}. Action required: {RequiresReview}",
                command.ContentType, command.ContentId, result.RequiresReview);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to analyze content {ContentType} {ContentId}: {Error}",
                command.ContentType, command.ContentId, ex.Message);
            throw;
        }
    }
}