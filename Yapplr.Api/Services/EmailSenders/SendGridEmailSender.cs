using SendGrid;
using SendGrid.Helpers.Mail;

namespace Yapplr.Api.Services.EmailSenders;

public class SendGridEmailSender : IEmailSender
{
    private readonly ISendGridClient _sendGridClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(ISendGridClient sendGridClient, IConfiguration configuration, ILogger<SendGridEmailSender> logger)
    {
        _sendGridClient = sendGridClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            var sendGridSettings = _configuration.GetSection("SendGridSettings");
            var fromEmail = sendGridSettings["FromEmail"];
            var fromName = sendGridSettings["FromName"] ?? "Yapplr";

            if (string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("SendGrid FromEmail is not configured");
                return false;
            }

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, textBody ?? htmlBody, htmlBody);

            var response = await _sendGridClient.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully via SendGrid to {ToEmail}", toEmail);
                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid failed to send email to {ToEmail}. Status: {StatusCode}, Response: {Response}", 
                    toEmail, response.StatusCode, responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid rejected the email to {ToEmail}: {Message}", toEmail, ex.Message);
            return false;
        }
    }
}
