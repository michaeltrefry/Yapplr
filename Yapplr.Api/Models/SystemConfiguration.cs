using System.ComponentModel.DataAnnotations;

namespace Yapplr.Api.Models;

/// <summary>
/// Database model for storing system-wide configuration settings
/// </summary>
public class SystemConfiguration
{
    public int Id { get; set; }
    
    /// <summary>
    /// Configuration key identifier
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Configuration value (stored as string, can be parsed as needed)
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable description of this configuration setting
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Configuration category for grouping related settings
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; } = "General";
    
    /// <summary>
    /// Whether this setting is visible in admin interface
    /// </summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// Whether this setting can be modified through admin interface
    /// </summary>
    public bool IsEditable { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
