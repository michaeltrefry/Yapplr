namespace Yapplr.Api.Services;

/// <summary>
/// Content validation result
/// </summary>
public class ContentValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Violations { get; set; } = new();
    public string? SanitizedContent { get; set; }
    public ContentRiskLevel RiskLevel { get; set; } = ContentRiskLevel.Low;
}