using MassTransit;
using Microsoft.EntityFrameworkCore;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.CQRS.Handlers;

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
                post.HiddenReasonType = PostHiddenReasonType.ModeratorHidden;
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