using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record ResetPasswordDto(
    [Required] string Token,
    [Required][MinLength(6)] string NewPassword
);