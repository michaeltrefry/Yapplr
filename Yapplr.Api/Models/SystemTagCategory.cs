namespace Yapplr.Api.Models;

public enum SystemTagCategory
{
    ContentWarning = 0,     // Visible to users - content warnings like NSFW, violence, etc.
    Violation = 1,          // Hidden from users - policy violations
    ModerationStatus = 2,   // Hidden from users - under review, approved, etc.
    Quality = 3,            // Hidden from users - spam, low quality, etc.
    Legal = 4,              // Hidden from users - copyright, legal issues
    Safety = 5              // Hidden from users - safety concerns, harassment
}