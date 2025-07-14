namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a follow notification
/// </summary>
public record SendFollowNotificationCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string FollowerUsername { get; init; }
}