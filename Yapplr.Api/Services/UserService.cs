using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public class UserService : IUserService
{
    private readonly YapplrDbContext _context;
    private readonly IBlockService _blockService;

    public UserService(YapplrDbContext context, IBlockService blockService)
    {
        _context = context;
        _blockService = blockService;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return null;

        return new UserDto(user.Id, user.Email, user.Username, user.Bio,
                          user.Birthday, user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt);
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string username, int? currentUserId = null)
    {
        var user = await _context.Users
            .Include(u => u.Posts)
            .Include(u => u.Followers)
            .Include(u => u.Following)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return null;

        var isFollowedByCurrentUser = false;
        if (currentUserId.HasValue)
        {
            isFollowedByCurrentUser = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == user.Id);
        }

        return new UserProfileDto(user.Id, user.Username, user.Bio, user.Birthday,
                                 user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt,
                                 user.Posts.Count, user.Followers.Count, user.Following.Count, isFollowedByCurrentUser);
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

    public async Task<IEnumerable<UserDto>> SearchUsersAsync(string query, int? currentUserId)
    {
        var users = await _context.Users
            .Where(u => u.Username.Contains(query) || u.Bio.Contains(query))
            .Take(20) // Limit results
            .ToListAsync();

        // Filter out blocked users if current user is provided
        if (currentUserId.HasValue)
        {
            var blockedUserIds = await GetBlockedUserIdsAsync(currentUserId.Value);
            users = users.Where(u => !blockedUserIds.Contains(u.Id)).ToList();
        }

        return users.Select(u => new UserDto(u.Id, u.Email, u.Username, u.Bio,
                                           u.Birthday, u.Pronouns, u.Tagline, u.ProfileImageFileName, u.CreatedAt));
    }

    private async Task<List<int>> GetBlockedUserIdsAsync(int userId)
    {
        // Get users that the current user has blocked
        var blockedByUser = await _context.Blocks
            .Where(b => b.BlockerId == userId)
            .Select(b => b.BlockedId)
            .ToListAsync();

        // Get users that have blocked the current user
        var blockedByOthers = await _context.Blocks
            .Where(b => b.BlockedId == userId)
            .Select(b => b.BlockerId)
            .ToListAsync();

        // Combine both lists to filter out all blocked relationships
        return blockedByUser.Concat(blockedByOthers).Distinct().ToList();
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

    public async Task<FollowResponseDto> FollowUserAsync(int followerId, int followingId)
    {
        // Check if already following
        var existingFollow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

        if (existingFollow != null)
        {
            // Already following, return current status
            var currentFollowerCount = await _context.Follows
                .CountAsync(f => f.FollowingId == followingId);
            return new FollowResponseDto(true, currentFollowerCount);
        }

        // Create new follow relationship
        var follow = new Models.Follow
        {
            FollowerId = followerId,
            FollowingId = followingId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Follows.Add(follow);
        await _context.SaveChangesAsync();

        var followerCount = await _context.Follows
            .CountAsync(f => f.FollowingId == followingId);

        return new FollowResponseDto(true, followerCount);
    }

    public async Task<FollowResponseDto> UnfollowUserAsync(int followerId, int followingId)
    {
        var follow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

        if (follow != null)
        {
            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();
        }

        var followerCount = await _context.Follows
            .CountAsync(f => f.FollowingId == followingId);

        return new FollowResponseDto(false, followerCount);
    }

    public async Task<IEnumerable<UserDto>> GetFollowingAsync(int userId)
    {
        var following = await _context.Follows
            .Include(f => f.Following)
            .Where(f => f.FollowerId == userId)
            .OrderBy(f => f.Following.Username)
            .Select(f => new UserDto(
                f.Following.Id,
                f.Following.Email,
                f.Following.Username,
                f.Following.Bio,
                f.Following.Birthday,
                f.Following.Pronouns,
                f.Following.Tagline,
                f.Following.ProfileImageFileName,
                f.Following.CreatedAt
            ))
            .ToListAsync();

        return following;
    }

    public async Task<IEnumerable<UserDto>> GetFollowersAsync(int userId)
    {
        var followers = await _context.Follows
            .Include(f => f.Follower)
            .Where(f => f.FollowingId == userId)
            .OrderBy(f => f.Follower.Username)
            .Select(f => new UserDto(
                f.Follower.Id,
                f.Follower.Email,
                f.Follower.Username,
                f.Follower.Bio,
                f.Follower.Birthday,
                f.Follower.Pronouns,
                f.Follower.Tagline,
                f.Follower.ProfileImageFileName,
                f.Follower.CreatedAt
            ))
            .ToListAsync();

        return followers;
    }

    public async Task<IEnumerable<UserWithOnlineStatusDto>> GetFollowingWithOnlineStatusAsync(int userId)
    {
        var onlineThreshold = DateTime.UtcNow.AddMinutes(-5); // Consider user online if seen within last 5 minutes

        var following = await _context.Follows
            .Include(f => f.Following)
            .Where(f => f.FollowerId == userId)
            .OrderBy(f => f.Following.Username)
            .Select(f => new UserWithOnlineStatusDto(
                f.Following.Id,
                f.Following.Email,
                f.Following.Username,
                f.Following.Bio,
                f.Following.Birthday,
                f.Following.Pronouns,
                f.Following.Tagline,
                f.Following.ProfileImageFileName,
                f.Following.CreatedAt,
                f.Following.LastSeenAt > onlineThreshold
            ))
            .ToListAsync();

        return following;
    }
}
