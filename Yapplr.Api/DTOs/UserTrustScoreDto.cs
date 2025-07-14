using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

public class UserTrustScoreDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public float TrustScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public UserStatus Status { get; set; }
    public UserRole Role { get; set; }
    public int PostCount { get; set; }
    public int CommentCount { get; set; }
    public int LikeCount { get; set; }
    public int ReportCount { get; set; }
    public int ModerationActionCount { get; set; }
}
