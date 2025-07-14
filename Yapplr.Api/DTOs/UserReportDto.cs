using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class UserReportDto
{
    public int Id { get; set; }
    public string ReportedByUsername { get; set; } = string.Empty;
    public UserReportStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByUsername { get; set; }
    public string? ReviewNotes { get; set; }

    // Content being reported
    public AdminPostDto? Post { get; set; }
    public AdminCommentDto? Comment { get; set; }

    // Selected system tags
    public List<SystemTagDto> SystemTags { get; set; } = new();
}
