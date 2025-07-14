namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a content moderation notification
/// </summary>
public record SendContentModerationNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string ContentType { get; init; } // "post", "comment"
    public required int ContentId { get; init; }
    public required string Action { get; init; } // "hidden", "flagged", "approved"
    public string? Reason { get; init; }
    public bool AllowAppeal { get; init; } = true;
}