using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record SendMessageDto(
    [Required] int ConversationId,
    [StringLength(1000)] string Content = "",
    string? ImageFileName = null
);