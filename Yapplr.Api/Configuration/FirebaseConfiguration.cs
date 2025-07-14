namespace Yapplr.Api.Configuration;

/// <summary>
/// Firebase notification provider configuration
/// </summary>
public class FirebaseConfiguration
{
    public bool Enabled { get; set; } = true;
    public string ProjectId { get; set; } = string.Empty;
    public string ServiceAccountKeyFile { get; set; } = string.Empty;
}