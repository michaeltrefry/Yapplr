using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class SystemTagSeedService
{
    private readonly YapplrDbContext _context;

    public SystemTagSeedService(YapplrDbContext context)
    {
        _context = context;
    }

    public async Task SeedDefaultSystemTagsAsync()
    {
        // Check if system tags already exist
        if (await _context.SystemTags.AnyAsync())
        {
            return; // Already seeded
        }

        var defaultTags = new List<SystemTag>
        {
            // Content Warning tags (visible to users)
            new SystemTag
            {
                Name = "NSFW",
                Description = "Not Safe For Work - Adult content",
                Category = SystemTagCategory.ContentWarning,
                IsVisibleToUsers = true,
                Color = "#EF4444",
                Icon = "warning",
                SortOrder = 1
            },
            new SystemTag
            {
                Name = "Violence",
                Description = "Contains violent content",
                Category = SystemTagCategory.ContentWarning,
                IsVisibleToUsers = true,
                Color = "#DC2626",
                Icon = "alert-triangle",
                SortOrder = 2
            },
            new SystemTag
            {
                Name = "Sensitive",
                Description = "Sensitive or triggering content",
                Category = SystemTagCategory.ContentWarning,
                IsVisibleToUsers = true,
                Color = "#F59E0B",
                Icon = "eye-off",
                SortOrder = 3
            },
            new SystemTag
            {
                Name = "Spoiler",
                Description = "Contains spoilers",
                Category = SystemTagCategory.ContentWarning,
                IsVisibleToUsers = true,
                Color = "#8B5CF6",
                Icon = "eye-off",
                SortOrder = 4
            },

            // Violation tags (hidden from users)
            new SystemTag
            {
                Name = "Harassment",
                Description = "Content violates harassment policy",
                Category = SystemTagCategory.Violation,
                IsVisibleToUsers = false,
                Color = "#DC2626",
                Icon = "user-x",
                SortOrder = 10
            },
            new SystemTag
            {
                Name = "Hate Speech",
                Description = "Contains hate speech",
                Category = SystemTagCategory.Violation,
                IsVisibleToUsers = false,
                Color = "#B91C1C",
                Icon = "message-square-x",
                SortOrder = 11
            },
            new SystemTag
            {
                Name = "Misinformation",
                Description = "Contains false or misleading information",
                Category = SystemTagCategory.Violation,
                IsVisibleToUsers = false,
                Color = "#F59E0B",
                Icon = "alert-circle",
                SortOrder = 12
            },
            new SystemTag
            {
                Name = "Copyright Violation",
                Description = "Violates copyright",
                Category = SystemTagCategory.Legal,
                IsVisibleToUsers = false,
                Color = "#7C3AED",
                Icon = "copyright",
                SortOrder = 13
            },

            // Moderation Status tags (hidden from users)
            new SystemTag
            {
                Name = "Under Review",
                Description = "Content is under moderation review",
                Category = SystemTagCategory.ModerationStatus,
                IsVisibleToUsers = false,
                Color = "#F59E0B",
                Icon = "clock",
                SortOrder = 20
            },
            new SystemTag
            {
                Name = "Approved",
                Description = "Content has been reviewed and approved",
                Category = SystemTagCategory.ModerationStatus,
                IsVisibleToUsers = false,
                Color = "#10B981",
                Icon = "check-circle",
                SortOrder = 21
            },
            new SystemTag
            {
                Name = "Flagged",
                Description = "Content has been flagged for review",
                Category = SystemTagCategory.ModerationStatus,
                IsVisibleToUsers = false,
                Color = "#EF4444",
                Icon = "flag",
                SortOrder = 22
            },

            // Quality tags (hidden from users)
            new SystemTag
            {
                Name = "Spam",
                Description = "Identified as spam content",
                Category = SystemTagCategory.Quality,
                IsVisibleToUsers = false,
                Color = "#6B7280",
                Icon = "trash-2",
                SortOrder = 30
            },
            new SystemTag
            {
                Name = "Low Quality",
                Description = "Low quality content",
                Category = SystemTagCategory.Quality,
                IsVisibleToUsers = false,
                Color = "#9CA3AF",
                Icon = "thumbs-down",
                SortOrder = 31
            },

            // Safety tags (hidden from users)
            new SystemTag
            {
                Name = "Self Harm",
                Description = "Content related to self-harm",
                Category = SystemTagCategory.Safety,
                IsVisibleToUsers = false,
                Color = "#DC2626",
                Icon = "heart",
                SortOrder = 40
            },
            new SystemTag
            {
                Name = "Doxxing",
                Description = "Contains personal information",
                Category = SystemTagCategory.Safety,
                IsVisibleToUsers = false,
                Color = "#B91C1C",
                Icon = "user-check",
                SortOrder = 41
            }
        };

        _context.SystemTags.AddRange(defaultTags);
        await _context.SaveChangesAsync();
    }
}
