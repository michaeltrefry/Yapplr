namespace Yapplr.Api.Configuration;

/// <summary>
/// Configuration for CORS policies
/// </summary>
public class CorsConfiguration
{
    public const string SectionName = "Cors";

    public CorsPolicyConfiguration AllowFrontend { get; set; } = new();
    public CorsPolicyConfiguration AllowSignalR { get; set; } = new();
    public CorsPolicyConfiguration AllowAll { get; set; } = new();
}