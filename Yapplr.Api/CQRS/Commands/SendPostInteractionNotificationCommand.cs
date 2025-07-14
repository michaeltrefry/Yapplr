namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a post interaction notification (like, comment, etc.)
/// </summary>
public record SendPostInteractionNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required int PostId { get; init; }
    public required string InteractionType { get; init; } // "like", "comment", "share"
    public required string ActorUsername { get; init; }
    public string? CommentText { get; init; }
}