using Postr.Api.DTOs;
using Postr.Api.Models;

namespace Postr.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterUserDto registerDto);
    Task<AuthResponseDto?> LoginAsync(LoginUserDto loginDto);
    Task<bool> RequestPasswordResetAsync(string email, string resetBaseUrl);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    string GenerateJwtToken(User user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
