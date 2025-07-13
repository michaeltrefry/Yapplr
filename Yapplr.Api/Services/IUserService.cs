using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<UserProfileDto?> GetUserProfileAsync(string username, int? currentUserId = null);
    Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto updateDto);
    Task<IEnumerable<UserDto>> SearchUsersAsync(string query);
    Task<IEnumerable<UserDto>> SearchUsersAsync(string query, int? currentUserId);
    Task<UserDto?> UpdateProfileImageAsync(int userId, string fileName);
    Task<UserDto?> RemoveProfileImageAsync(int userId, IImageService imageService);
    Task<FollowResponseDto> FollowUserAsync(int followerId, int followingId);
    Task<FollowResponseDto> UnfollowUserAsync(int followerId, int followingId);
    Task<IEnumerable<UserDto>> GetFollowingAsync(int userId);
    Task<IEnumerable<UserDto>> GetFollowersAsync(int userId);
    Task<IEnumerable<UserWithOnlineStatusDto>> GetFollowingWithOnlineStatusAsync(int userId);
    Task<IEnumerable<UserWithOnlineStatusDto>> GetTopFollowingWithOnlineStatusAsync(int userId, int limit = 10);
    Task<bool> UpdateFcmTokenAsync(int userId, string? fcmToken);
    Task<bool> UpdateExpoPushTokenAsync(int userId, string? expoPushToken);
    Task<IEnumerable<FollowRequestDto>> GetPendingFollowRequestsAsync(int userId);
    Task<FollowResponseDto> ApproveFollowRequestAsync(int requestId, int userId);
    Task<FollowResponseDto> DenyFollowRequestAsync(int requestId, int userId);
    Task<FollowResponseDto> ApproveFollowRequestByUserIdAsync(int requesterId, int userId);
    Task<FollowResponseDto> DenyFollowRequestByUserIdAsync(int requesterId, int userId);

    // Admin methods
    Task<User?> GetUserEntityByIdAsync(int userId);
    Task<bool> SuspendUserAsync(int userId, int suspendedByUserId, string reason, DateTime? suspendedUntil = null);
    Task<bool> UnsuspendUserAsync(int userId);
    Task<bool> BanUserAsync(int userId, int bannedByUserId, string reason, bool isShadowBan = false);
    Task<bool> UnbanUserAsync(int userId);
    Task<bool> ChangeUserRoleAsync(int userId, int changedByUserId, UserRole newRole, string reason);
    Task<bool> ForcePasswordResetAsync(int userId, int requestedByUserId, string reason);
    Task<IEnumerable<AdminUserDto>> GetUsersForAdminAsync(int page = 1, int pageSize = 25, UserStatus? status = null, UserRole? role = null);
    Task<AdminUserDto?> GetUserForAdminAsync(int userId);
}
