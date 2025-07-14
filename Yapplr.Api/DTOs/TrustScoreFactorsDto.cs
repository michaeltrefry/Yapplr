namespace Yapplr.Api.DTOs;

public class TrustScoreFactorsDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public float CurrentScore { get; set; }
    public Dictionary<string, object> Factors { get; set; } = new();
}
