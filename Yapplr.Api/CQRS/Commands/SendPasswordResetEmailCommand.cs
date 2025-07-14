namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send a password reset email
/// </summary>
public record SendPasswordResetEmailCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Username { get; init; }
    public required string ResetToken { get; init; }
}