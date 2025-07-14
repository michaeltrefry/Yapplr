namespace Yapplr.Api.DTOs;

public record ConversationDto(
    int Id,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<UserDto> Participants,
    MessageDto? LastMessage = null,
    int UnreadCount = 0
);