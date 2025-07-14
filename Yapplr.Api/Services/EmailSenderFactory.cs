using Yapplr.Api.Services.EmailSenders;

namespace Yapplr.Api.Services;

public class EmailSenderFactory : IEmailSenderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public EmailSenderFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public IEmailSender CreateEmailSender()
    {
        var emailProvider = _configuration["EmailProvider"];
        
        return emailProvider?.ToLowerInvariant() switch
        {
            "awsses" => _serviceProvider.GetRequiredService<AwsSesEmailSender>(),
            "smtp" => _serviceProvider.GetRequiredService<SmtpEmailSender>(),
            "sendgrid" => _serviceProvider.GetRequiredService<SendGridEmailSender>(),
            _ => _serviceProvider.GetRequiredService<AwsSesEmailSender>() // Default to AWS SES
        };
    }
}
