namespace Yapplr.Api.DTOs;

public class ModerationStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public int BannedUsers { get; set; }
    public int ShadowBannedUsers { get; set; }
    public int TotalPosts { get; set; }
    public int HiddenPosts { get; set; }
    public int TotalComments { get; set; }
    public int HiddenComments { get; set; }
    public int PendingAppeals { get; set; }
    public int TodayActions { get; set; }
    public int WeekActions { get; set; }
    public int MonthActions { get; set; }
}
