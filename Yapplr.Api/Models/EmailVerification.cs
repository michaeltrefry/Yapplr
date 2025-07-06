using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class EmailVerification
{
    public int Id { get; set; }
    
    [Required]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
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
