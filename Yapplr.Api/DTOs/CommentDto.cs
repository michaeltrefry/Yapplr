namespace Yapplr.Api.DTOs;

public record CommentDto(
    int Id,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    UserDto User,
    bool IsEdited = false,
    int LikeCount = 0,
    bool IsLikedByCurrentUser = false
);