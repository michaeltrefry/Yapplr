namespace Yapplr.Api.DTOs;

public record UserWithOnlineStatusDto(
    int Id,
    string Email,
    string Username,
    string Bio,
    DateTime? Birthday,
    string Pronouns,
    string Tagline,
    string? ProfileImageUrl,
    DateTime CreatedAt,
    bool IsOnline
);