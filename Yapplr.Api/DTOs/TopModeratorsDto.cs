namespace Yapplr.Api.DTOs;

public class TopModeratorsDto
{
    public List<ModeratorStatsDto> Moderators { get; set; } = new();
    public int TotalModerators { get; set; }
    public int TotalActions { get; set; }
}
