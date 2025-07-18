using Yapplr.Api.Services.EmailTemplates;

namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a notification digest email
/// </summary>
public record SendNotificationDigestEmailCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Username { get; init; }
    public required List<DigestNotification> Notifications { get; init; }
    public required DateTime PeriodStart { get; init; }
    public required DateTime PeriodEnd { get; init; }
    public string? UnsubscribeUrl { get; init; }
}
