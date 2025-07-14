namespace Yapplr.Api.DTOs;

public class CreateUserReportDto
{
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<int> SystemTagIds { get; set; } = new();
}
