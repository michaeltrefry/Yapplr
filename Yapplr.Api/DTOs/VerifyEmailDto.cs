using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record VerifyEmailDto(
    [Required] string Token
);