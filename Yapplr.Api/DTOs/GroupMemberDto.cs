using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record GroupMemberDto(
    int Id,
    DateTime JoinedAt,
    GroupMemberRole Role,
    UserDto User
);
