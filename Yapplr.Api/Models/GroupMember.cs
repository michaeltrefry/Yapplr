namespace Yapplr.Api.Models;

public class GroupMember
{
    public int Id { get; set; }
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    // For future use - could add roles like Admin, Moderator, Member
    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;
    
    // Foreign keys
    public int GroupId { get; set; }
    public int UserId { get; set; }
    
    // Navigation properties
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum GroupMemberRole
{
    Member = 0,
    Moderator = 1,
    Admin = 2
}
