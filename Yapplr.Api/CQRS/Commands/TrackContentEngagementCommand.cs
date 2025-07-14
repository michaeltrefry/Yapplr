namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to track content engagement
/// </summary>
public record TrackContentEngagementCommand : BaseCommand
{
    public required string ContentType { get; init; } // "post", "comment"
    public required int ContentId { get; init; }
    public required int AuthorId { get; init; }
    public required string EngagementType { get; init; } // "view", "like", "comment", "share"
    public required DateTime Timestamp { get; init; }
    public int? EngagingUserId { get; init; } // null for anonymous views
}