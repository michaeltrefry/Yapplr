namespace Yapplr.Api.DTOs;

public record GroupListDto(
    int Id,
    string Name,
    string Description,
    string? ImageFileName,
    DateTime CreatedAt,
    string CreatorUsername,
    int MemberCount,
    int PostCount,
    bool IsCurrentUserMember = false
);
