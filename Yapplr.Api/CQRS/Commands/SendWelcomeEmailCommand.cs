namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a welcome email to a new user
/// </summary>
public record SendWelcomeEmailCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Username { get; init; }
    public required string VerificationToken { get; init; }
}