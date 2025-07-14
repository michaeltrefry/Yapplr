namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a message notification
/// </summary>
public record SendMessageNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string SenderUsername { get; init; }
    public required string MessageContent { get; init; }
    public required int ConversationId { get; init; }
}