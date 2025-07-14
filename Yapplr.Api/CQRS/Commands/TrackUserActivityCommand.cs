namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to track user activity
/// </summary>
public record TrackUserActivityCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string ActivityType { get; init; } // "login", "post_created", "comment_created", "like_given"
    public required DateTime Timestamp { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}