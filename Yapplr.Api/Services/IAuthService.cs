using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterUserDto registerDto);
    Task<AuthResponseDto?> LoginAsync(LoginUserDto loginDto);
    Task<bool> RequestPasswordResetAsync(string email, string resetBaseUrl);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<bool> SendEmailVerificationAsync(string email, string verificationBaseUrl);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ResendEmailVerificationAsync(string email, string verificationBaseUrl);
    string GenerateJwtToken(User user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
