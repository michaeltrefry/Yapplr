using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class CreateAppealDto
{
    public AppealType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
    public int? TargetPostId { get; set; }
    public int? TargetCommentId { get; set; }
}
