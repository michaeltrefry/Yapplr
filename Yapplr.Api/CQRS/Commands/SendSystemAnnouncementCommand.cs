namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a system announcement notification
/// </summary>
public record SendSystemAnnouncementCommand : BaseCommand
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public List<int>? TargetUserIds { get; init; } // null = all users
    public string AnnouncementType { get; init; } = "general";
}