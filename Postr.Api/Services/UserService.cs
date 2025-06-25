using Microsoft.EntityFrameworkCore;
using Postr.Api.Data;
using Postr.Api.DTOs;

namespace Postr.Api.Services;

public class UserService : IUserService
{
    private readonly PostrDbContext _context;

    public UserService(PostrDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return null;

        return new UserDto(user.Id, user.Email, user.Username, user.Bio,
                          user.Birthday, user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt);
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string username)
    {
        var user = await _context.Users
            .Include(u => u.Posts)
            .FirstOrDefaultAsync(u => u.Username == username);
        
        if (user == null)
            return null;

        return new UserProfileDto(user.Id, user.Username, user.Bio, user.Birthday,
                                 user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt, user.Posts.Count);
    }

    public async Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto updateDto)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return null;

        // Update only provided fields
        if (updateDto.Bio != null)
            user.Bio = updateDto.Bio;
        
        if (updateDto.Birthday.HasValue)
            user.Birthday = DateTime.SpecifyKind(updateDto.Birthday.Value, DateTimeKind.Utc);
        
        if (updateDto.Pronouns != null)
            user.Pronouns = updateDto.Pronouns;
        
        if (updateDto.Tagline != null)
            user.Tagline = updateDto.Tagline;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new UserDto(user.Id, user.Email, user.Username, user.Bio,
                          user.Birthday, user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt);
    }

    public async Task<IEnumerable<UserDto>> SearchUsersAsync(string query)
    {
        var users = await _context.Users
            .Where(u => u.Username.Contains(query) || u.Bio.Contains(query))
            .Take(20) // Limit results
            .ToListAsync();

        return users.Select(u => new UserDto(u.Id, u.Email, u.Username, u.Bio,
                                           u.Birthday, u.Pronouns, u.Tagline, u.ProfileImageFileName, u.CreatedAt));
    }

    public async Task<UserDto?> UpdateProfileImageAsync(int userId, string fileName)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        // Delete old profile image if it exists
        if (!string.IsNullOrEmpty(user.ProfileImageFileName))
        {
            // Note: We could delete the old image file here, but we'll keep it simple for now
            // In production, you might want to clean up old files
        }

        user.ProfileImageFileName = fileName;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new UserDto(user.Id, user.Email, user.Username, user.Bio,
                          user.Birthday, user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt);
    }

    public async Task<UserDto?> RemoveProfileImageAsync(int userId, IImageService imageService)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        // Delete the image file if it exists
        if (!string.IsNullOrEmpty(user.ProfileImageFileName))
        {
            imageService.DeleteImage(user.ProfileImageFileName);
        }

        user.ProfileImageFileName = string.Empty;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new UserDto(user.Id, user.Email, user.Username, user.Bio,
                          user.Birthday, user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt);
    }
}
