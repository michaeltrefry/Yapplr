namespace Yapplr.Api.DTOs;

public class MentionDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MentionedUserId { get; set; }
    public int MentioningUserId { get; set; }
    public int PostId { get; set; } // The post/comment where mention occurred
    public bool IsCommentMention { get; set; } // True if mentioned in a comment, false if in a post
    public int? ParentPostId { get; set; } // If comment mention, this is the parent post ID
}
