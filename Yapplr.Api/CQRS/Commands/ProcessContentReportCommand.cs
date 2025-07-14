namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to process a content report
/// </summary>
public record ProcessContentReportCommand : BaseCommand
{
    public required int ReportId { get; init; }
    public required string ContentType { get; init; }
    public required int ContentId { get; init; }
    public required int ReporterUserId { get; init; }
    public required string ReportType { get; init; }
    public required string Reason { get; init; }
    public string? AdditionalContext { get; init; }
}