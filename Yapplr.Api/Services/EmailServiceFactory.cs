namespace Yapplr.Api.Services;

public interface IEmailServiceFactory
{
    IEmailService CreateEmailService();
}

public class EmailServiceFactory : IEmailServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public EmailServiceFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public IEmailService CreateEmailService()
    {
        var emailProvider = _configuration["EmailProvider"];
        
        return emailProvider?.ToLowerInvariant() switch
        {
            "awsses" => _serviceProvider.GetRequiredService<AwsSesEmailService>(),
            "smtp" => _serviceProvider.GetRequiredService<EmailService>(),
            _ => _serviceProvider.GetRequiredService<AwsSesEmailService>() // Default to AWS SES
        };
    }
}
