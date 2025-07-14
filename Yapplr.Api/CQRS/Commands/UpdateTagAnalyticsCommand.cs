namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to update tag analytics
/// </summary>
public record UpdateTagAnalyticsCommand : BaseCommand
{
    public required int TagId { get; init; }
    public required string Action { get; init; } // "post_created", "post_liked", "post_shared"
    public required DateTime Timestamp { get; init; }
    public int? ActorUserId { get; init; }
}