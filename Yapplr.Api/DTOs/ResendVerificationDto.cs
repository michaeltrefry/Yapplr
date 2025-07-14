using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public record ResendVerificationDto(
    [Required][EmailAddress] string Email
);