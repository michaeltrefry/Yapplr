

namespace Yapplr.Api.Services.EmailSenders;

/// <summary>
/// Console email sender for development - logs emails to console instead of sending them
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        _logger.LogInformation("📧 [CONSOLE EMAIL] Would send email:");
        _logger.LogInformation("   To: {ToEmail}", toEmail);
        _logger.LogInformation("   Subject: {Subject}", subject);
        _logger.LogInformation("   HTML Body: {HtmlBody}", htmlBody);
        
        if (!string.IsNullOrEmpty(textBody))
        {
            _logger.LogInformation("   Text Body: {TextBody}", textBody);
        }
        
        _logger.LogInformation("📧 [CONSOLE EMAIL] Email logged successfully");
        
        // Always return true for console logging
        return Task.FromResult(true);
    }
}
