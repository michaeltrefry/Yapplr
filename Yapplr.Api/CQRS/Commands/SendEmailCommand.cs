namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send an email
/// </summary>
public record SendEmailCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? TextBody { get; init; }
    public int Priority { get; init; } = 5; // 1 = highest, 10 = lowest
    public int MaxRetries { get; init; } = 3;
}