namespace Yapplr.Api.DTOs;

public record GroupDto(
    int Id,
    string Name,
    string Description,
    string? ImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsOpen,
    UserDto User, // Group creator/owner
    int MemberCount,
    int PostCount,
    bool IsCurrentUserMember = false
);
