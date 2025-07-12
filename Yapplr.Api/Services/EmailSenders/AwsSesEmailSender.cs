using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace Yapplr.Api.Services.EmailSenders;

public class AwsSesEmailSender : IEmailSender
{
    private readonly IAmazonSimpleEmailService? _sesClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsSesEmailSender> _logger;

    public AwsSesEmailSender(IAmazonSimpleEmailService? sesClient, IConfiguration configuration, ILogger<AwsSesEmailSender> logger)
    {
        _sesClient = sesClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            // Check if SES client is available (might be null during migrations)
            if (_sesClient == null)
            {
                _logger.LogWarning("AWS SES client is not available - email sending skipped");
                return false;
            }

            var sesSettings = _configuration.GetSection("AwsSesSettings");
            var fromEmail = sesSettings["FromEmail"];
            var fromName = sesSettings["FromName"] ?? "Yapplr";

            if (string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("AWS SES FromEmail not configured");
                return false;
            }

            var source = string.IsNullOrEmpty(fromName) ? fromEmail : $"{fromName} <{fromEmail}>";

            var sendRequest = new SendEmailRequest
            {
                Source = source,
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body()
                }
            };

            // Set HTML body
            if (!string.IsNullOrEmpty(htmlBody))
            {
                sendRequest.Message.Body.Html = new Content
                {
                    Charset = "UTF-8",
                    Data = htmlBody
                };
            }

            // Set text body
            if (!string.IsNullOrEmpty(textBody))
            {
                sendRequest.Message.Body.Text = new Content
                {
                    Charset = "UTF-8",
                    Data = textBody
                };
            }

            var response = await _sesClient.SendEmailAsync(sendRequest);
            
            _logger.LogInformation("Email sent successfully via AWS SES. MessageId: {MessageId}, To: {Email}", 
                response.MessageId, toEmail);
            
            return true;
        }
        catch (MessageRejectedException ex)
        {
            _logger.LogError(ex, "AWS SES rejected the email to {Email}: {Reason}", toEmail, ex.Message);
            return false;
        }
        catch (MailFromDomainNotVerifiedException ex)
        {
            _logger.LogError(ex, "AWS SES domain not verified for {Email}", toEmail);
            return false;
        }
        catch (ConfigurationSetDoesNotExistException ex)
        {
            _logger.LogError(ex, "AWS SES configuration set does not exist: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via AWS SES to {Email}", toEmail);
            return false;
        }
    }
}
