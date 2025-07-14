using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class UserAppealDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public AppealType Type { get; set; }
    public AppealStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
    public int? TargetPostId { get; set; }
    public int? TargetCommentId { get; set; }
    public string? ReviewedByUsername { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
