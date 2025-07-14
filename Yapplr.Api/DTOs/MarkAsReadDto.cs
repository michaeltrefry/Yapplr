using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record MarkAsReadDto(
    [Required] int ConversationId
);