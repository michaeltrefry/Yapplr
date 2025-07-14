namespace Yapplr.Api.DTOs;

public record PostSystemTagDto(
    int Id,
    string Name,
    string Description,
    string Category,
    bool IsVisibleToUsers,
    string Color,
    string? Icon,
    string? Reason,
    DateTime AppliedAt,
    UserDto AppliedByUser
);