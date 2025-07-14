namespace Yapplr.Api.Configuration;

/// <summary>
/// Configuration for a single CORS policy
/// </summary>
public class CorsPolicyConfiguration
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedMethods { get; set; } = Array.Empty<string>();
    public string[] AllowedHeaders { get; set; } = Array.Empty<string>();
    public bool AllowCredentials { get; set; } = false;
    public bool AllowAnyOrigin { get; set; } = false;
    public bool AllowAnyMethod { get; set; } = false;
    public bool AllowAnyHeader { get; set; } = false;
}