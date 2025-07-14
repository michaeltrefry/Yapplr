namespace Yapplr.Api.Services;

/// <summary>
/// Content visibility levels based on trust scores
/// </summary>
public enum ContentVisibilityLevel
{
    Hidden,              // Content is hidden from feeds
    LimitedVisibility,   // Only visible to followers
    ReducedVisibility,   // Lower priority in feeds
    NormalVisibility,    // Normal feed visibility
    FullVisibility       // Boosted visibility in feeds
}