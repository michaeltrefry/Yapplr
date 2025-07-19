using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class AdminPostDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageFileName { get; set; }
    public PostPrivacy Privacy { get; set; }
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }
    public DateTime? HiddenAt { get; set; }
    public string? HiddenByUsername { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserDto User { get; set; } = null!;
    public int LikeCount { get; set; } // Legacy - will be replaced by ReactionCounts
    public int CommentCount { get; set; }
    public int RepostCount { get; set; }
    public List<SystemTagDto> SystemTags { get; set; } = new();
    public List<AiSuggestedTagDto> AiSuggestedTags { get; set; } = new();
    public List<ReactionCountDto> ReactionCounts { get; set; } = new();
    public int TotalReactionCount { get; set; }
}
