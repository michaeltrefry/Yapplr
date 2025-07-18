using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record UpdateGroupDto(
    [Required][StringLength(100, MinimumLength = 3)] string Name,
    [StringLength(500)] string Description,
    string? ImageFileName = null
);
