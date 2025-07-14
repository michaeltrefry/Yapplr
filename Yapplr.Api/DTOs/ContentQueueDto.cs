namespace Yapplr.Api.DTOs;

public class ContentQueueDto
{
    public List<AdminPostDto> FlaggedPosts { get; set; } = new();
    public List<AdminCommentDto> FlaggedComments { get; set; } = new();
    public List<UserAppealDto> PendingAppeals { get; set; } = new();
    public List<UserReportDto> UserReports { get; set; } = new();
    public int TotalFlaggedContent { get; set; }
}
