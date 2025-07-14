using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record MessageDto(
    int Id,
    string Content,
    string? ImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsEdited,
    bool IsDeleted,
    int ConversationId,
    UserDto Sender,
    MessageStatusType? Status = null
);