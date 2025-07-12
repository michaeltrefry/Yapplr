namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to track user activity
/// </summary>
public record TrackUserActivityCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string ActivityType { get; init; } // "login", "post_created", "comment_created", "like_given"
    public required DateTime Timestamp { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

/// <summary>
/// Command to track content engagement
/// </summary>
public record TrackContentEngagementCommand : BaseCommand
{
    public required string ContentType { get; init; } // "post", "comment"
    public required int ContentId { get; init; }
    public required int AuthorId { get; init; }
    public required string EngagementType { get; init; } // "view", "like", "comment", "share"
    public required DateTime Timestamp { get; init; }
    public int? EngagingUserId { get; init; } // null for anonymous views
}

/// <summary>
/// Command to update tag analytics
/// </summary>
public record UpdateTagAnalyticsCommand : BaseCommand
{
    public required int TagId { get; init; }
    public required string Action { get; init; } // "post_created", "post_liked", "post_shared"
    public required DateTime Timestamp { get; init; }
    public int? ActorUserId { get; init; }
}

/// <summary>
/// Command to generate daily analytics report
/// </summary>
public record GenerateDailyAnalyticsCommand : BaseCommand
{
    public required DateTime Date { get; init; }
    public List<string>? ReportTypes { get; init; } // null = all types
}

/// <summary>
/// Command to process user behavior patterns
/// </summary>
public record ProcessUserBehaviorPatternsCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public List<string>? PatternTypes { get; init; } // "engagement", "posting_frequency", "interaction_patterns"
}
