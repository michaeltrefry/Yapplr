namespace Yapplr.Api.DTOs;

public class AiSuggestedTagDto
{
    public int Id { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }
    public DateTime SuggestedAt { get; set; }
    public bool IsApproved { get; set; }
    public bool IsRejected { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUsername { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalReason { get; set; }
}
