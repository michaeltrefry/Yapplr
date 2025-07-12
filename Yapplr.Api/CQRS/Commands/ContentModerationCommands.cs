namespace Yapplr.Api.CQRS.Commands;

/// <summary>
/// Command to analyze content for moderation
/// </summary>
public record AnalyzeContentCommand : BaseCommand
{
    public required string ContentType { get; init; } // "post", "comment", "message"
    public required int ContentId { get; init; }
    public required string Content { get; init; }
    public required int AuthorId { get; init; }
    public bool IsEdit { get; init; } = false;
}

/// <summary>
/// Command to process a user report
/// </summary>
public record ProcessUserReportCommand : BaseCommand
{
    public required int ReportId { get; init; }
    public required int ReportedUserId { get; init; }
    public required int ReporterUserId { get; init; }
    public required string ReportType { get; init; }
    public required string Reason { get; init; }
    public string? AdditionalContext { get; init; }
}

/// <summary>
/// Command to process a content report
/// </summary>
public record ProcessContentReportCommand : BaseCommand
{
    public required int ReportId { get; init; }
    public required string ContentType { get; init; }
    public required int ContentId { get; init; }
    public required int ReporterUserId { get; init; }
    public required string ReportType { get; init; }
    public required string Reason { get; init; }
    public string? AdditionalContext { get; init; }
}

/// <summary>
/// Command to apply automatic moderation action
/// </summary>
public record ApplyModerationActionCommand : BaseCommand
{
    public required string ContentType { get; init; }
    public required int ContentId { get; init; }
    public required string Action { get; init; } // "hide", "flag", "approve", "delete"
    public required string Reason { get; init; }
    public required string Source { get; init; } // "ai", "user_report", "admin"
    public float? ConfidenceScore { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Command to update user trust score
/// </summary>
public record UpdateUserTrustScoreCommand : BaseCommand
{
    public required int TargetUserId { get; init; }
    public required string Action { get; init; } // "violation", "positive_interaction", "report_validated"
    public required float ScoreChange { get; init; }
    public string? Reason { get; init; }
}
