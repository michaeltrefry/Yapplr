using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Postr.Api.Data;
using Postr.Api.DTOs;
using Postr.Api.Models;

namespace Postr.Api.Services;

public class AuthService : IAuthService
{
    private readonly PostrDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthService(PostrDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterUserDto registerDto)
    {
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            return null; // User already exists
        }

        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
        {
            return null; // Username already taken
        }

        // Create new user
        var user = new User
        {
            Email = registerDto.Email,
            Username = registerDto.Username,
            PasswordHash = HashPassword(registerDto.Password),
            Bio = registerDto.Bio,
            Birthday = registerDto.Birthday.HasValue ? DateTime.SpecifyKind(registerDto.Birthday.Value, DateTimeKind.Utc) : null,
            Pronouns = registerDto.Pronouns,
            Tagline = registerDto.Tagline,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var userDto = new UserDto(user.Id, user.Email, user.Username, user.Bio,
                                 user.Birthday, user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt);

        return new AuthResponseDto(token, userDto);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginUserDto loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        
        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return null; // Invalid credentials
        }

        var token = GenerateJwtToken(user);
        var userDto = new UserDto(user.Id, user.Email, user.Username, user.Bio,
                                 user.Birthday, user.Pronouns, user.Tagline, user.ProfileImageFileName, user.CreatedAt);

        return new AuthResponseDto(token, userDto);
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"]!);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public async Task<bool> RequestPasswordResetAsync(string email, string resetBaseUrl)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            // Don't reveal if email exists or not for security
            return true;
        }

        // Invalidate any existing reset tokens for this user
        var existingTokens = await _context.PasswordResets
            .Where(pr => pr.UserId == user.Id && !pr.IsUsed && pr.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in existingTokens)
        {
            token.IsUsed = true;
            token.UsedAt = DateTime.UtcNow;
        }

        // Generate secure reset token
        var resetToken = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

        var passwordReset = new PasswordReset
        {
            Token = resetToken,
            Email = email,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResets.Add(passwordReset);
        await _context.SaveChangesAsync();

        // Send reset email
        var resetUrl = $"{resetBaseUrl}?token={resetToken}";
        await _emailService.SendPasswordResetEmailAsync(email, user.Username, resetToken, resetUrl);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var passwordReset = await _context.PasswordResets
            .Include(pr => pr.User)
            .FirstOrDefaultAsync(pr => pr.Token == token && !pr.IsUsed && pr.ExpiresAt > DateTime.UtcNow);

        if (passwordReset == null)
        {
            return false; // Invalid or expired token
        }

        // Update user password
        passwordReset.User.PasswordHash = HashPassword(newPassword);
        passwordReset.User.UpdatedAt = DateTime.UtcNow;

        // Mark token as used
        passwordReset.IsUsed = true;
        passwordReset.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
