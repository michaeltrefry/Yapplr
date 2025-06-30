using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.DTOs;

public class UpdateFcmTokenDto
{
    [Required]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;
}
