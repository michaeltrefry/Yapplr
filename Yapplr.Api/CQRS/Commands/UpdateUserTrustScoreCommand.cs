namespace Yapplr.Api.CQRS.Commands;

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