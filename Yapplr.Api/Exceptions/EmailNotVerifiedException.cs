namespace Yapplr.Api.Exceptions;

public class EmailNotVerifiedException(string email, string message = "Email address must be verified before logging in.") : Exception(message)
{
    public string Email { get; } = email;
}
