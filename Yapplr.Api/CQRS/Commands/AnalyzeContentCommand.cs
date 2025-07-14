namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to analyze content for moderation
/// </summary>
public record AnalyzeContentCommand : BaseCommand
{
    public required string ContentType { get; init; } // "post", "comment", "message"
    public required int ContentId { get; init; }
    public required string Content { get; init; }
    public required int AuthorId { get; init; }
    public bool IsEdit { get; init; } = false;
}