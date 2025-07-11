using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;
using Yapplr.Api.Exceptions;

namespace Yapplr.Api.Services;

public class AuthService : IAuthService
{
    private readonly YapplrDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthService(YapplrDbContext context, IConfiguration configuration, IEmailService emailService)
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

        // Validate terms acceptance
        if (!registerDto.AcceptTerms)
        {
            throw new ArgumentException("Terms of service must be accepted to create an account");
        }

        // Create new user (unverified by default)
        var user = new User
        {
            Email = registerDto.Email,
            Username = registerDto.Username,
            PasswordHash = HashPassword(registerDto.Password),
            Bio = registerDto.Bio,
            Birthday = registerDto.Birthday.HasValue ? DateTime.SpecifyKind(registerDto.Birthday.Value, DateTimeKind.Utc) : null,
            Pronouns = registerDto.Pronouns,
            Tagline = registerDto.Tagline,
            EmailVerified = false, // User starts unverified
            TermsAcceptedAt = DateTime.UtcNow, // Record terms acceptance
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate and send email verification
        var verificationToken = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddHours(24); // Token expires in 24 hours

        var emailVerification = new EmailVerification
        {
            Token = verificationToken,
            Email = registerDto.Email,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(emailVerification);
        await _context.SaveChangesAsync();

        // Send verification email
        var verificationUrl = $"https://yapplr.com/verify-email?token={verificationToken}";
        await _emailService.SendEmailVerificationAsync(registerDto.Email, user.Username, verificationToken, verificationUrl);

        // Return auth response but user will need to verify email to fully access the app
        var token = GenerateJwtToken(user);
        var userDto = user.ToDto();

        return new AuthResponseDto(token, userDto);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginUserDto loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return null; // Invalid credentials
        }

        // Prevent login as system user
        if (user.Role == UserRole.System)
        {
            return null; // System user cannot login
        }

        // Check if email is verified
        if (!user.EmailVerified)
        {
            throw new EmailNotVerifiedException(user.Email);
        }

        var token = GenerateJwtToken(user);
        var userDto = user.ToDto();

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

    public async Task<bool> SendEmailVerificationAsync(string email, string verificationBaseUrl)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || user.EmailVerified)
        {
            // Don't reveal if email exists or is already verified
            return true;
        }

        // Invalidate any existing verification tokens for this user
        var existingTokens = await _context.EmailVerifications
            .Where(ev => ev.UserId == user.Id && !ev.IsUsed && ev.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in existingTokens)
        {
            token.IsUsed = true;
            token.UsedAt = DateTime.UtcNow;
        }

        // Generate new verification token
        var verificationToken = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddHours(24); // Token expires in 24 hours

        var emailVerification = new EmailVerification
        {
            Token = verificationToken,
            Email = email,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerifications.Add(emailVerification);
        await _context.SaveChangesAsync();

        // Send verification email
        var verificationUrl = $"{verificationBaseUrl}?token={verificationToken}";
        await _emailService.SendEmailVerificationAsync(email, user.Username, verificationToken, verificationUrl);

        return true;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var emailVerification = await _context.EmailVerifications
            .Include(ev => ev.User)
            .FirstOrDefaultAsync(ev => ev.Token == token && !ev.IsUsed && ev.ExpiresAt > DateTime.UtcNow);

        if (emailVerification == null)
        {
            return false; // Invalid or expired token
        }

        // Mark user as verified
        emailVerification.User.EmailVerified = true;
        emailVerification.User.UpdatedAt = DateTime.UtcNow;

        // Mark token as used
        emailVerification.IsUsed = true;
        emailVerification.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResendEmailVerificationAsync(string email, string verificationBaseUrl)
    {
        // This is the same as SendEmailVerificationAsync - it will invalidate old tokens and send a new one
        return await SendEmailVerificationAsync(email, verificationBaseUrl);
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32(bytes, 0);
        // Generate a 6-digit code (100000 to 999999)
        return (100000 + (number % 900000)).ToString();
    }
}
