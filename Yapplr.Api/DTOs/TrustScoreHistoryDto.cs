namespace Yapplr.Api.DTOs;

public class TrustScoreHistoryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public float PreviousScore { get; set; }
    public float NewScore { get; set; }
    public float ScoreChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? TriggeredByUsername { get; set; }
    public string? CalculatedBy { get; set; }
    public bool IsAutomatic { get; set; }
    public float? Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
}
