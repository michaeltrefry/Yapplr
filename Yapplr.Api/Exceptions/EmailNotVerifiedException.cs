namespace Yapplr.Api.Exceptions;

public class EmailNotVerifiedException : Exception
{
    public string Email { get; }

    public EmailNotVerifiedException(string email) 
        : base("Email address must be verified before logging in.")
    {
        Email = email;
    }

    public EmailNotVerifiedException(string email, string message) 
        : base(message)
    {
        Email = email;
    }
}
