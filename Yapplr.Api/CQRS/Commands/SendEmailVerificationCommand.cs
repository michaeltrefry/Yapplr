namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send an email verification email
/// </summary>
public record SendEmailVerificationCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Username { get; init; }
    public required string VerificationToken { get; init; }
}