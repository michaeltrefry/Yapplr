namespace Yapplr.Api.DTOs;

public record CommentDto(
    int Id,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    UserDto User,
    bool IsEdited = false
);