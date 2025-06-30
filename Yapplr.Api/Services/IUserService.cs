using Yapplr.Api.DTOs;

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
    Task<bool> UpdateFcmTokenAsync(int userId, string fcmToken);
}
