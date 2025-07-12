using System.Net;
using System.Net.Mail;
using System.Text;

namespace Yapplr.Api.Services.EmailSenders;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly ISmartRetryService _retryService;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger, ISmartRetryService retryService)
    {
        _configuration = configuration;
        _logger = logger;
        _retryService = retryService;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        // Use smart retry for email sending
        return await _retryService.ExecuteWithRetryAsync(
            async () => {
                try
                {
                    var smtpSettings = _configuration.GetSection("SmtpSettings");
                    var host = smtpSettings["Host"];
                    var port = int.Parse(smtpSettings["Port"] ?? "587");
                    var username = smtpSettings["Username"];
                    var password = smtpSettings["Password"];
                    var fromEmail = smtpSettings["FromEmail"];
                    var fromName = smtpSettings["FromName"] ?? "Yapplr";

                    if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    {
                        _logger.LogWarning("SMTP settings not configured. Email not sent.");
                        return false;
                    }

                    using var client = new SmtpClient(host, port)
                    {
                        Credentials = new NetworkCredential(username, password),
                        EnableSsl = true
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail ?? username, fromName),
                        Subject = subject,
                        IsBodyHtml = true,
                        Body = htmlBody
                    };

                    mailMessage.To.Add(toEmail);

                    // Add text body as alternate view if provided
                    if (!string.IsNullOrEmpty(textBody))
                    {
                        var textView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain");
                        mailMessage.AlternateViews.Add(textView);
                    }

                    await client.SendMailAsync(mailMessage);
                    
                    _logger.LogInformation("Email sent successfully via SMTP to {ToEmail}", toEmail);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email via SMTP to {ToEmail}: {Message}", toEmail, ex.Message);
                    return false;
                }
            },
            $"SendEmail_{toEmail.GetHashCode()}");
    }
}
