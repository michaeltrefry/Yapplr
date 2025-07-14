namespace Yapplr.Api.DTOs;

public record ConversationListDto(
    int Id,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    UserDto OtherParticipant,
    MessageDto? LastMessage = null,
    int UnreadCount = 0
);