namespace Yapplr.Api.DTOs;

public class MentionDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MentionedUserId { get; set; }
    public int MentioningUserId { get; set; }
    public int? PostId { get; set; }
    public int? CommentId { get; set; }
}
