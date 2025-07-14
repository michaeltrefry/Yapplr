using System.ComponentModel.DataAnnotations;
using Yapplr.Api.Common;

namespace Yapplr.Api.Models.Analytics;

/// <summary>
/// Tracks system performance metrics for monitoring and optimization
/// </summary>
public class PerformanceMetric : IEntity
{
    public int Id { get; set; }
    
    /// <summary>
    /// Type of metric being recorded
    /// </summary>
    public MetricType MetricType { get; set; }
    
    /// <summary>
    /// Metric value (e.g., response time in ms, memory usage in MB)
    /// </summary>
    public double Value { get; set; }
    
    /// <summary>
    /// Unit of measurement (e.g., "ms", "MB", "percent", "count")
    /// </summary>
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// Component or service that generated the metric
    /// </summary>
    [StringLength(100)]
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Specific operation or endpoint
    /// </summary>
    [StringLength(200)]
    public string? Operation { get; set; }
    
    /// <summary>
    /// Additional tags for filtering and grouping (JSON format)
    /// </summary>
    [StringLength(1000)]
    public string? Tags { get; set; }
    
    /// <summary>
    /// Server or instance identifier
    /// </summary>
    [StringLength(100)]
    public string? InstanceId { get; set; }
    
    /// <summary>
    /// Environment (e.g., "production", "staging", "development")
    /// </summary>
    [StringLength(20)]
    public string? Environment { get; set; }
    
    /// <summary>
    /// Version of the application
    /// </summary>
    [StringLength(50)]
    public string? Version { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this metric indicates a success or failure
    /// </summary>
    public bool? Success { get; set; }
    
    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// User ID if this metric is user-specific
    /// </summary>
    public int? UserId { get; set; }
    public User? User { get; set; }
    
    /// <summary>
    /// Session ID for grouping related metrics
    /// </summary>
    [StringLength(100)]
    public string? SessionId { get; set; }
}
