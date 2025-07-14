namespace Yapplr.Api.Services;

public interface IEmailSenderFactory
{
    IEmailSender CreateEmailSender();
}