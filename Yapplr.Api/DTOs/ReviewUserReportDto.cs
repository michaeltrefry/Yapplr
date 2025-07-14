using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class ReviewUserReportDto
{
    public UserReportStatus Status { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
}
