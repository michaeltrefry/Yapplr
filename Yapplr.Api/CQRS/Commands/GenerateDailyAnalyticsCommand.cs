namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to generate daily analytics report
/// </summary>
public record GenerateDailyAnalyticsCommand : BaseCommand
{
    public required DateTime Date { get; init; }
    public List<string>? ReportTypes { get; init; } // null = all types
}