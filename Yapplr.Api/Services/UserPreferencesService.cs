using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class UserPreferencesService : IUserPreferencesService
{
    private readonly YapplrDbContext _context;

    public UserPreferencesService(YapplrDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreferencesDto> GetUserPreferencesAsync(int userId)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create default preferences if they don't exist
            preferences = new UserPreferences
            {
                UserId = userId,
                DarkMode = false,
                RequireFollowApproval = false
            };
            
            _context.UserPreferences.Add(preferences);
            await _context.SaveChangesAsync();
        }

        return new UserPreferencesDto
        {
            DarkMode = preferences.DarkMode,
            RequireFollowApproval = preferences.RequireFollowApproval
        };
    }

    public async Task<UserPreferencesDto> UpdateUserPreferencesAsync(int userId, UpdateUserPreferencesDto updateDto)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create new preferences if they don't exist
            preferences = new UserPreferences
            {
                UserId = userId,
                DarkMode = updateDto.DarkMode ?? false,
                RequireFollowApproval = updateDto.RequireFollowApproval ?? false
            };
            
            _context.UserPreferences.Add(preferences);
        }
        else
        {
            // Update existing preferences
            if (updateDto.DarkMode.HasValue)
            {
                preferences.DarkMode = updateDto.DarkMode.Value;
            }

            if (updateDto.RequireFollowApproval.HasValue)
            {
                preferences.RequireFollowApproval = updateDto.RequireFollowApproval.Value;
            }

            preferences.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new UserPreferencesDto
        {
            DarkMode = preferences.DarkMode,
            RequireFollowApproval = preferences.RequireFollowApproval
        };
    }
}
