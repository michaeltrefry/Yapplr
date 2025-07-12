using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class PasswordReset
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public bool IsUsed { get; set; } = false;
    
    public DateTime? UsedAt { get; set; }
    
    // Foreign key
    public int UserId { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
}
