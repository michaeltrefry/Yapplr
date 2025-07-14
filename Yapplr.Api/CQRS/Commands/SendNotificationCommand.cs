namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a push notification
/// </summary>
public record SendNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public Dictionary<string, string>? Data { get; init; }
    public string NotificationType { get; init; } = "generic";
    public int Priority { get; init; } = 5; // 1 = highest, 10 = lowest
}