namespace Yapplr.Api.CQRS.Commands;

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