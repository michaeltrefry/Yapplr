using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;
using Yapplr.Api.Common;

namespace Yapplr.Api.Services;

public class UserService : BaseService, IUserService
{
    private readonly IBlockService _blockService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public UserService(YapplrDbContext context, IBlockService blockService, INotificationService notificationService, IEmailService emailService, IConfiguration configuration, ILogger<UserService> logger) : base(context, logger)
    {
        _blockService = blockService;
        _notificationService = notificationService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public new async Task<UserDto?> GetUserByIdAsync(int userId)
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
            .Where(u => EF.Functions.ILike(u.Username, $"%{query}%") || EF.Functions.ILike(u.Bio, $"%{query}%"))
            .Take(20) // Limit results
            .ToListAsync();

        return users.Select(u => u.ToDto());
    }

    public async Task<IEnumerable<UserDto>> SearchUsersAsync(string query, int? currentUserId)
    {
        var users = await _context.Users
            .Where(u => EF.Functions.ILike(u.Username, $"%{query}%") || EF.Functions.ILike(u.Bio, $"%{query}%"))
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

    private new async Task<List<int>> GetBlockedUserIdsAsync(int userId)
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

    public async Task<bool> UpdateExpoPushTokenAsync(int userId, string? expoPushToken)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        if (expoPushToken == null)
        {
            _logger.LogInformation("Clearing Expo push token for user {UserId}", userId);
        }
        else
        {
            _logger.LogInformation("Updating Expo push token for user {UserId}: {Token}", userId, expoPushToken.Substring(0, Math.Min(20, expoPushToken.Length)) + "...");
        }

        user.ExpoPushToken = expoPushToken;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully updated Expo push token for user {UserId}", userId);
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

    // Admin methods
    public async Task<User?> GetUserEntityByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.SuspendedByUser)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> SuspendUserAsync(int userId, int suspendedByUserId, string reason, DateTime? suspendedUntil = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var moderator = await _context.Users.FindAsync(suspendedByUserId);
        if (moderator == null) return false;

        user.Status = UserStatus.Suspended;
        user.SuspendedUntil = suspendedUntil;
        user.SuspensionReason = reason;
        user.SuspendedByUserId = suspendedByUserId;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Enhancement 1: Send in-app notification to the suspended user
        await _notificationService.CreateUserSuspensionNotificationAsync(userId, reason, suspendedUntil, moderator.Username);

        // Enhancement 3: Send email notification
        try
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "https://yapplr.com";
            var appealUrl = $"{frontendUrl}/appeals";

            await _emailService.SendUserSuspensionEmailAsync(
                user.Email,
                user.Username,
                reason,
                suspendedUntil,
                moderator.Username,
                appealUrl
            );

            _logger.LogInformation("Suspension email sent to user {UserId} ({Email})", userId, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send suspension email to user {UserId} ({Email})", userId, user.Email);
            // Don't fail the suspension if email fails
        }

        return true;
    }

    public async Task<bool> UnsuspendUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Status = UserStatus.Active;
        user.SuspendedUntil = null;
        user.SuspensionReason = null;
        user.SuspendedByUserId = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Send notification to the unsuspended user
        await _notificationService.CreateUserUnsuspensionNotificationAsync(userId, "System");

        return true;
    }

    public async Task<bool> BanUserAsync(int userId, int bannedByUserId, string reason, bool isShadowBan = false)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var moderator = await _context.Users.FindAsync(bannedByUserId);
        if (moderator == null) return false;

        user.Status = isShadowBan ? UserStatus.ShadowBanned : UserStatus.Banned;
        user.SuspensionReason = reason;
        user.SuspendedByUserId = bannedByUserId;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Send notification to the banned user
        await _notificationService.CreateUserBanNotificationAsync(userId, reason, isShadowBan, moderator.Username);

        return true;
    }

    public async Task<bool> UnbanUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Status = UserStatus.Active;
        user.SuspensionReason = null;
        user.SuspendedByUserId = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Send notification to the unbanned user
        await _notificationService.CreateUserUnbanNotificationAsync(userId, "System");

        return true;
    }

    public async Task<bool> ChangeUserRoleAsync(int userId, int changedByUserId, UserRole newRole, string reason)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ForcePasswordResetAsync(int userId, int requestedByUserId, string reason)
    {
        // This would typically involve creating a password reset token and sending an email
        // For now, we'll just log the action
        await Task.CompletedTask;
        return true;
    }

    public async Task<IEnumerable<AdminUserDto>> GetUsersForAdminAsync(int page = 1, int pageSize = 25, UserStatus? status = null, UserRole? role = null)
    {
        var query = _context.Users
            .Include(u => u.SuspendedByUser)
            .Include(u => u.Posts)
            .Include(u => u.Followers)
            .Include(u => u.Following)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role,
            Status = u.Status,
            SuspendedUntil = u.SuspendedUntil,
            SuspensionReason = u.SuspensionReason,
            SuspendedByUsername = u.SuspendedByUser?.Username,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            LastLoginIp = u.LastLoginIp,
            EmailVerified = u.EmailVerified,
            PostCount = u.Posts.Count,
            FollowerCount = u.Followers.Count,
            FollowingCount = u.Following.Count
        });
    }

    public async Task<AdminUserDto?> GetUserForAdminAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.SuspendedByUser)
            .Include(u => u.Posts)
            .Include(u => u.Followers)
            .Include(u => u.Following)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new AdminUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
            SuspendedUntil = user.SuspendedUntil,
            SuspensionReason = user.SuspensionReason,
            SuspendedByUsername = user.SuspendedByUser?.Username,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            LastLoginIp = user.LastLoginIp,
            EmailVerified = user.EmailVerified,
            PostCount = user.Posts.Count,
            FollowerCount = user.Followers.Count,
            FollowingCount = user.Following.Count
        };
    }
}
