namespace Yapplr.Api.DTOs;

public record AuthResponseDto(
    string Token,
    UserDto User
);