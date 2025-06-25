using System.ComponentModel.DataAnnotations;

namespace Postr.Api.DTOs;

public record RegisterUserDto(
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password,
    [Required][StringLength(50, MinimumLength = 3)] string Username,
    [StringLength(500)] string Bio = "",
    DateTime? Birthday = null,
    [StringLength(100)] string Pronouns = "",
    [StringLength(200)] string Tagline = ""
);

public record LoginUserDto(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record UpdateUserDto(
    [StringLength(500)] string? Bio,
    DateTime? Birthday,
    [StringLength(100)] string? Pronouns,
    [StringLength(200)] string? Tagline
);

public record UserDto(
    int Id,
    string Email,
    string Username,
    string Bio,
    DateTime? Birthday,
    string Pronouns,
    string Tagline,
    string ProfileImageFileName,
    DateTime CreatedAt
);

public record UserProfileDto(
    int Id,
    string Username,
    string Bio,
    DateTime? Birthday,
    string Pronouns,
    string Tagline,
    string ProfileImageFileName,
    DateTime CreatedAt,
    int PostCount,
    int FollowerCount,
    int FollowingCount,
    bool IsFollowedByCurrentUser
);

public record AuthResponseDto(
    string Token,
    UserDto User
);

public record ForgotPasswordDto(
    [Required][EmailAddress] string Email
);

public record ResetPasswordDto(
    [Required] string Token,
    [Required][MinLength(6)] string NewPassword
);

public record FollowResponseDto(
    bool IsFollowing,
    int FollowerCount
);
