using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Bio { get; set; } = string.Empty;
    
    public DateTime? Birthday { get; set; }
    
    [StringLength(100)]
    public string Pronouns { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string Tagline { get; set; } = string.Empty;

    [StringLength(255)]
    public string ProfileImageFileName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Repost> Reposts { get; set; } = new List<Repost>();

    // Follow relationships
    public ICollection<Follow> Followers { get; set; } = new List<Follow>(); // Users following this user
    public ICollection<Follow> Following { get; set; } = new List<Follow>(); // Users this user is following
}
