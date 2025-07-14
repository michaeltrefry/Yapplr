using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record LoginUserDto(
    [Required][EmailAddress] string Email,
    [Required] string Password
);