using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;
using Yapplr.Api.Data;
using Microsoft.EntityFrameworkCore;

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

/// <summary>
/// Handler for applying moderation actions
/// </summary>
public class ApplyModerationActionCommandHandler : BaseCommandHandler<ApplyModerationActionCommand>
{
    private readonly YapplrDbContext _dbContext;
    private readonly ICommandPublisher _commandPublisher;

    public ApplyModerationActionCommandHandler(
        YapplrDbContext dbContext,
        ICommandPublisher commandPublisher,
        ILogger<ApplyModerationActionCommandHandler> logger) : base(logger)
    {
        _dbContext = dbContext;
        _commandPublisher = commandPublisher;
    }

    protected override async Task HandleAsync(ApplyModerationActionCommand command, ConsumeContext<ApplyModerationActionCommand> context)
    {
        try
        {
            // Apply the moderation action based on content type
            switch (command.ContentType.ToLower())
            {
                case "post":
                    await ApplyPostModerationAsync(command);
                    break;
                case "comment":
                    await ApplyCommentModerationAsync(command);
                    break;
                default:
                    Logger.LogWarning("Unknown content type for moderation: {ContentType}", command.ContentType);
                    return;
            }

            // Send notification to content author if action was taken
            if (command.Action != "approve")
            {
                var notificationCommand = new SendContentModerationNotificationCommand
                {
                    UserId = command.UserId,
                    TargetUserId = await GetContentAuthorIdAsync(command.ContentType, command.ContentId),
                    ContentType = command.ContentType,
                    ContentId = command.ContentId,
                    Action = command.Action,
                    Reason = command.Reason,
                    AllowAppeal = command.Action == "hidden"
                };

                await _commandPublisher.PublishAsync(notificationCommand);
            }

            // Update user trust score if this was a violation
            if (command.Action == "hidden" || command.Action == "delete")
            {
                var trustScoreCommand = new UpdateUserTrustScoreCommand
                {
                    UserId = command.UserId,
                    TargetUserId = await GetContentAuthorIdAsync(command.ContentType, command.ContentId),
                    Action = "violation",
                    ScoreChange = -0.1f,
                    Reason = $"Content {command.Action} for: {command.Reason}"
                };

                await _commandPublisher.PublishAsync(trustScoreCommand);
            }

            Logger.LogInformation("Applied moderation action {Action} to {ContentType} {ContentId}",
                command.Action, command.ContentType, command.ContentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to apply moderation action {Action} to {ContentType} {ContentId}: {Error}",
                command.Action, command.ContentType, command.ContentId, ex.Message);
            throw;
        }
    }

    private async Task ApplyPostModerationAsync(ApplyModerationActionCommand command)
    {
        var post = await _dbContext.Posts.FindAsync(command.ContentId);
        if (post == null) return;

        switch (command.Action)
        {
            case "hide":
                post.IsHidden = true;
                post.HiddenReason = command.Reason;
                post.HiddenAt = DateTime.UtcNow;
                break;
            case "flag":
                post.IsFlagged = true;
                post.FlaggedReason = command.Reason;
                post.FlaggedAt = DateTime.UtcNow;
                break;
            case "delete":
                _dbContext.Posts.Remove(post);
                break;
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task ApplyCommentModerationAsync(ApplyModerationActionCommand command)
    {
        var comment = await _dbContext.Comments.FindAsync(command.ContentId);
        if (comment == null) return;

        switch (command.Action)
        {
            case "hide":
                comment.IsHidden = true;
                comment.HiddenReason = command.Reason;
                comment.HiddenAt = DateTime.UtcNow;
                break;
            case "flag":
                comment.IsFlagged = true;
                comment.FlaggedReason = command.Reason;
                comment.FlaggedAt = DateTime.UtcNow;
                break;
            case "delete":
                _dbContext.Comments.Remove(comment);
                break;
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task<int> GetContentAuthorIdAsync(string contentType, int contentId)
    {
        return contentType.ToLower() switch
        {
            "post" => await _dbContext.Posts
                .Where(p => p.Id == contentId)
                .Select(p => p.UserId)
                .FirstOrDefaultAsync(),
            "comment" => await _dbContext.Comments
                .Where(c => c.Id == contentId)
                .Select(c => c.UserId)
                .FirstOrDefaultAsync(),
            _ => 0
        };
    }
}
