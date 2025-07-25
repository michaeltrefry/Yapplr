using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;
using Yapplr.Api.Extensions;
using Yapplr.Api.Exceptions;
using Yapplr.Api.CQRS;
using Yapplr.Api.CQRS.Commands;
using Serilog.Context;

namespace Yapplr.Api.Services;

public class AuthService : IAuthService
{
    private readonly YapplrDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ICommandPublisher _commandPublisher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(YapplrDbContext context, IConfiguration configuration, IEmailService emailService, ICommandPublisher commandPublisher, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _commandPublisher = commandPublisher;
        _logger = logger;
    }

    public async Task<RegisterResponseDto?> RegisterAsync(RegisterUserDto registerDto)
    {
        using var operationScope = LogContext.PushProperty("Operation", "UserRegistration");
        using var emailScope = LogContext.PushProperty("Email", registerDto.Email);
        using var usernameScope = LogContext.PushProperty("Username", registerDto.Username);

        _logger.LogInformation("Starting user registration for {Email} with username {Username}",
            registerDto.Email, registerDto.Username);

        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            _logger.LogWarning("Registration failed: Email {Email} already exists", registerDto.Email);
            return null; // User already exists
        }

        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
        {
            _logger.LogWarning("Registration failed: Username {Username} already taken", registerDto.Username);
            return null; // Username already taken
        }

        // Validate terms acceptance
        if (!registerDto.AcceptTerms)
        {
            _logger.LogWarning("Registration failed: Terms not accepted for {Email}", registerDto.Email);
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

        using var userScope = LogContext.PushProperty("UserId", user.Id);
        _logger.LogInformation("User {UserId} created successfully with email {Email}", user.Id, registerDto.Email);

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

        _logger.LogInformation("Email verification token generated for user {UserId}", user.Id);

        // Send verification email using CQRS command
        var emailCommand = new SendWelcomeEmailCommand
        {
            UserId = user.Id,
            ToEmail = registerDto.Email,
            Username = user.Username,
            VerificationToken = verificationToken
        };

        // Publish command asynchronously - don't wait for completion
        _ = Task.Run(async () =>
        {
            try
            {
                await _commandPublisher.PublishAsync(emailCommand);
                _logger.LogInformation("Welcome email command published for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                // Log error but don't fail registration
                _logger.LogError(ex, "Failed to publish welcome email command for user {UserId}", user.Id);
            }
        });

        // Return registration response without token - user must verify email first
        var userDto = user.ToDto();

        return new RegisterResponseDto(
            "Registration successful. Please check your email to verify your account before logging in.",
            userDto
        );
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginUserDto loginDto)
    {
        using var operationScope = LogContext.PushProperty("Operation", "UserLogin");
        using var emailScope = LogContext.PushProperty("Email", loginDto.Email);

        _logger.LogInformation("Login attempt for email {Email}", loginDto.Email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            _logger.LogSecurityEvent("LoginFailure",
                userId: user?.Id,
                details: new { Email = loginDto.Email, Reason = user == null ? "UserNotFound" : "InvalidPassword" });
            return null; // Invalid credentials
        }

        using var userScope = LogContext.PushProperty("UserId", user.Id);
        using var usernameScope = LogContext.PushProperty("Username", user.Username);

        // Prevent login as system user
        if (user.Role == UserRole.System)
        {
            _logger.LogWarning("Login failed: System user {UserId} cannot login", user.Id);
            return null; // System user cannot login
        }

        // Check if email is verified
        if (!user.EmailVerified)
        {
            _logger.LogWarning("Login failed: Unverified email for user {UserId} ({Email})", user.Id, user.Email);
            throw new EmailNotVerifiedException(user.Email);
        }

        var token = GenerateJwtToken(user);
        var userDto = user.ToDto();

        _logger.LogSecurityEvent("LoginSuccess",
            userId: user.Id,
            details: new { Username = user.Username, Role = user.Role.ToString() },
            level: LogLevel.Information);

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
            new Claim(ClaimTypes.Role, user.Role.ToString()),
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

        // Send reset email using CQRS command
        var emailCommand = new SendPasswordResetEmailCommand
        {
            UserId = user.Id,
            ToEmail = email,
            Username = user.Username,
            ResetToken = resetToken
        };

        // Publish command asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _commandPublisher.PublishAsync(emailCommand);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to publish password reset email command: {ex.Message}");
            }
        });

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
