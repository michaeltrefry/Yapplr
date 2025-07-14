using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record UpdateCommentDto(
    [Required][StringLength(256, MinimumLength = 1)] string Content
);