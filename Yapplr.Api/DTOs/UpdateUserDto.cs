using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record UpdateUserDto(
    [StringLength(500)] string? Bio,
    DateTime? Birthday,
    [StringLength(100)] string? Pronouns,
    [StringLength(200)] string? Tagline
);