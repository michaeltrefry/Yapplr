using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class ReviewAppealDto
{
    public AppealStatus Status { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
}
