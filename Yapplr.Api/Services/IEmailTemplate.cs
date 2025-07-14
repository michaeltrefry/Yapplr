namespace Yapplr.Api.Services;

public interface IEmailTemplate
{
    string Subject { get; }
    string GenerateHtmlBody();
    string GenerateTextBody();
}