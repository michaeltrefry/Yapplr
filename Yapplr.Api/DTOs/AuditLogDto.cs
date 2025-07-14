using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class AuditLogDto
{
    public int Id { get; set; }
    public AuditAction Action { get; set; }
    public int UserId { get; set; }
    public string PerformedByUsername { get; set; } = string.Empty;
    public string? TargetUsername { get; set; }
    public int? TargetUserId { get; set; }
    public int? TargetPostId { get; set; }
    public int? TargetCommentId { get; set; }
    public string? Reason { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
