namespace Yapplr.Api.DTOs;

public class UpdateTrustScoreDto
{
    public float ScoreChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
}
