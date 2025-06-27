using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface IUserPreferencesService
{
    Task<UserPreferencesDto> GetUserPreferencesAsync(int userId);
    Task<UserPreferencesDto> UpdateUserPreferencesAsync(int userId, UpdateUserPreferencesDto updateDto);
}
