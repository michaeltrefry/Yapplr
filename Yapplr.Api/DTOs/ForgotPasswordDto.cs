using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record ForgotPasswordDto(
    [Required][EmailAddress] string Email
);