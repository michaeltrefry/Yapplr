namespace Yapplr.Api.Services;

/// <summary>
/// Configuration options for the trust score background service
/// </summary>
public class TrustScoreBackgroundOptions
{
    public const string SectionName = "TrustScoreBackground";

    /// <summary>
    /// How often to run the background service
    /// </summary>
    public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Whether to apply inactivity decay
    /// </summary>
    public bool EnableInactivityDecay { get; set; } = true;

    /// <summary>
    /// Number of days of inactivity before decay starts
    /// </summary>
    public int InactivityDays { get; set; } = 30;

    /// <summary>
    /// Daily decay rate for inactive users
    /// </summary>
    public float DecayRate { get; set; } = 0.005f;

    /// <summary>
    /// Whether to periodically recalculate trust scores
    /// </summary>
    public bool EnablePeriodicRecalculation { get; set; } = true;

    /// <summary>
    /// Batch size for recalculation
    /// </summary>
    public int RecalculationBatchSize { get; set; } = 50;

    /// <summary>
    /// Whether to log trust score statistics
    /// </summary>
    public bool EnableStatisticsLogging { get; set; } = true;

    /// <summary>
    /// Whether to alert on low trust scores
    /// </summary>
    public bool EnableLowTrustScoreAlerts { get; set; } = true;

    /// <summary>
    /// Threshold for low trust score alerts
    /// </summary>
    public float LowTrustScoreThreshold { get; set; } = 0.3f;

    /// <summary>
    /// Maximum number of low trust score users to report
    /// </summary>
    public int LowTrustScoreAlertLimit { get; set; } = 10;
}