using Postr.Api.DTOs;

namespace Postr.Api.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<UserProfileDto?> GetUserProfileAsync(string username, int? currentUserId = null);
    Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto updateDto);
    Task<IEnumerable<UserDto>> SearchUsersAsync(string query);
    Task<UserDto?> UpdateProfileImageAsync(int userId, string fileName);
    Task<UserDto?> RemoveProfileImageAsync(int userId, IImageService imageService);
    Task<FollowResponseDto> FollowUserAsync(int followerId, int followingId);
    Task<FollowResponseDto> UnfollowUserAsync(int followerId, int followingId);
    Task<IEnumerable<UserDto>> GetFollowingAsync(int userId);
}
