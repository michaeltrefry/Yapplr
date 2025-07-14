using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record RegisterUserDto(
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password,
    [Required][StringLength(50, MinimumLength = 3)] string Username,
    [Required] bool AcceptTerms,
    [StringLength(500)] string Bio = "",
    DateTime? Birthday = null,
    [StringLength(100)] string Pronouns = "",
    [StringLength(200)] string Tagline = ""
);