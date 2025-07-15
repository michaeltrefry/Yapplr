namespace Yapplr.Api.DTOs;

/// <summary>
/// Response DTO for user registration - does not include JWT token
/// Users must verify their email before they can login and receive a token
/// </summary>
public record RegisterResponseDto(
    string Message,
    UserDto User
);
