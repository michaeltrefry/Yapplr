namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to send an email
/// </summary>
public record SendEmailCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? TextBody { get; init; }
    public int Priority { get; init; } = 5; // 1 = highest, 10 = lowest
    public int MaxRetries { get; init; } = 3;
}

/// <summary>
/// Command to send a welcome email to a new user
/// </summary>
public record SendWelcomeEmailCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Username { get; init; }
    public required string VerificationToken { get; init; }
}

/// <summary>
/// Command to send an email verification email
/// </summary>
public record SendEmailVerificationCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Username { get; init; }
    public required string VerificationToken { get; init; }
}

/// <summary>
/// Command to send a password reset email
/// </summary>
public record SendPasswordResetEmailCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Username { get; init; }
    public required string ResetToken { get; init; }
}

/// <summary>
/// Command to send a notification email (for important system notifications)
/// </summary>
public record SendNotificationEmailCommand : BaseCommand
{
    public required string ToEmail { get; init; }
    public required string Username { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public string NotificationType { get; init; } = "system";
}
