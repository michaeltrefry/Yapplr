namespace Yapplr.Api.Exceptions;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() 
        : base("Invalid credentials")
    {
    }

    public InvalidCredentialsException(string message) 
        : base(message)
    {
    }
}
