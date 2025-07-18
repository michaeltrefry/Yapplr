namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a notification email (for important system notifications)
/// </summary>
public record SendNotificationEmailCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Username { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public string? NotificationType { get; init; } = "system";
    public string? ActionUrl { get; init; }
}