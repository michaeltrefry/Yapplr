using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models;

public class Group : IUserOwnedEntity
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string? ImageFileName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Group settings - all groups are open for now
    public bool IsOpen { get; set; } = true; // Always true for now, anyone can join
    
    // Foreign keys
    public int UserId { get; set; } // Creator/Owner of the group
    
    // Navigation properties
    public User User { get; set; } = null!; // Group creator/owner
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    
    // Computed properties for convenience
    public int MemberCount => Members?.Count ?? 0;
    public int PostCount => Posts?.Count ?? 0;
}
