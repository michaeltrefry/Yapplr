using Postr.Api.DTOs;

namespace Postr.Api.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<UserProfileDto?> GetUserProfileAsync(string username);
    Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto updateDto);
    Task<IEnumerable<UserDto>> SearchUsersAsync(string query);
}
