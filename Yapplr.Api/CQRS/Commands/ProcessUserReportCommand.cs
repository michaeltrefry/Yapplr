namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to process a user report
/// </summary>
public record ProcessUserReportCommand : BaseCommand
{
    public required int ReportId { get; init; }
    public required int ReportedUserId { get; init; }
    public required int ReporterUserId { get; init; }
    public required string ReportType { get; init; }
    public required string Reason { get; init; }
    public string? AdditionalContext { get; init; }
}