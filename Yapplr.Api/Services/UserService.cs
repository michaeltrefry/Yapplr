using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;

namespace Yapplr.Api.Services;

public class UserService : IUserService
{
    private readonly YapplrDbContext _context;
    private readonly IBlockService _blockService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserService> _logger;

    public UserService(YapplrDbContext context, IBlockService blockService, INotificationService notificationService, ILogger<UserService> logger)
    {
        _context = context;
        _blockService = blockService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return null;

        return user.ToDto();
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
        var hasPendingFollowRequest = false;

        if (currentUserId.HasValue)
        {
            isFollowedByCurrentUser = await _context.Follows
                .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == user.Id);

            if (!isFollowedByCurrentUser)
            {
                hasPendingFollowRequest = await _context.FollowRequests
                    .AnyAsync(fr => fr.RequesterId == currentUserId.Value &&
                                   fr.RequestedId == user.Id &&
                                   fr.Status == FollowRequestStatus.Pending);
            }
        }

        // Get user preferences to check if follow approval is required
        var userPreferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id);
        var requiresFollowApproval = userPreferences?.RequireFollowApproval ?? false;

        return new UserProfileDto(user.Id, user.Username, user.Bio, user.Birthday,
                                 user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt,
                                 user.Posts.Count, user.Followers.Count, user.Following.Count,
                                 isFollowedByCurrentUser, hasPendingFollowRequest, requiresFollowApproval);
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

        return user.ToDto();
    }

    public async Task<IEnumerable<UserDto>> SearchUsersAsync(string query)
    {
        var users = await _context.Users
            .Where(u => u.Username.Contains(query) || u.Bio.Contains(query))
            .Take(20) // Limit results
            .ToListAsync();

        return users.Select(u => u.ToDto());
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

        return users.Select(u => u.ToDto());
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

        return user.ToDto();
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

        return user.ToDto();
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

        // Check if there's already a pending follow request
        var existingPendingRequest = await _context.FollowRequests
            .FirstOrDefaultAsync(fr => fr.RequesterId == followerId &&
                                      fr.RequestedId == followingId &&
                                      fr.Status == FollowRequestStatus.Pending);

        if (existingPendingRequest != null)
        {
            // Already has pending request, return current status
            var currentFollowerCount = await _context.Follows
                .CountAsync(f => f.FollowingId == followingId);
            return new FollowResponseDto(false, currentFollowerCount, true);
        }

        // Check if the user being followed requires approval
        var targetUserPreferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == followingId);

        var requiresApproval = targetUserPreferences?.RequireFollowApproval ?? false;

        if (requiresApproval)
        {
            // Create follow request instead of direct follow
            var followRequest = new FollowRequest
            {
                RequesterId = followerId,
                RequestedId = followingId,
                CreatedAt = DateTime.UtcNow
            };

            _context.FollowRequests.Add(followRequest);
            await _context.SaveChangesAsync();

            // Create follow request notification
            await _notificationService.CreateFollowRequestNotificationAsync(followingId, followerId);

            var followerCount = await _context.Follows
                .CountAsync(f => f.FollowingId == followingId);

            return new FollowResponseDto(false, followerCount, true);
        }
        else
        {
            // Create direct follow relationship
            var follow = new Models.Follow
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();

            // Create follow notification
            await _notificationService.CreateFollowNotificationAsync(followingId, followerId);

            var followerCount = await _context.Follows
                .CountAsync(f => f.FollowingId == followingId);

            return new FollowResponseDto(true, followerCount);
        }
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
            .Select(f => f.Following.ToDto())
            .ToListAsync();

        return following;
    }

    public async Task<IEnumerable<UserDto>> GetFollowersAsync(int userId)
    {
        var followers = await _context.Follows
            .Include(f => f.Follower)
            .Where(f => f.FollowingId == userId)
            .OrderBy(f => f.Follower.Username)
            .Select(f => f.Follower.ToDto())
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

    public async Task<bool> UpdateFcmTokenAsync(int userId, string? fcmToken)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        if (fcmToken == null)
        {
            _logger.LogInformation("Clearing FCM token for user {UserId}", userId);
        }
        else
        {
            _logger.LogInformation("Updating FCM token for user {UserId}: {Token}", userId, fcmToken.Substring(0, Math.Min(20, fcmToken.Length)) + "...");
        }

        user.FcmToken = fcmToken;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully updated FCM token for user {UserId}", userId);
        return true;
    }

    public async Task<IEnumerable<FollowRequestDto>> GetPendingFollowRequestsAsync(int userId)
    {
        var requests = await _context.FollowRequests
            .Include(fr => fr.Requester)
            .Include(fr => fr.Requested)
            .Where(fr => fr.RequestedId == userId && fr.Status == FollowRequestStatus.Pending)
            .OrderByDescending(fr => fr.CreatedAt)
            .Select(fr => new FollowRequestDto
            {
                Id = fr.Id,
                CreatedAt = fr.CreatedAt,
                Requester = fr.Requester.ToDto(),
                Requested = fr.Requested.ToDto()
            })
            .ToListAsync();

        return requests;
    }

    public async Task<FollowResponseDto> ApproveFollowRequestAsync(int requestId, int userId)
    {
        var request = await _context.FollowRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId && fr.RequestedId == userId);

        if (request == null)
        {
            throw new ArgumentException("Follow request not found or not authorized");
        }

        // Create the follow relationship
        var follow = new Models.Follow
        {
            FollowerId = request.RequesterId,
            FollowingId = request.RequestedId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Follows.Add(follow);

        // Update the follow request status instead of deleting
        request.Status = FollowRequestStatus.Approved;
        request.ProcessedAt = DateTime.UtcNow;

        // Mark the follow request notification as read
        var followRequestNotification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Type == NotificationType.FollowRequest &&
                                     n.ActorUserId == request.RequesterId &&
                                     n.UserId == request.RequestedId);
        if (followRequestNotification != null)
        {
            followRequestNotification.IsRead = true;
            followRequestNotification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Create follow notification
        await _notificationService.CreateFollowNotificationAsync(request.RequestedId, request.RequesterId);

        var followerCount = await _context.Follows
            .CountAsync(f => f.FollowingId == request.RequestedId);

        return new FollowResponseDto(true, followerCount);
    }

    public async Task<FollowResponseDto> DenyFollowRequestAsync(int requestId, int userId)
    {
        var request = await _context.FollowRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId && fr.RequestedId == userId);

        if (request == null)
        {
            throw new ArgumentException("Follow request not found or not authorized");
        }

        // Update the follow request status instead of deleting
        request.Status = FollowRequestStatus.Denied;
        request.ProcessedAt = DateTime.UtcNow;

        // Mark the follow request notification as read
        var followRequestNotification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Type == NotificationType.FollowRequest &&
                                     n.ActorUserId == request.RequesterId &&
                                     n.UserId == request.RequestedId);
        if (followRequestNotification != null)
        {
            followRequestNotification.IsRead = true;
            followRequestNotification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var followerCount = await _context.Follows
            .CountAsync(f => f.FollowingId == request.RequestedId);

        return new FollowResponseDto(false, followerCount);
    }

    public async Task<FollowResponseDto> ApproveFollowRequestByUserIdAsync(int requesterId, int userId)
    {
        var request = await _context.FollowRequests
            .FirstOrDefaultAsync(fr => fr.RequesterId == requesterId && fr.RequestedId == userId);

        if (request == null)
        {
            throw new ArgumentException("Follow request not found");
        }

        return await ApproveFollowRequestAsync(request.Id, userId);
    }

    public async Task<FollowResponseDto> DenyFollowRequestByUserIdAsync(int requesterId, int userId)
    {
        var request = await _context.FollowRequests
            .FirstOrDefaultAsync(fr => fr.RequesterId == requesterId && fr.RequestedId == userId);

        if (request == null)
        {
            throw new ArgumentException("Follow request not found");
        }

        return await DenyFollowRequestAsync(request.Id, userId);
    }
}
