namespace Yapplr.Api.Services;

public interface IEmailServiceFactory
{
    IEmailService CreateEmailService();
}

public class EmailServiceFactory : IEmailServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailSenderFactory _emailSenderFactory;

    public EmailServiceFactory(IServiceProvider serviceProvider, IEmailSenderFactory emailSenderFactory)
    {
        _serviceProvider = serviceProvider;
        _emailSenderFactory = emailSenderFactory;
    }

    public IEmailService CreateEmailService()
    {
        var emailSender = _emailSenderFactory.CreateEmailSender();
        var logger = _serviceProvider.GetRequiredService<ILogger<UnifiedEmailService>>();

        return new UnifiedEmailService(emailSender, logger);
    }
}
