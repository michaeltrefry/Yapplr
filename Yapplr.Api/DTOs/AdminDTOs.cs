using Yapplr.Api.Models;

namespace Yapplr.Api.DTOs;

// System Tag DTOs
public class SystemTagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SystemTagCategory Category { get; set; }
    public bool IsVisibleToUsers { get; set; }
    public bool IsActive { get; set; }
    public string Color { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSystemTagDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SystemTagCategory Category { get; set; }
    public bool IsVisibleToUsers { get; set; } = false;
    public string Color { get; set; } = "#6B7280";
    public string? Icon { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdateSystemTagDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public SystemTagCategory? Category { get; set; }
    public bool? IsVisibleToUsers { get; set; }
    public bool? IsActive { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int? SortOrder { get; set; }
}

public class ApplySystemTagDto
{
    public int SystemTagId { get; set; }
    public string? Reason { get; set; }
}

// User Management DTOs
public class AdminUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    public string? SuspensionReason { get; set; }
    public string? SuspendedByUsername { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public bool EmailVerified { get; set; }
    public int PostCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
}

public class SuspendUserDto
{
    public string Reason { get; set; } = string.Empty;
    public DateTime? SuspendedUntil { get; set; } // null for permanent suspension
}

public class BanUserDto
{
    public string Reason { get; set; } = string.Empty;
    public bool IsShadowBan { get; set; } = false;
}

public class ChangeUserRoleDto
{
    public UserRole Role { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// Content Management DTOs
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
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int RepostCount { get; set; }
    public List<SystemTagDto> SystemTags { get; set; } = new();
    public List<AiSuggestedTagDto> AiSuggestedTags { get; set; } = new();
}

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
    public int PostId { get; set; }
    public List<SystemTagDto> SystemTags { get; set; } = new();
    public List<AiSuggestedTagDto> AiSuggestedTags { get; set; } = new();
}

public class AiSuggestedTagDto
{
    public int Id { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }
    public DateTime SuggestedAt { get; set; }
    public bool IsApproved { get; set; }
    public bool IsRejected { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUsername { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalReason { get; set; }
}

public class HideContentDto
{
    public string Reason { get; set; } = string.Empty;
}

// Audit Log DTOs
public class AuditLogDto
{
    public int Id { get; set; }
    public AuditAction Action { get; set; }
    public string PerformedByUsername { get; set; } = string.Empty;
    public string? TargetUsername { get; set; }
    public int? TargetPostId { get; set; }
    public int? TargetCommentId { get; set; }
    public string? Reason { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

// User Appeal DTOs
public class UserAppealDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public AppealType Type { get; set; }
    public AppealStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
    public int? TargetPostId { get; set; }
    public int? TargetCommentId { get; set; }
    public string? ReviewedByUsername { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAppealDto
{
    public AppealType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
    public int? TargetPostId { get; set; }
    public int? TargetCommentId { get; set; }
}

public class ReviewAppealDto
{
    public AppealStatus Status { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
}

// Analytics DTOs
public class ModerationStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public int BannedUsers { get; set; }
    public int ShadowBannedUsers { get; set; }
    public int TotalPosts { get; set; }
    public int HiddenPosts { get; set; }
    public int TotalComments { get; set; }
    public int HiddenComments { get; set; }
    public int PendingAppeals { get; set; }
    public int TodayActions { get; set; }
    public int WeekActions { get; set; }
    public int MonthActions { get; set; }
}

public class ContentQueueDto
{
    public List<AdminPostDto> FlaggedPosts { get; set; } = new();
    public List<AdminCommentDto> FlaggedComments { get; set; } = new();
    public List<UserAppealDto> PendingAppeals { get; set; } = new();
    public int TotalFlaggedContent { get; set; }
}

// Enhanced Analytics DTOs
public class UserGrowthStatsDto
{
    public List<DailyStatsDto> DailyStats { get; set; } = new();
    public int TotalNewUsers { get; set; }
    public int TotalActiveUsers { get; set; }
    public double GrowthRate { get; set; }
    public int PeakDayNewUsers { get; set; }
    public DateTime PeakDate { get; set; }
}

public class ContentStatsDto
{
    public List<DailyStatsDto> DailyPosts { get; set; } = new();
    public List<DailyStatsDto> DailyComments { get; set; } = new();
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public double PostsGrowthRate { get; set; }
    public double CommentsGrowthRate { get; set; }
    public int AveragePostsPerDay { get; set; }
    public int AverageCommentsPerDay { get; set; }
}

public class ModerationTrendsDto
{
    public List<DailyStatsDto> DailyActions { get; set; } = new();
    public List<ActionTypeStatsDto> ActionBreakdown { get; set; } = new();
    public int TotalActions { get; set; }
    public double ActionsGrowthRate { get; set; }
    public int PeakDayActions { get; set; }
    public DateTime PeakDate { get; set; }
}

public class SystemHealthDto
{
    public double UptimePercentage { get; set; }
    public int ActiveUsers24h { get; set; }
    public int ErrorCount24h { get; set; }
    public double AverageResponseTime { get; set; }
    public int DatabaseConnections { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
    public List<SystemAlertDto> Alerts { get; set; } = new();
}

public class TopModeratorsDto
{
    public List<ModeratorStatsDto> Moderators { get; set; } = new();
    public int TotalModerators { get; set; }
    public int TotalActions { get; set; }
}

public class ContentTrendsDto
{
    public List<HashtagStatsDto> TrendingHashtags { get; set; } = new();
    public List<DailyStatsDto> EngagementTrends { get; set; } = new();
    public int TotalHashtags { get; set; }
    public double AverageEngagementRate { get; set; }
}

public class UserEngagementStatsDto
{
    public List<DailyStatsDto> DailyEngagement { get; set; } = new();
    public double AverageSessionDuration { get; set; }
    public int TotalSessions { get; set; }
    public double RetentionRate { get; set; }
    public List<EngagementTypeStatsDto> EngagementBreakdown { get; set; } = new();
}

// Supporting DTOs
public class DailyStatsDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class ActionTypeStatsDto
{
    public string ActionType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class SystemAlertDto
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ModeratorStatsDto
{
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int TotalActions { get; set; }
    public int UserActions { get; set; }
    public int ContentActions { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastActive { get; set; }
}

public class HashtagStatsDto
{
    public string Hashtag { get; set; } = string.Empty;
    public int Count { get; set; }
    public double GrowthRate { get; set; }
    public int UniqueUsers { get; set; }
}

public class EngagementTypeStatsDto
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
