namespace Yapplr.Api.Services;

/// <summary>
/// Content filter configuration
/// </summary>
public class ContentFilterConfig
{
    public bool EnableProfanityFilter { get; set; } = true;
    public bool EnableSpamDetection { get; set; } = true;
    public bool EnablePhishingDetection { get; set; } = true;
    public bool EnableMaliciousLinkDetection { get; set; } = true;
    public bool EnableContentSanitization { get; set; } = true;
    public int MaxContentLength { get; set; } = 1000;
    public int MaxUrlsPerMessage { get; set; } = 3;
    public bool BlockSuspiciousDomains { get; set; } = true;
}