using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class EssentialUserSeedService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<EssentialUserSeedService> _logger;

    public EssentialUserSeedService(YapplrDbContext context, ILogger<EssentialUserSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedEssentialUsersAsync()
    {
        try
        {
            // Create system user if it doesn't exist
            await CreateSystemUserIfNotExistsAsync();
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during essential user seeding");
            throw;
        }
    }

    private async Task CreateSystemUserIfNotExistsAsync()
    {
        // Check if system user already exists
        var systemUserExists = await _context.Users.AnyAsync(u => u.Role == UserRole.System);
        if (systemUserExists)
        {
            _logger.LogInformation("✅ System user already exists");
            return;
        }

        _logger.LogInformation("🤖 Creating system user...");

        var systemUser = new User
        {
            Username = "system",
            Email = "system@yapplr.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random unguessable password
            Bio = "Yapplr System",
            Pronouns = "it/its",
            Tagline = "Official Yapplr system account for notifications",
            Role = UserRole.System,
            EmailVerified = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        _context.Users.Add(systemUser);
        await _context.SaveChangesAsync(); // Save user first to get the ID
        _logger.LogInformation("✅ System user created: {Username} ({Email})", systemUser.Username, systemUser.Email);

        // Create notification preferences for system user with email notifications disabled
        var systemPreferences = new NotificationPreferences
        {
            UserId = systemUser.Id, // Now this has a valid ID
            // Disable all notifications for system user
            EnableEmailNotifications = false,
            EnableEmailDigest = false,
            EnableInstantEmailNotifications = false,
            EnableMessageNotifications = false,
            EnableMentionNotifications = false,
            EnableReplyNotifications = false,
            EnableCommentNotifications = false,
            EnableFollowNotifications = false,
            EnableLikeNotifications = false,
            EnableRepostNotifications = false,
            EnableFollowRequestNotifications = false,
            PreferredMethod = NotificationDeliveryMethod.Disabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationPreferences.Add(systemPreferences);
        await _context.SaveChangesAsync(); // Save preferences
        _logger.LogInformation("✅ System user notification preferences created (all notifications disabled)");
    }
}
