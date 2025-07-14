using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record CreateMessageDto(
    [Required] int RecipientId,
    [StringLength(1000)] string Content = "",
    string? ImageFileName = null
);