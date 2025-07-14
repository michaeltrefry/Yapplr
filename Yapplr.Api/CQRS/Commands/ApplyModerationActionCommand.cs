namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to apply automatic moderation action
/// </summary>
public record ApplyModerationActionCommand : BaseCommand
{
    public required string ContentType { get; init; }
    public required int ContentId { get; init; }
    public required string Action { get; init; } // "hide", "flag", "approve", "delete"
    public required string Reason { get; init; }
    public required string Source { get; init; } // "ai", "user_report", "admin"
    public float? ConfidenceScore { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}