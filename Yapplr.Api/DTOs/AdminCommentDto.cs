namespace Yapplr.Api.DTOs;

public class AdminCommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }
    public DateTime? HiddenAt { get; set; }
    public string? HiddenByUsername { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserDto User { get; set; } = null!;
    public GroupDto? Group { get; set; } // Optional - only set for group comments
    public int PostId { get; set; }
    public List<SystemTagDto> SystemTags { get; set; } = new();
    public List<AiSuggestedTagDto> AiSuggestedTags { get; set; } = new();
}
