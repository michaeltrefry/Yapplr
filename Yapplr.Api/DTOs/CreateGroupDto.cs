using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record CreateGroupDto(
    [Required][StringLength(100, MinimumLength = 3)] string Name,
    [StringLength(500)] string Description = "",
    string? ImageUrl = null
);
