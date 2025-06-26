using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public record CreateMessageDto(
    [Required] int RecipientId,
    [StringLength(1000)] string Content = "",
    string? ImageFileName = null
);

public record SendMessageDto(
    [Required] int ConversationId,
    [StringLength(1000)] string Content = "",
    string? ImageFileName = null
);

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

public record ConversationDto(
    int Id,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<UserDto> Participants,
    MessageDto? LastMessage = null,
    int UnreadCount = 0
);

public record ConversationListDto(
    int Id,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    UserDto OtherParticipant,
    MessageDto? LastMessage = null,
    int UnreadCount = 0
);

public record MarkAsReadDto(
    [Required] int ConversationId
);
