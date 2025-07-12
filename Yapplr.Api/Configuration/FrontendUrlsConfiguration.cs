namespace Yapplr.Api.Configuration;

public class FrontendUrlsConfiguration
{
    public const string SectionName = "FrontendUrls";
    
    public string BaseUrl { get; set; } = "http://localhost:3000";
    public string VerifyEmailPath { get; set; } = "/verify-email";
    
    public string GetVerifyEmailUrl(string token)
    {
        return $"{BaseUrl.TrimEnd('/')}{VerifyEmailPath}?token={token}";
    }
}
