using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class UserPreferences
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    public bool DarkMode { get; set; } = false;

    public bool RequireFollowApproval { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}
